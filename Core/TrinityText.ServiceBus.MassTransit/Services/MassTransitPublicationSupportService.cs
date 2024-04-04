using Resulz;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using TrinityText.Business;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace TrinityText.ServiceBus.MassTransit.Services
{
    public class MassTransitPublicationSupportService : IPublicationSupportService
    {
        private readonly PublicationSupportOptions _options;

        private readonly ITextService _textService;

        private readonly IPageService _pageService;

        private readonly IPageSchemaService _pageSchemaService;

        private readonly IFileManagerService _fileManagerService;
        private readonly ITransferServiceCoordinator _transferServiceCoordinator;

        private readonly ICompressionFileService _compressionFileService;

        private readonly IPublicationService _publicationService;

        public MassTransitPublicationSupportService(IPublicationService publicationService, ITextService textService, IPageService pageService, IPageSchemaService pageSchemaService,
            IFileManagerService fileManagerService, ICompressionFileService compressionFileService, ITransferServiceCoordinator transferServiceCoordinator,
            IOptions<PublicationSupportOptions> options)
        {
            _publicationService = publicationService;
            _textService = textService;
            _pageService = pageService;
            _pageSchemaService = pageSchemaService;
            _fileManagerService = fileManagerService;
            _compressionFileService = compressionFileService;
            _transferServiceCoordinator = transferServiceCoordinator;
            _options = options.Value;
        }

        private async Task<OperationResult<string>> CreateExportFile(int id, PayloadDTO payload, PublicationType exportType, PublicationFormat publishType, DateTime filesGenerationDate, bool compressFileOutput, string user, CdnServerDTO cdnServer)
        {
            var basePath = _options.LocalDirectory;
            var website = payload.Website;
            var tenant = payload.Tenant;
            var sites = payload.Sites;
            var textTypes = payload.TextTypes;


            DirectoryInfo baseDirectory = new DirectoryInfo(basePath);
            if (!baseDirectory.Exists)
            {
                baseDirectory.Create();
            }

            var currentDirectory = baseDirectory.CreateSubdirectory($"{website}_{id}_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}");

            var textSubDirectory = currentDirectory.CreateSubdirectory("Text");
            var filesSubDirectory = currentDirectory.CreateSubdirectory("Files");

            foreach (var i in sites)
            {
                if (exportType == PublicationType.All || exportType == PublicationType.Texts)
                {
                    var siteDirectory = textSubDirectory.CreateSubdirectory(i.Site.ToUpper());
                    var textsPerSiteRs = await _textService.GetPublishableTexts(website, i.Site, i.Languages.ToArray(), textTypes.ToArray());

                    if (textsPerSiteRs.Success)
                    {
                        var textsPerSite = textsPerSiteRs.Value;
                        GenerateTextsFileBySite(website, textsPerSite, siteDirectory.FullName, publishType);
                    }
                    else
                    {
                        return OperationResult<string>.MakeFailure(textsPerSiteRs.Errors);
                    }
                }

                if (exportType == PublicationType.All || exportType == PublicationType.Pages || exportType == PublicationType.PDF)
                {
                    var pagesPerSiteRs = await _pageService.GetPublishablePages(website, i.Site, i.Languages.ToArray());

                    if (pagesPerSiteRs.Success)
                    {
                        var pagesPerSite = pagesPerSiteRs.Value;
                        if (exportType == PublicationType.All || exportType == PublicationType.Pages)
                        {
                            var instanceDirectory = textSubDirectory.CreateSubdirectory(i.Site.ToUpper());
                            await GeneratePagesFileBySite(tenant, website, i.Site, pagesPerSite, instanceDirectory.FullName, string.Empty, cdnServer, publishType);
                        }

                        if (exportType == PublicationType.All || exportType == PublicationType.PDF)
                        {
                            var instanceDirectory = filesSubDirectory.CreateSubdirectory(i.Site.ToUpper());
                            GeneratePDFPagesFileBySite(tenant, website, i.Site, pagesPerSite, instanceDirectory.FullName);
                        }
                    }
                    else
                    {
                        return OperationResult<string>.MakeFailure(pagesPerSiteRs.Errors);
                    }
                }
            }

            if (exportType == PublicationType.All || exportType == PublicationType.Files)
            {
                await GenerateFilesByWebsite(website, filesGenerationDate, currentDirectory.FullName);
            }

            GenerateFileTimestamp(currentDirectory, user);

            DeleteEmptySubdirectories(currentDirectory.FullName);

            if (compressFileOutput)
            {
                var filePath = await _compressionFileService.CompressFolder(currentDirectory.FullName, basePath);
                currentDirectory.Delete(true);

                return OperationResult<string>.MakeSuccess(filePath);
            }
            else
            {
                return OperationResult<string>.MakeSuccess(currentDirectory.FullName);
            }
        }

        public void FastDeleteEmptySubdirectories(string parentDirectory)
        {
            System.Threading.Tasks.Parallel.ForEach(System.IO.Directory.GetDirectories(parentDirectory), directory => {
                FastDeleteEmptySubdirectories(directory);
                if (!System.IO.Directory.EnumerateFileSystemEntries(directory).Any()) System.IO.Directory.Delete(directory, false);
            });
        }

        public void DeleteEmptySubdirectories(string parentDirectory)
        {
            foreach (string directory in System.IO.Directory.GetDirectories(parentDirectory))
            {
                DeleteEmptySubdirectories(directory);
                if (!System.IO.Directory.EnumerateFileSystemEntries(directory).Any()) System.IO.Directory.Delete(directory, false);
            }
        }

        public async Task<OperationResult> Generate(PublicationDTO setting)
        {
            var result = OperationResult.MakeSuccess();
            try
            {
                var website = setting.Website;
                var dataType = setting.DataType;
                var filterDate = setting.FilterDataDate;
                var cdnServer = setting.CdnServer;

                var payload = setting.Payload;

                var filePathRs = await CreateExportFile(setting.Id.Value, payload, dataType, setting.Format, filterDate, true, setting.CreationUser, cdnServer);

                if (filePathRs.Success)
                {
                    var filePath = filePathRs.Value;
                    byte[] byteArray = System.IO.File.ReadAllBytes(filePath);
                    System.IO.File.Delete(filePath);

                    var updateRs = await _publicationService.Update(setting.Id.Value, PublicationStatus.Generating, "Zip file completed", byteArray);

                    if (updateRs.Success == false)
                    {
                        result.AppendErrors(updateRs.Errors);
                    }
                }
                else
                {
                    result.AppendErrors(filePathRs.Errors);
                }
            }
            catch (Exception ex)
            {
                result.AppendError("GENERATE", ex.Message);
            }
            return result;
        }

        public async Task<OperationResult> Publish(PublicationDTO setting)
        {
            var basePath = $"{_options.LocalDirectory}/Uploading/{Guid.NewGuid()}";
            var result = OperationResult.MakeSuccess();
            try
            {
                await _compressionFileService.DecompressFolder(basePath, setting.ZipFile);

                var payload = setting.Payload;


                var server = setting.FtpServer;
                var d = new DirectoryInfo(basePath);

                var uploadRs = await _transferServiceCoordinator.Upload(payload.Tenant, setting.Website, d, server.Host, server.Username, server.Password);

                if (uploadRs.Success == false)
                {
                    result.AppendErrors(uploadRs.Errors);
                }
            }
            catch (Exception ex)
            {
                result.AppendError("PUBLISH", ex.Message);
            }
            finally
            {
                try
                {
                    DirectoryInfo folder = new DirectoryInfo(basePath);
                    if (folder.Exists)
                    {
                        folder.Delete(true);
                    }
                }
                catch
                {

                }
            }
            return result;
        }

        #region private methods

        private void GenerateFileTimestamp(DirectoryInfo currentDirectory, string username)
        {
            var textFile = $"{currentDirectory}\\trinity-text.txt";
            var text = $"{username}|{DateTime.Now:dd-MM-yyyy|HH-mm-ss}";

            System.IO.File.WriteAllText(textFile, text);
        }

        private void GenerateTextsFileBySite(string website, IDictionary<string, List<TextDTO>> textsPerLanguage, string directoryPath, PublicationFormat type)
        {
            if (string.IsNullOrWhiteSpace(directoryPath))
            {
                directoryPath = _options.LocalDirectory;
            }

            DirectoryInfo directory = new DirectoryInfo(directoryPath);
            if (!directory.Exists)
            {
                directory.Create();
            }

            foreach (var lang in textsPerLanguage.Keys)
            {
                var langDir = directory.CreateSubdirectory(lang);

                var resources = textsPerLanguage[lang];

                IDictionary<string, string> types =
                    resources
                    .GroupBy(r => r.TextType?.Name ?? website)
                    .ToDictionary(r => r.Key, r => r.First().TextType?.Subfolder ?? string.Empty);

                foreach (var t in types.Keys)
                {
                    IList<TextDTO> textsPerType = new List<TextDTO>();

                    if (t.Equals(website, StringComparison.InvariantCultureIgnoreCase))
                    {
                        textsPerType = resources.Where(r => r.TextType == null)
                        .ToList();
                    }
                    else
                    {
                        textsPerType = resources.Where(r => r.TextType != null && r.TextType.Name == t)
                        .ToList();
                    }


                    var fileName = string.IsNullOrWhiteSpace(t) ? website : t;
                    var file = new byte[0];
                    switch (type)
                    {
                        case PublicationFormat.XML:
                            file = CreateXmlResourcesDocument(textsPerType);
                            break;

                        case PublicationFormat.JSON:
                            file = CreateJsonResourcesDocument(textsPerType);
                            break;

                        default:
                            throw new NotSupportedException(type.ToString());
                    }

                    var folder = new StringBuilder(langDir.FullName);
                    var subfolder = types[t];
                    if (!string.IsNullOrWhiteSpace(subfolder))
                    {
                        folder.Append($"\\{subfolder}");

                        DirectoryInfo subfolderInfo = new DirectoryInfo(folder.ToString());
                        if (!subfolderInfo.Exists)
                        {
                            subfolderInfo.Create();
                        }
                    }

                    string filePath = string.Format("{0}\\{1}.{2}", folder, fileName, type.ToString().ToLower());
                    //file.Save(filePath);
                    System.IO.File.WriteAllBytes(filePath, file);
                }
            }
        }

        private async Task GenerateFilesByWebsite(string website, DateTime filesGenerationDate, string directoryPath)
        {

            var mainFolderRs
                = await _fileManagerService.GetAllFoldersByWebsite(website);

            if (mainFolderRs.Success)
            {
                var mainFolder = mainFolderRs.Value;

                await CreateFolderAndFiles(website, mainFolder, directoryPath, filesGenerationDate);
            }
        }

        private async Task CreateFolderAndFiles(string website, FolderDTO folder, string folderPath, DateTime filesGenerationDate)
        {
            DirectoryInfo directory = new DirectoryInfo(folderPath);
            if (!directory.Exists)
            {
                directory.Create();
            }

            var filesRs = await _fileManagerService.GetFilesByFolder(website, folder.Id.Value, true, filesGenerationDate);
            if (filesRs.Success)
            {
                var files = filesRs.Value;

                foreach (var f in files)
                {
                    var fileName = $"{folderPath}\\{f.Filename}";
                    var file = new FileInfo(fileName);
                    using FileStream stream = file.OpenWrite();
                    stream.Write(f.Content, 0, f.Content.Length);
                    stream.Flush();
                    stream.Close();
                }

                foreach (var sub in folder.SubFolders)
                {
                    string subfolderPath = $"{folderPath}\\{sub.Name}";
                    await CreateFolderAndFiles(website, sub, subfolderPath, filesGenerationDate);
                }
            }
        }

        private void GeneratePDFPagesFileBySite(string tenant, string website, string site, IDictionary<string, List<PageDTO>> contentsPerLanguages, string directoryPath)
        {
            //if (string.IsNullOrEmpty(directoryPath))
            //{
            //    directoryPath = _options.LocalDirectory;
            //}

            //DirectoryInfo directory = new DirectoryInfo(directoryPath);
            //if (!directory.Exists)
            //{
            //    directory.Create();
            //}
            //foreach (var lang in contentsPerLanguages.Keys)
            //{
            //    var langDir = directory.CreateSubdirectory(lang);
            //    var folder = langDir.FullName;
            //    var contents = contentsPerLanguages[lang];

            //    IDictionary<int, string> types =
            //        contents
            //        .GroupBy(r => r.PageType.Id.Value)
            //        .ToDictionary(r => r.Key, r => r.First().Tipologia.Subfolder);

            //    foreach (var t in types.Keys)
            //    {
            //        IList<PageDTO> contentsPerTypePDF =
            //            contents
            //            .Where(c => c.PageType.Id == t && c.GeneratePdf == true && !string.IsNullOrEmpty(c.Tipologia.PrintElementName))
            //            .ToList();

            //        if (contentsPerTypePDF.Count > 0)
            //        {
            //            //var subfolder = types[t];
            //            //if (!string.IsNullOrEmpty(subfolder))
            //            //{
            //            //    folder += "\\" + subfolder;

            //            //    DirectoryInfo subfolderInfo = new DirectoryInfo(folder);
            //            //    if (!subfolderInfo.Exists)
            //            //    {
            //            //        subfolderInfo.Create();
            //            //    }
            //            //}

            //            PDFUtility pdfUtility = new PDFUtility();
            //            foreach (var c in contentsPerTypePDF)
            //            {
            //                string pdfFilePath = string.Format("{0}\\{1}.pdf", folder, c.Titolo.Replace(' ', '_'));
            //                pdfUtility.PrintFromXML(c.Xml, c.Tipologia.PrintElementName, pdfFilePath, tenant, vendor, instance.InstanceId, lang);
            //            }
            //        }
            //    }
            //}
        }

        private async Task GeneratePagesFileBySite(string tenant, string website, string site, IDictionary<string, List<PageDTO>> contentsPerLanguages, string directoryPath, string baseUrl, CdnServerDTO cdnServer, PublicationFormat type)
        {
            if (string.IsNullOrWhiteSpace(directoryPath))
            {
                directoryPath = _options.LocalDirectory;
            }

            DirectoryInfo directory = new DirectoryInfo(directoryPath);
            if (!directory.Exists)
            {
                directory.Create();
            }

            foreach (var lang in contentsPerLanguages.Keys)
            {
                var langDir = directory.CreateSubdirectory(lang);

                var contents = contentsPerLanguages[lang];

                IDictionary<int, string> types =
                    contents
                    .GroupBy(r => r.PageType.Id.Value)
                    .ToDictionary(r => r.Key, r => r.First().PageType.Subfolder);

                foreach (var t in types.Keys)
                {
                    IList<PageDTO> contentsPerType =
                        contents.Where(r => r.PageType.Id == t)
                        .ToList();

                    var xmlSchema = contentsPerType.First().PageType.Schema;
                    var structure = _pageSchemaService.GetContentStructure(xmlSchema);
                    var fileName = contentsPerType.First().PageType.OutputFilename;

                    var file = new byte[0];

                    switch (type)
                    {
                        case PublicationFormat.XML:
                            file = await _pageSchemaService.CreateXmlContentsDocument(structure, contentsPerType, tenant, website, site, lang, baseUrl, cdnServer);
                            break;

                        case PublicationFormat.JSON:
                            file = await _pageSchemaService.CreateJsonContentsDocument(structure, contentsPerType, tenant, website, site, lang, baseUrl, cdnServer);
                            break;

                        default:
                            throw new NotSupportedException(type.ToString());
                    }

                    var folder = new StringBuilder(langDir.FullName);
                    var subfolder = types[t];
                    if (!string.IsNullOrWhiteSpace(subfolder))
                    {
                        folder.Append($"\\{subfolder}");

                        var subfolderInfo = new DirectoryInfo(folder.ToString());
                        if (!subfolderInfo.Exists)
                        {
                            subfolderInfo.Create();
                        }
                    }

                    string filepath = string.Format("{0}\\{1}.{2}", folder, fileName, type.ToString().ToLower());
                    //xml.Save(xmlFilePath);
                    System.IO.File.WriteAllBytes(filepath, file);
                }
            }
        }

        private byte[] CreateXmlResourcesDocument(IList<TextDTO> texts)
        {
            XDocument doc = new XDocument();
            XDeclaration declaration = new XDeclaration("1.0", "utf-8", string.Empty);
            doc.Declaration = declaration;
            XElement root = new XElement("resources");
            foreach (var r in texts)
            {
                var element = new XElement("resource");
                element.SetAttributeValue("name", r.Name);

                if (!string.IsNullOrEmpty(r.Country))
                {
                    element.SetAttributeValue("country", r.Country);
                }
                XCData cdata = new XCData(r.TextRevision?.Content ?? string.Empty);
                element.Add(cdata);

                root.Add(element);
            }
            doc.Add(root);
            var file = doc.ToString();

            return Encoding.UTF8.GetBytes(file);
        }

        private byte[] CreateJsonResourcesDocument(IList<TextDTO> texts)
        {
            var list = texts
                .Select(rt => new
                {
                    Name = rt.Name,
                    Text = rt.TextRevision?.Content ?? string.Empty,
                    Country = rt.Country,
                }).ToList();

            var file = JsonSerializer.Serialize(list);
            return Encoding.UTF8.GetBytes(file); ;
        }

        #endregion
    }
}
