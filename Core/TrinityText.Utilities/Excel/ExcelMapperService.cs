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

            return await SaveToBytes(pages, "pages");
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

            return await SaveToBytes(widgets, "widgets");
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
                    TEXT = l.TextRevision?.Content ?? "[NULL]"
                }).ToArray();

            return await SaveToBytes(texts, "texts");
        }

        private static async Task<byte[]> SaveToBytes<T>(T[] rows, string sheetName) where T : class
        {
            var em = new ExcelMapper()
            {
                HeaderRow = true,
                CreateMissingHeaders = true,
            };
            using var ms = new MemoryStream();
            await em.SaveAsync(ms, rows, sheetName, xlsx: true);
            return ms.ToArray();
        }

        public async Task<byte[]> GetExcelFileStream(IDictionary<KeyValuePair<string, string>, TextDTO[]> textsForSiteLang)
        {
            // The mapper keeps the workbook between calls: every SaveAsync adds a sheet and rewrites the whole
            // workbook to the target, so the (reset) MemoryStream replaces the former temp file + read-back + delete.
            using var ms = new MemoryStream();
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
                        LANGUAGE = l.Language ?? "",
                        CONTENT = l.TextRevision?.Content ?? string.Empty,
                    }).ToArray();

                ms.SetLength(0);
                await em.SaveAsync(ms, list, sheetName, xlsx: true);
            }

            return ms.ToArray();
        }

        public async Task<TextDTO[]> GetTextsFromStream(string user, Stream fileStream, IReadOnlyCollection<string> allowedWebsites = null)
        {
            var list = new List<TextDTO>();
            try
            {
                var typesRs = await _textTypeService.GetAll();

                if (typesRs.Success)
                {
                    var types = typesRs.Value;

                    // one lookup per row on the type list -> dictionary (first match wins, case-insensitive)
                    var typesByName = new Dictionary<string, TextTypeDTO>(StringComparer.InvariantCultureIgnoreCase);
                    foreach (var t in types)
                    {
                        typesByName.TryAdd(t.Name, t);
                    }

                    var em = new ExcelMapper(fileStream);
                    var rows = em.Fetch();

                    var rowCount = 0;
                    foreach (var r in rows)
                    {
                        if (++rowCount > MaxImportRows)
                        {
                            throw new InvalidOperationException($"The sheet has more than {MaxImportRows} rows");
                        }

                        var rw = (IDictionary<string, object>)r;
                        var key = GetExcelValue(r, "KEY");
                        var typeName = GetExcelValue(r, "TYPE")?.ToUpperInvariant();
                        var website = GetExcelValue(r, "WEBSITE");
                        var site = GetExcelValue(r, "SITE");
                        var country = GetExcelValue(r, "COUNTRY");
                        var lang = GetExcelValue(r, "LANGUAGE");
                        var text = GetExcelValue(r, "TEXT");

                        if (!string.IsNullOrWhiteSpace(key))
                        {
                            // the sheet decides the website of every row: only the ones the caller can manage
                            string websiteCell = website;
                            string siteCell = site;
                            string langCell = lang;

                            if (allowedWebsites != null
                                && !string.IsNullOrWhiteSpace(websiteCell) && websiteCell != "*"
                                && !allowedWebsites.Contains(websiteCell, StringComparer.OrdinalIgnoreCase))
                            {
                                throw new UnauthorizedAccessException($"The sheet contains texts of the website '{websiteCell}'");
                            }

                            // site and language become folder names in the export
                            if ((!string.IsNullOrWhiteSpace(siteCell) && siteCell != "*" && !PathSafety.IsValidSegment(siteCell))
                                || (!string.IsNullOrWhiteSpace(langCell) && !PathSafety.IsValidSegment(langCell)))
                            {
                                throw new InvalidOperationException($"Invalid site or language for the text '{key}'");
                            }

                            TextTypeDTO type = null;

                            var cont = true;

                            if (!"*".Equals(typeName, StringComparison.InvariantCultureIgnoreCase)
                                && !string.IsNullOrWhiteSpace(typeName))
                            {
                                typesByName.TryGetValue(typeName, out type);

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
                                    TextTypeId = type?.Id,
                                    Website = ScopeValue(website),
                                    Country = ScopeValue(country),
                                    Site = ScopeValue(site),
                                };

                                list.Add(dto);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // an unreadable workbook must not look like "0 texts to import"
                _logger.LogError(ex, "GetTextsFromStream");
                throw;
            }

            return [.. list];
        }

        private const int MaxImportRows = 50_000;

        // "*" or a blank cell = not scoped (null), never an empty string
        private static string ScopeValue(string cell)
            => string.IsNullOrWhiteSpace(cell) || "*".Equals(cell, StringComparison.Ordinal) ? null : cell;

        private static string GetExcelValue(IDictionary<string, object> r, string key)
        {
            return r.TryGetValue(key, out object v) ? v?.ToString()?.Trim() : string.Empty;
        }
    }

    
}
