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

            // whatever happens after this point, do not leave a (possibly huge) partial export behind:
            // every MassTransit retry would add another copy
            try
            {

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
                            var siteDirectory = textSubDirectory.CreateSubdirectory(s.Site.ToUpperInvariant());

                            if (textsByWebsite.TryGetValue(s.Site, out var textsPerSite))
                            {
                                var dict = textsPerSite.GroupBy(t => t.Language).ToFrozenDictionary(k => k.Key, v => v.ToList().AsReadOnly());
                                await GenerateTextsFileBySite(website, dict, siteDirectory.FullName, publishType);
                            }
                        }
                    }
                    else
                    {
                        TryDeleteDirectory(currentDirectory.FullName);
                        return OperationResult<string>.MakeFailure(textsByWebsiteRs.Errors);
                    }
                }

                if (exportType == PublicationType.All || exportType == PublicationType.Pages)
                {
                    var pageByWebsiteRs = await _pageService.GetPublishablePagesByWebsite(website, siteLanguages);
                // shared by every document of the export: widgets, links and page schemas are resolved once
                var widgetCache = new WidgetResolutionCache();
                var structures = new Dictionary<int, TrinityText.Business.Schema.PageSchema>();

                    if (pageByWebsiteRs.Success)
                    {
                        var pageByWebsite = pageByWebsiteRs.Value;
                        foreach (var s in sites)
                        {
                            var siteDirectory = textSubDirectory.CreateSubdirectory(s.Site.ToUpperInvariant());

                            if (pageByWebsite.TryGetValue(s.Site, out var textsPerSite))
                            {
                                var dict = textsPerSite.GroupBy(t => t.Language).ToFrozenDictionary(k => k.Key, v => v.ToList().AsReadOnly());
                                await GeneratePagesFileBySite(tenant, website, s.Site, dict, siteDirectory.FullName, string.Empty, cdnServer, publishType, widgetCache, structures);
                            }
                        }

                    }
                    else
                    {
                        TryDeleteDirectory(currentDirectory.FullName);
                        return OperationResult<string>.MakeFailure(pageByWebsiteRs.Errors);
                    }
                }

                if (exportType == PublicationType.All || exportType == PublicationType.Files)
                {
                    await GenerateFilesByWebsite(website, filesGenerationDate, currentDirectory.FullName);
                }

                await GenerateFileTimestamp(currentDirectory, user);

                // I/O-bound recursive deletion: the sequential version avoids unbounded Parallel.ForEach
                // thread-pool pressure; these trees are small (site/language folders).
                DeleteEmptySubdirectories(currentDirectory.FullName);

                if (compressFileOutput)
                {
                    var filePath = await _compressionFileService.CompressFolder(currentDirectory.FullName, basePath);
                    if (string.IsNullOrWhiteSpace(filePath))
                    {
                        throw new InvalidOperationException("The export folder could not be compressed");
                    }

                    currentDirectory.Delete(true);

                    return OperationResult<string>.MakeSuccess(filePath);
                }
                else
                {
                    return OperationResult<string>.MakeSuccess(currentDirectory.FullName);
                }
            }
            catch
            {
                TryDeleteDirectory(currentDirectory.FullName);
                throw;
            }
        }

        private void TryDeleteDirectory(string path)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, true);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unable to remove the export directory {path}", path);
            }
        }

        public static void DeleteEmptySubdirectories(string parentDirectory)
        {
            foreach (string directory in System.IO.Directory.GetDirectories(parentDirectory))
            {
                DeleteEmptySubdirectories(directory);
                if (!System.IO.Directory.EnumerateFileSystemEntries(directory).Any()) System.IO.Directory.Delete(directory, false);
            }
        }

        /// <summary>
        /// The payload is stored as free JSON: make sure it targets the same website as the publication
        /// and that tenant/website are safe path segments before they are used to build paths.
        /// </summary>
        private static void EnsurePayloadMatchesPublication(PayloadDTO payload, PublicationDTO setting)
        {
            if (payload == null || !string.Equals(payload.Website, setting.Website, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Payload website does not match the publication website");
            }

            PathSafety.EnsureValidSegment(payload.Website, nameof(payload.Website));
            PathSafety.EnsureValidSegment(payload.Tenant, nameof(payload.Tenant));

            // site and language codes become directory names of the export
            foreach (var site in payload.Sites ?? [])
            {
                PathSafety.EnsureValidSegment(site.Site, "Site");
                foreach (var language in site.Languages ?? [])
                {
                    PathSafety.EnsureValidSegment(language, "Language");
                }
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
                EnsurePayloadMatchesPublication(payload, setting);

                _logger.LogInformation("GenerateWebsite {website} started", website);

                var filePathRs = await CreateExportFile(setting.Id.Value, payload, dataType, setting.Format, filterDate, true, setting.CreationUser, cdnServer);

                if (filePathRs.Success)
                {
                    var filePath = filePathRs.Value;
                    // the ZIP is streamed from disk to the database: it is never fully loaded in memory
                    OperationResult updateRs;
                    try
                    {
                        await using var zipStream = new System.IO.FileStream(filePath, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.Read, 81920, System.IO.FileOptions.Asynchronous | System.IO.FileOptions.SequentialScan);
                        updateRs = await _publicationService.UpdateWithZipStream(setting.Id.Value, PublicationStatus.Generating, "Zip file completed", zipStream);
                    }
                    finally
                    {
                        System.IO.File.Delete(filePath);
                    }

                    if (updateRs.Success == false)
                    {
                        result.AppendErrors(updateRs.Errors);
                        _logger.LogError("GenerateWebsite {website} end with errors: {errors}", website, string.Join(",", updateRs.Errors.Select(s => s.Description)));
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
                if (setting.ZipFile != null && setting.ZipFile.Length > 0)
                {
                    await _compressionFileService.DecompressFolder(basePath, setting.ZipFile);
                }
                else
                {
                    // the ZIP is not held in memory: it flows from the database to a temporary file, then it is extracted
                    var zipPath = basePath + ".zip";
                    Directory.CreateDirectory(Path.GetDirectoryName(zipPath));
                    try
                    {
                        await using (var zipStream = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, FileOptions.Asynchronous))
                        {
                            var copyRs = await _publicationService.CopyZipTo(setting.Id.Value, zipStream);
                            if (!copyRs.Success)
                            {
                                throw new InvalidOperationException("The publication has no ZIP content to publish");
                            }
                        }

                        await _compressionFileService.DecompressFile(basePath, zipPath);
                    }
                    finally
                    {
                        if (File.Exists(zipPath))
                        {
                            File.Delete(zipPath);
                        }
                    }
                }

                var payload = setting.Payload;
                EnsurePayloadMatchesPublication(payload, setting);

                var server = setting.FtpServer;
                var d = new DirectoryInfo(basePath);

                var uploadRs = await _transferServiceCoordinator.Upload(payload.Tenant, setting.Website, d, server.Host, server.Username, server.Password, server.Port);

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

        private static async Task GenerateFileTimestamp(DirectoryInfo currentDirectory, string username)
        {
            var textFile = Path.Combine(currentDirectory.FullName, "trinity-text.txt");
            var text = $"{username}|{DateTime.Now.ToString("dd-MM-yyyy|HH-mm-ss", System.Globalization.CultureInfo.InvariantCulture)}";

            await System.IO.File.WriteAllTextAsync(textFile, text);
        }

        private async Task GenerateTextsFileBySite(string website, FrozenDictionary<string, ReadOnlyCollection<TextDTO>> textsPerLanguage, string directoryPath, PublicationFormat type)
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

                // grouped once: scanning every text for every type was O(types x texts)
                var untypedTexts = resources.Where(r => r.TextType == null).ToList().AsReadOnly();
                var typedTexts = resources.Where(r => r.TextType != null).ToLookup(r => r.TextType.Name);

                foreach (var t in types.Keys)
                {
                    var textsPerType = t.Equals(website, StringComparison.InvariantCultureIgnoreCase) ?
                        untypedTexts :
                        typedTexts[t].ToList().AsReadOnly();

                    var fileName = string.IsNullOrWhiteSpace(t) ? website : t;
                    var file = Array.Empty<byte>();
                    file = type switch
                    {
                        PublicationFormat.XML => CreateXmlResourcesDocument(textsPerType),
                        PublicationFormat.JSON => CreateJsonResourcesDocument(textsPerType),
                        _ => throw new NotSupportedException(type.ToString()),
                    };
                    var folder = ResolveOutputFolder(directory.FullName, langDir.FullName, types[t]);

                    var filePath = PathSafety.EnsureWithinRoot(directory.FullName, Path.Combine(folder, $"{fileName}.{type.ToString().ToLowerInvariant()}"));
                    await System.IO.File.WriteAllBytesAsync(filePath, file);
                }
            }
        }

        private async Task GenerateFilesByWebsite(string website, DateTime filesGenerationDate, string directoryPath)
        {

            var mainFolderRs
                = await _fileManagerService.GetAllFoldersByWebsite(website);

            if (!mainFolderRs.Success)
            {
                // a publication without (some of) its files must not be reported as successful
                throw new InvalidOperationException($"Unable to read the folders of website {website}: {string.Join(",", mainFolderRs.Errors.Select(e => e.Description))}");
            }

            await CreateFolderAndFiles(website, mainFolderRs.Value, directoryPath, filesGenerationDate);
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
                // metadata first, then one blob at a time: memory stays bounded by the largest file, not by the folder
                var filesRs = await _fileManagerService.GetFilesByFolder(website, folder.Id.Value, false, filesGenerationDate);
                if (!filesRs.Success)
                {
                    throw new InvalidOperationException($"Unable to read the files of folder {folder.Name} ({folder.Id}): {string.Join(",", filesRs.Errors.Select(e => e.Description))}");
                }

                foreach (var f in filesRs.Value)
                {
                    var fileName = PathSafety.EnsureWithinRoot(directory.FullName, Path.Combine(directory.FullName, f.Filename));

                    var contentRs = await _fileManagerService.GetFileContent(f.Id);
                    if (!contentRs.Success)
                    {
                        throw new InvalidOperationException($"Unable to read the content of file {f.Filename} ({f.Id})");
                    }

                    await File.WriteAllBytesAsync(fileName, contentRs.Value);
                }

                foreach (var sub in folder.SubFolders)
                {
                    var subfolderPath = PathSafety.EnsureWithinRoot(directory.FullName, Path.Combine(directory.FullName, sub.Name));
                    await CreateFolderAndFiles(website, sub, subfolderPath, filesGenerationDate);
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

        private async Task GeneratePagesFileBySite(string tenant, string website, string site, FrozenDictionary<string, ReadOnlyCollection<PageDTO>> contentsPerLanguages, string directoryPath, string baseUrl, CdnServerDTO cdnServer, PublicationFormat type, WidgetResolutionCache widgetCache, Dictionary<int, TrinityText.Business.Schema.PageSchema> structures)
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

                    var documentSchema = contentsPerType.First().PageType.Schema;
                    var fileName = ResolvePageFileName(contentsPerType.First().PageType);
                    if (!structures.TryGetValue(t, out var structure))
                    {
                        structure = _pageSchemaService.GetContentStructure(documentSchema);
                        structures[t] = structure;
                    }
                    var file = type switch
                    {
                        PublicationFormat.XML => await _pageSchemaService.CreateXmlContentsDocument(structure, contentsPerType, tenant, website, site, lang, baseUrl, cdnServer, widgetCache),
                        PublicationFormat.JSON => await _pageSchemaService.CreateJsonContentsDocument(structure, contentsPerType, tenant, website, site, lang, baseUrl, cdnServer, widgetCache),
                        _ => throw new NotSupportedException(type.ToString()),
                    };
                    var folder = ResolveOutputFolder(directory.FullName, langDir.FullName, types[t]);

                    var filepath = PathSafety.EnsureWithinRoot(directory.FullName, Path.Combine(folder, $"{fileName}.{type.ToString().ToLowerInvariant()}"));
                    await System.IO.File.WriteAllBytesAsync(filepath, file);
                }
            }
        }

        // language directory (+ optional subfolder), confined to the export root; created when missing
        private static string ResolveOutputFolder(string root, string languageDirectory, string subfolder)
        {
            var folder = string.IsNullOrWhiteSpace(subfolder)
                ? languageDirectory
                : PathSafety.EnsureWithinRoot(root, Path.Combine(languageDirectory, subfolder));

            Directory.CreateDirectory(folder);
            return folder;
        }

        // an empty OutputFilename would make every page type write ".xml" over each other
        private static string ResolvePageFileName(PageTypeDTO pageType)
        {
            if (!string.IsNullOrWhiteSpace(pageType.OutputFilename))
            {
                return pageType.OutputFilename;
            }

            return PathSafety.IsValidSegment(pageType.Name) ? pageType.Name : $"pagetype_{pageType.Id}";
        }

        // characters that XML 1.0 cannot represent (not even in CDATA) would abort the whole export
        private static string RemoveInvalidXmlChars(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            foreach (var c in value)
            {
                if (!System.Xml.XmlConvert.IsXmlChar(c) && !char.IsSurrogate(c))
                {
                    return string.Concat(value.Where(ch => System.Xml.XmlConvert.IsXmlChar(ch) || char.IsSurrogate(ch)));
                }
            }

            return value;
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
                element.SetAttributeValue("name", RemoveInvalidXmlChars(r.Name));

                if (!string.IsNullOrWhiteSpace(r.Country))
                {
                    element.SetAttributeValue("country", r.Country);
                }
                var cdata = new XCData(RemoveInvalidXmlChars(r.TextRevision?.Content ?? string.Empty));
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
