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

        public MassTransitPublicationSupportService(ITextService textService, IPageService pageService, IPageSchemaService pageSchemaService,
            IFileManagerService fileManagerService, ICompressionFileService compressionFileService, ITransferServiceCoordinator transferServiceCoordinator,
            IOptions<PublicationSupportOptions> options)
        {
            _textService = textService;
            _pageService = pageService;
            _pageSchemaService = pageSchemaService;
            _fileManagerService = fileManagerService;
            _compressionFileService = compressionFileService;
            _transferServiceCoordinator = transferServiceCoordinator;
            _options = options.Value;
        }

        public async Task<OperationResult<string>> ExportFiles(int id, PayloadDTO payload, PublicationType exportType, PublishType publishType, DateTime filesGenerationDate, bool compressFileOutput, string user, CdnServerDTO cdnServer)
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

            var currentDirectory = baseDirectory.CreateSubdirectory(string.Format("{0}_{1}_{2:dd-MM-yyyy_HH-mm-ss}", website, id, DateTime.Now));

            var textSubDirectory = currentDirectory.CreateSubdirectory("Text");
            var filesSubDirectory = currentDirectory.CreateSubdirectory("Files");

            foreach (var i in sites)
            {
                if (exportType == PublicationType.All || exportType == PublicationType.Resources)
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

                if (exportType == PublicationType.All || exportType == PublicationType.Contents || exportType == PublicationType.PDF)
                {
                    var pagesPerSiteRs = await _pageService.GetPublishablePages(website, i.Site, i.Languages.ToArray());

                    if (pagesPerSiteRs.Success)
                    {
                        var pagesPerSite = pagesPerSiteRs.Value;
                        if (exportType == PublicationType.All || exportType == PublicationType.Contents)
                        {
                            var instanceDirectory = textSubDirectory.CreateSubdirectory(i.Site.ToUpper());
                            await GeneratePagesFileBySite(tenant, website, i.Site, pagesPerSite, instanceDirectory.FullName, string.Empty, cdnServer, publishType);
                        }

                        if (exportType == PublicationType.All || exportType == PublicationType.PDF)
                        {
                            var instanceDirectory = filesSubDirectory.CreateSubdirectory(i.Site.ToUpper());
                            GeneratePDFPagesFileBySite(tenant, website, i.Site, pagesPerSite, instanceDirectory.FullName);
                        }
                        else
                        {
                            return OperationResult<string>.MakeFailure(pagesPerSiteRs.Errors);
                        }
                    }
                }
            }

            if (exportType == PublicationType.All || exportType == PublicationType.Files)
            {
                await GenerateFilesByWebsite(website, filesGenerationDate, currentDirectory.FullName);
            }

            GenerateFileTimestamp(currentDirectory, user);

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

        private void GenerateFileTimestamp(DirectoryInfo currentDirectory, string username)
        {
            var textFile = currentDirectory + "\\trinity-text.txt";
            var text = string.Format("{0}|{1:dd-MM-yyyy|HH-mm-ss}", username, DateTime.Now);

            System.IO.File.WriteAllText(textFile, text);
        }



        public async Task<string> Generate(PublicationDTO filesGenerationSettings)
        {
            StringBuilder operationsLog = new StringBuilder();
            try
            {
                var vendor = filesGenerationSettings.Website;
                var exportType = filesGenerationSettings.PublicationType;
                var filesGenerationDate = filesGenerationSettings.FilterDataDate;
                var cdnServer = filesGenerationSettings.CdnServer;

                var payload = filesGenerationSettings.Payload;


                var filePathRs = await ExportFiles(filesGenerationSettings.Id.Value, payload, exportType, filesGenerationSettings.PublishType, filesGenerationDate, true, filesGenerationSettings.Utente, cdnServer);

                if (filePathRs.Success)
                {
                    var filePath = filePathRs.Value;
                    byte[] byteArray = System.IO.File.ReadAllBytes(filePath);
                    System.IO.File.Delete(filePath);

                    filesGenerationSettings.ZipFile = byteArray;
                    filesGenerationSettings.HasZipFile = true;
                    filesGenerationSettings.Status = "Zip file completed";
                    filesGenerationSettings.StatusCode = PublicationStatus.Generated;
                }
                else
                {
                    foreach (var er in filePathRs.Errors)
                    {
                        operationsLog.AppendLine($"{er.Context}: {er.Description}");
                    }
                }
            }
            catch (Exception e)
            {
                operationsLog.AppendLine("--ex: " + e.Message);
                if (e.InnerException != null && !string.IsNullOrEmpty(e.InnerException.Message))
                {
                    operationsLog.AppendLine("--innerex: " + e.InnerException.Message);
                }
            }
            return operationsLog.ToString();
        }

        public async Task<string> Publish(PublicationDTO filesGenerationSettings)
        {
            var basePath = _options.LocalDirectory + "/Uploading/" + Guid.NewGuid();
            StringBuilder operationsLog = new StringBuilder();
            try
            {
                await _compressionFileService.DecompressFolder(basePath, filesGenerationSettings.ZipFile);

                var payload = filesGenerationSettings.Payload;
                //var instances = JsonConvert.DeserializeObject<IList<InstanceDTO>>(payload);

                //var tenant = instances.Select(i => i.TenantId).SingleOrDefault();


                var server = filesGenerationSettings.FtpServer;
                var d = new DirectoryInfo(basePath);

                var publishLog = await _transferServiceCoordinator.Upload(payload.Tenant, filesGenerationSettings.Website, d, server.Host, server.Username, server.Password);

                if (!string.IsNullOrEmpty(publishLog))
                {
                    operationsLog.AppendLine(publishLog);
                }
            }
            catch (Exception e)
            {
                operationsLog.AppendLine("--ex: " + e.Message);
                if (e.InnerException != null && !string.IsNullOrEmpty(e.InnerException.Message))
                {
                    operationsLog.AppendLine("--innerex: " + e.InnerException.Message);
                }
            }
            finally
            {
                DirectoryInfo folder = new DirectoryInfo(basePath);
                if (folder.Exists)
                {
                    folder.Delete(true);
                }
            }
            return operationsLog.ToString();
        }

        #region private methods

        private void GenerateTextsFileBySite(string website, IDictionary<string, List<TextDTO>> textsPerLanguage, string directoryPath, PublishType type)
        {
            if (string.IsNullOrEmpty(directoryPath))
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
                    .GroupBy(r => r.TextType.Name)
                    .ToDictionary(r => r.Key, r => r.First().TextType.Subfolder);

                foreach (var t in types.Keys)
                {
                    IList<TextDTO> textsPerType =
                        resources.Where(r => r.TextType.Name == t)
                        .ToList();
                    var fileName = string.IsNullOrWhiteSpace(t) ? website : t;
                    var file = new byte[0];
                    switch (type)
                    {
                        case PublishType.XML:
                            file = CreateXmlResourcesDocument(textsPerType);
                            break;

                        case PublishType.JSON:
                            file = CreateJsonResourcesDocument(textsPerType);
                            break;

                        default:
                            throw new NotSupportedException(type.ToString());
                            break;
                    }

                    var folder = langDir.FullName;
                    var subfolder = types[t];
                    if (!string.IsNullOrEmpty(subfolder))
                    {
                        folder += "\\" + subfolder;

                        DirectoryInfo subfolderInfo = new DirectoryInfo(folder);
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
                    var fileName = folderPath + "\\" + f.Filename;
                    FileInfo file = new FileInfo(fileName);
                    using (FileStream stream = file.OpenWrite())
                    {
                        stream.Write(f.Content, 0, f.Content.Length);
                        stream.Flush();
                        stream.Close();
                    }
                }

                foreach (var sub in folder.SubFolders)
                {
                    string subfolderPath = folderPath + "\\" + sub.Name;
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

        private async Task GeneratePagesFileBySite(string tenant, string website, string site, IDictionary<string, List<PageDTO>> contentsPerLanguages, string directoryPath, string baseUrl, CdnServerDTO cdnServer, PublishType type)
        {
            if (string.IsNullOrEmpty(directoryPath))
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
                        case PublishType.XML:
                            file = await _pageSchemaService.CreateXmlContentsDocument(structure, contentsPerType, tenant, website, site, lang, baseUrl, cdnServer);
                            break;

                        case PublishType.JSON:
                            file = await _pageSchemaService.CreateJsonContentsDocument(structure, contentsPerType, tenant, website, site, lang, baseUrl, cdnServer);
                            break;

                        default:
                            throw new NotSupportedException(type.ToString());
                            break;
                    }

                    var folder = langDir.FullName;
                    var subfolder = types[t];
                    if (!string.IsNullOrEmpty(subfolder))
                    {
                        folder += "\\" + subfolder;

                        DirectoryInfo subfolderInfo = new DirectoryInfo(folder);
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
                XCData cdata = new XCData(r.TextRevision.Content);
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
                    Text = rt.TextRevision.Content,
                    Country = rt.Country,
                }).ToList();

            var file = JsonSerializer.Serialize(list);
            return Encoding.UTF8.GetBytes(file); ;
        }

        #endregion
    }
}
