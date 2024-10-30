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
using System.Collections.Frozen;
using System.Collections.ObjectModel;
using Microsoft.Extensions.Logging;

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

        private readonly ILogger<MassTransitPublicationSupportService> _logger;

        public MassTransitPublicationSupportService(IPublicationService publicationService, ITextService textService, IPageService pageService, IPageSchemaService pageSchemaService,
            IFileManagerService fileManagerService, ICompressionFileService compressionFileService, ITransferServiceCoordinator transferServiceCoordinator,
            ILogger<MassTransitPublicationSupportService> logger,
            IOptions<PublicationSupportOptions> options)
        {
            _publicationService = publicationService;
            _textService = textService;
            _pageService = pageService;
            _pageSchemaService = pageSchemaService;
            _fileManagerService = fileManagerService;
            _compressionFileService = compressionFileService;
            _transferServiceCoordinator = transferServiceCoordinator;
            _logger = logger;
            _options = options.Value;
        }

        public async Task<OperationResult<string>> CreateExportFile(int id, PayloadDTO payload, PublicationType exportType, PublicationFormat publishType, DateTime filesGenerationDate, bool compressFileOutput, string user, CdnServerDTO cdnServer)
        {
            var basePath = _options.LocalDirectory;
            var website = payload.Website;
            var tenant = payload.Tenant;
            var sites = payload.Sites.AsReadOnly();
            var textTypes = payload.TextTypes.AsReadOnly();


            var baseDirectory = new DirectoryInfo(basePath);
            if (!baseDirectory.Exists)
            {
                baseDirectory.Create();
            }

            var currentDirectory = baseDirectory.CreateSubdirectory($"{website}_{id}_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}");

            var textSubDirectory = currentDirectory.CreateSubdirectory("Text");
            //var filesSubDirectory = currentDirectory.CreateSubdirectory("Files");

            var allLanguages = sites.SelectMany(s => s.Languages).Distinct().ToArray();

            var siteLanguages = sites.ToDictionary(s => s.Site, v => v.Languages);

            if (exportType == PublicationType.All || exportType == PublicationType.Texts)
            {
                var textsByWebsiteRs = await _textService.GetPublishableTextsByWebsite(website, siteLanguages, textTypes);

                if (textsByWebsiteRs.Success)
                {
                    var textsByWebsite = textsByWebsiteRs.Value;
                    foreach (var s in sites)
                    {
                        var siteDirectory = textSubDirectory.CreateSubdirectory(s.Site.ToUpper());

                        if (textsByWebsite.TryGetValue(s.Site, out var textsPerSite))
                        {
                            var dict = textsPerSite.GroupBy(t => t.Language).ToFrozenDictionary(k => k.Key, v => v.ToList().AsReadOnly());
                            GenerateTextsFileBySite(website, dict, siteDirectory.FullName, publishType);
                        }
                    }
                }
                else
                {
                    return OperationResult<string>.MakeFailure(textsByWebsiteRs.Errors);
                }
            }

            if (exportType == PublicationType.All || exportType == PublicationType.Pages)
            {
                var pageByWebsiteRs = await _pageService.GetPublishablePagesByWebsite(website, siteLanguages);

                if (pageByWebsiteRs.Success)
                {
                    var pageByWebsite = pageByWebsiteRs.Value;
                    foreach (var s in sites)
                    {
                        var siteDirectory = textSubDirectory.CreateSubdirectory(s.Site.ToUpper());

                        if (pageByWebsite.TryGetValue(s.Site, out var textsPerSite))
                        {
                            var dict = textsPerSite.GroupBy(t => t.Language).ToFrozenDictionary(k => k.Key, v => v.ToList().AsReadOnly());
                            await GeneratePagesFileBySite(tenant, website, s.Site, dict, siteDirectory.FullName, string.Empty, cdnServer, publishType);
                        }
                    }

                }
                else
                {
                    return OperationResult<string>.MakeFailure(pageByWebsiteRs.Errors);
                }
            }

            if (exportType == PublicationType.All || exportType == PublicationType.Files)
            {
                await GenerateFilesByWebsite(website, filesGenerationDate, currentDirectory.FullName);
            }

            GenerateFileTimestamp(currentDirectory, user);

            //DeleteEmptySubdirectories(currentDirectory.FullName);
            FastDeleteEmptySubdirectories(currentDirectory.FullName);

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

        public static void FastDeleteEmptySubdirectories(string parentDirectory)
        {
            System.Threading.Tasks.Parallel.ForEach(System.IO.Directory.GetDirectories(parentDirectory), directory =>
            {
                FastDeleteEmptySubdirectories(directory);
                if (!System.IO.Directory.EnumerateFileSystemEntries(directory).Any()) System.IO.Directory.Delete(directory, false);
            });
        }

        public static void DeleteEmptySubdirectories(string parentDirectory)
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
            var website = setting.Website;
            var dataType = setting.DataType;
            var filterDate = setting.FilterDataDate;
            var cdnServer = setting.CdnServer;
            try
            {
                var payload = setting.Payload;

                _logger.LogInformation("GenerateWebsite {website} started", website);

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
                        _logger.LogError("GenerateWebsite {website} end with errors: {errors}", website, string.Join(",", filePathRs.Errors.Select(s => s.Description)));
                    }
                }
                else
                {
                    result.AppendErrors(filePathRs.Errors);
                    _logger.LogError("GenerateWebsite {website} end with errors: {errors}", website, string.Join(",", filePathRs.Errors.Select(s => s.Description)));
                }
            }
            catch (Exception ex)
            {
                result.AppendError("GENERATE", ex.Message);
                _logger.LogError(ex, "GenerateWebsite {website} end with error", website);
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
                    var folder = new DirectoryInfo(basePath);
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

        private static void GenerateFileTimestamp(DirectoryInfo currentDirectory, string username)
        {
            var textFile = $"{currentDirectory}\\trinity-text.txt";
            var text = $"{username}|{DateTime.Now:dd-MM-yyyy|HH-mm-ss}";

            System.IO.File.WriteAllText(textFile, text);
        }

        private void GenerateTextsFileBySite(string website, FrozenDictionary<string, ReadOnlyCollection<TextDTO>> textsPerLanguage, string directoryPath, PublicationFormat type)
        {
            if (string.IsNullOrWhiteSpace(directoryPath))
            {
                directoryPath = _options.LocalDirectory;
            }

            var directory = new DirectoryInfo(directoryPath);
            if (!directory.Exists)
            {
                directory.Create();
            }

            foreach (var lang in textsPerLanguage.Keys)
            {
                var langDir = directory.CreateSubdirectory(lang);

                var resources = textsPerLanguage[lang];

                var types =
                    resources
                    .GroupBy(r => r.TextType?.Name ?? website)
                    .ToFrozenDictionary(r => r.Key, r => r.First().TextType?.Subfolder ?? string.Empty);

                foreach (var t in types.Keys)
                {
                    var textsPerType = t.Equals(website, StringComparison.InvariantCultureIgnoreCase) ?
                        resources.Where(r => r.TextType == null).ToList().AsReadOnly() :
                        resources.Where(r => r.TextType != null && r.TextType.Name == t).ToList().AsReadOnly();

                    var fileName = string.IsNullOrWhiteSpace(t) ? website : t;
                    var file = Array.Empty<byte>();
                    file = type switch
                    {
                        PublicationFormat.XML => CreateXmlResourcesDocument(textsPerType),
                        PublicationFormat.JSON => CreateJsonResourcesDocument(textsPerType),
                        _ => throw new NotSupportedException(type.ToString()),
                    };
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

                    var filePath = $"{folder}\\{fileName}.{type.ToString().ToLower()}";
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
            var directory = new DirectoryInfo(folderPath);
            if (!directory.Exists)
            {
                directory.Create();
            }

            if (folder != null && (folder?.Id.HasValue ?? false))
            {
                var filesRs = await _fileManagerService.GetFilesByFolder(website, folder.Id.Value, true, filesGenerationDate);
                if (filesRs.Success)
                {
                    var files = filesRs.Value;

                    foreach (var f in files)
                    {
                        var fileName = $"{folderPath}\\{f.Filename}";

                        await File.WriteAllBytesAsync(fileName, f.Content);

                        //var file = new FileInfo(fileName);
                        //using FileStream stream = file.OpenWrite();
                        //stream.Write(f.Content, 0, f.Content.Length);
                        //stream.Flush();
                        //stream.Close();
                    }

                    foreach (var sub in folder.SubFolders)
                    {
                        string subfolderPath = $"{folderPath}\\{sub.Name}";
                        await CreateFolderAndFiles(website, sub, subfolderPath, filesGenerationDate);
                    }
                }
            }
        }

        //private static void GeneratePDFPagesFileBySite(string tenant, string website, string site, IDictionary<string, List<PageDTO>> contentsPerLanguages, string directoryPath)
        //{
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
        //}

        private async Task GeneratePagesFileBySite(string tenant, string website, string site, FrozenDictionary<string, ReadOnlyCollection<PageDTO>> contentsPerLanguages, string directoryPath, string baseUrl, CdnServerDTO cdnServer, PublicationFormat type)
        {
            if (string.IsNullOrWhiteSpace(directoryPath))
            {
                directoryPath = _options.LocalDirectory;
            }

            var directory = new DirectoryInfo(directoryPath);
            if (!directory.Exists)
            {
                directory.Create();
            }

            foreach (var lang in contentsPerLanguages.Keys)
            {
                var langDir = directory.CreateSubdirectory(lang);

                var contents = contentsPerLanguages[lang];

                var types =
                    contents
                    .GroupBy(r => r.PageType.Id.Value)
                    .ToFrozenDictionary(r => r.Key, r => r.First().PageType.Subfolder);

                foreach (var t in types.Keys)
                {
                    var contentsPerType =
                        contents.Where(r => r.PageType.Id == t)
                        .ToList()
                        .AsReadOnly();

                    var xmlSchema = contentsPerType.First().PageType.Schema;
                    var fileName = contentsPerType.First().PageType.OutputFilename;
                    var structure = _pageSchemaService.GetContentStructure(xmlSchema);
                    var file = type switch
                    {
                        PublicationFormat.XML => await _pageSchemaService.CreateXmlContentsDocument(structure, contentsPerType, tenant, website, site, lang, baseUrl, cdnServer),
                        PublicationFormat.JSON => await _pageSchemaService.CreateJsonContentsDocument(structure, contentsPerType, tenant, website, site, lang, baseUrl, cdnServer),
                        _ => throw new NotSupportedException(type.ToString()),
                    };
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

                    string filepath = $"{folder}\\{fileName}.{type.ToString().ToLower()}";
                    //xml.Save(xmlFilePath);
                    System.IO.File.WriteAllBytes(filepath, file);
                }
            }
        }

        private static byte[] CreateXmlResourcesDocument(IReadOnlyCollection<TextDTO> texts)
        {
            var doc = new XDocument();
            var declaration = new XDeclaration("1.0", "utf-8", string.Empty);
            doc.Declaration = declaration;
            var root = new XElement("resources");
            foreach (var r in texts)
            {
                var element = new XElement("resource");
                element.SetAttributeValue("name", r.Name);

                if (!string.IsNullOrWhiteSpace(r.Country))
                {
                    element.SetAttributeValue("country", r.Country);
                }
                var cdata = new XCData(r.TextRevision?.Content ?? string.Empty);
                element.Add(cdata);

                root.Add(element);
            }
            doc.Add(root);
            var file = doc.ToString(SaveOptions.DisableFormatting);

            return Encoding.UTF8.GetBytes(file);
        }

        private static byte[] CreateJsonResourcesDocument(IReadOnlyCollection<TextDTO> texts)
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
