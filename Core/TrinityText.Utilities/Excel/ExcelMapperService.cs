using Ganss.Excel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TrinityText.Business;

namespace TrinityText.Utilities.Excel
{
    public class ExcelMapperService : IExcelService
    {
        private readonly ExcelOptions _options;

        private readonly ITextTypeService _textTypeService;

        private readonly ILogger<ExcelMapperService> _logger;

        public ExcelMapperService(ITextTypeService textTypeService, IOptions<ExcelOptions> options, ILogger<ExcelMapperService> logger)
        {
            _textTypeService = textTypeService;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<byte[]> GetExcelFileStream(PageDTO[] list)
        {
            var pages = list
                .Select(l => new
                {
                    TITLE = l.Title,
                    WEBSITE = !string.IsNullOrWhiteSpace(l.Website) ? l.Website : "*",
                    SITE = !string.IsNullOrWhiteSpace(l.Site) ? l.Site : "*",
                    LANGUAGE = l.Language,
                    CONTENT = l.Content
                }).ToArray();


            return await CreateExcelFile(pages, "Pages");
        }

        public async Task<byte[]> GetExcelFileStream(WidgetDTO[] list)
        {
            var widgets = list
                .Select(l => new
                {
                    KEY = l.Key,
                    WEBSITE = !string.IsNullOrWhiteSpace(l.Website) ? l.Website : "*",
                    SITE = !string.IsNullOrWhiteSpace(l.Site) ? l.Site : "*",
                    LANGUAGE = l.Language,
                    CONTENT = l.Content
                }).ToArray();


            return await CreateExcelFile(widgets, "Widgets");
        }

        public async Task<byte[]> GetExcelFileStream(TextDTO[] list)
        {
            var texts = list
                .Select(l => new
                {
                    KEY = l.Name,
                    TYPE = l.TextType != null ? l.TextType.Name : "*",
                    WEBSITE = !string.IsNullOrWhiteSpace(l.Website) ? l.Website : "*",
                    SITE = !string.IsNullOrWhiteSpace(l.Site) ? l.Site : "*",
                    COUNTRY = !string.IsNullOrWhiteSpace(l.Country) ? l.Country : "*",
                    LANGUAGE = l.Language,
                    TEXT = l.TextRevision.Content
                }).ToArray();

            return await CreateExcelFile(texts, "Texts");
        }

        public async Task<byte[]> GetExcelFileStream(IDictionary<KeyValuePair<string, string>, TextDTO[]> textsForSiteLang)
        {
            var basePath = _options.TempDirectory;
            var separator = basePath.EndsWith("/") ? string.Empty : "/";
            var filePath = $@"{basePath}{separator}Translations{Guid.NewGuid()}.xlsx";
            try
            {
                var sheetNames = new List<string>();
                var sheetHeaders = new List<Dictionary<string, int?>>();
                var sheetCells = new List<Dictionary<KeyValuePair<int, int>, string>>();

                var sheetIndex = 0;
                var em = new ExcelMapper();
                foreach (var site in textsForSiteLang)
                {
                    var siteName = site.Key.Key;
                    var lang = site.Key.Value;

                    var sheetName = $"{siteName}-{lang}";

                    var list =
                        site.Value
                        .Select(l => new
                        {
                            KEY = l.Name,
                            TYPE = l.TextType != null ? l.TextType.Name : (!string.IsNullOrWhiteSpace(l.Site) ? l.Website : "*"),
                            WEBSITE = !string.IsNullOrWhiteSpace(l.Website) ? l.Website : "*",
                            SITE = !string.IsNullOrWhiteSpace(l.Site) ? l.Site : "*",
                            LANGUAGE = l.Language,
                            CONTENT = l.TextRevision.Content
                        }).ToArray();

                    await em.SaveAsync(filePath, list, sheetIndex);

                    sheetIndex++;
                }

                return await GetStream(filePath);
            }
            catch (Exception e)
            {
                throw e;
            }
            finally
            {
                var file = new FileInfo(filePath);

                if (file.Exists)
                {
                    file.Delete();
                }
            }
        }

        public async Task<TextDTO[]> GetTextsFromStream(string user, Stream fileStream)
        {
            var list = new List<TextDTO>();
            try
            {
                var typesRs = await _textTypeService.GetAll();

                if (typesRs.Success)
                {
                    var types = typesRs.Value;

                    var em = new ExcelMapper(fileStream);

                    var rows = em.Fetch();

                    foreach (var r in rows)
                    {
                        var key = r.KEY.Trim();
                        var typeName = r.TYPE.ToUpper().Trim();
                        var website = r.WEBSITE.Trim();
                        var site = r.SITE.Trim();
                        var country = r.COUNTRY.Trim();
                        var lang = r.LANGUAGE.Trim();
                        var text = r.TEXT.Trim();

                        if (!string.IsNullOrEmpty(key))
                        {
                            TextTypeDTO type = null;

                            var cont = true;

                            if (!"*".Equals(typeName, StringComparison.InvariantCultureIgnoreCase)
                                && !string.IsNullOrEmpty(typeName))
                            {
                                type = types
                                    .Where(t => t.Name.Equals(typeName, StringComparison.InvariantCultureIgnoreCase))
                                    .FirstOrDefault();

                                cont = type != null;
                            }

                            if (cont)
                            {
                                var dto = new TextDTO()
                                {
                                    Name = key.Trim().Replace(' ', '_'),
                                    Language = lang,
                                    TextRevision = new TextRevisionDTO()
                                    {
                                        Content = text,
                                        CreationUser = user,
                                        CreationDate = DateTime.Now
                                    },
                                    Active = true,
                                    TextTypeId = type != null ? type.Id : null,
                                    Website = "*".Equals(website, StringComparison.InvariantCultureIgnoreCase) ? null : website,
                                    Country = "*".Equals(country, StringComparison.InvariantCultureIgnoreCase) ? null : country,
                                    Site = "*".Equals(site, StringComparison.InvariantCultureIgnoreCase) ? null : site,
                                };

                                list.Add(dto);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"");
            }

            return list.ToArray();
        }

        private async Task<byte[]> CreateExcelFile(IEnumerable<dynamic> rows, string fileName)
        {
            var basePath = _options.TempDirectory;
            var separator = basePath.EndsWith("/") ? string.Empty : "/";
            var filePath = $@"{basePath}{separator}{fileName}{Guid.NewGuid()}.xlsx";
            try
            {
                var em = new ExcelMapper();

                await em.SaveAsync(filePath, rows, xlsx: true);

                return await GetStream(filePath);
            }
            catch (Exception e)
            {
                throw e;
            }
            finally
            {
                var file = new FileInfo(filePath);

                if (file.Exists)
                {
                    file.Delete();
                }
            }
        }

        private async Task<byte[]> GetStream(string filePath)
        {
            byte[] fileBytes = null;

            using (System.IO.FileStream fs = System.IO.File.OpenRead(filePath))
            {
                fileBytes = new byte[fs.Length];
                int br = await fs.ReadAsync(fileBytes, 0, fileBytes.Length);
                if (br != fs.Length)
                    throw new System.IO.IOException(filePath);
                fs.Close();
            }

            return fileBytes;
        }
    }
}
