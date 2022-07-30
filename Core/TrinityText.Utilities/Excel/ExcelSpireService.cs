using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Spire.Xls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TrinityText.Business;

namespace TrinityText.Utilities
{
    public class ExcelSpireService : IExcelService
    {
        private readonly ExcelOptions _options;

        private readonly ITextTypeService _textTypeService;

        private readonly ILogger<ExcelSpireService> _logger;

        public ExcelSpireService(ITextTypeService textTypeService, IOptions<ExcelOptions> options, ILogger<ExcelSpireService> logger)
        {
            _textTypeService = textTypeService;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<IList<TextDTO>> GetTextsFromStream(string utente, Stream fileStream)
        {
            
            List<TextDTO> list = new List<TextDTO>();
            try
            {
                using (Workbook workbook = new Workbook())
                {
                    workbook.LoadFromStream(fileStream);

                    using (var currentWorksheet = workbook.Worksheets[0])
                    {

                        var start = currentWorksheet.FirstRow + 1;
                        var end = currentWorksheet.LastRow + 1;

                        var typesRs = await _textTypeService.GetAll();

                        if (typesRs.Success)
                        {
                            var types = typesRs.Value;

                            for (int i = start; i < end; i++)
                            {
                                var key = Convert.ToString(currentWorksheet.Range[i, 1].Value).Trim();
                                var typeName = Convert.ToString(currentWorksheet.Range[i, 2].Value).ToUpper().Trim();
                                var vendor = Convert.ToString(currentWorksheet.Range[i, 3].Value).Trim();
                                var istance = Convert.ToString(currentWorksheet.Range[i, 4].Value).Trim();
                                var nation = Convert.ToString(currentWorksheet.Range[i, 5].Value).Trim();
                                var lang = Convert.ToString(currentWorksheet.Range[i, 6].Value).Trim();
                                var text = Convert.ToString(currentWorksheet.Range[i, 7].Value).Trim();

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
                                        TextDTO dto = new TextDTO()
                                        {
                                            Name = key.Trim().Replace(' ', '_'),
                                            Language = lang,
                                            TextRevision = new TextRevisionDTO()
                                            {
                                                Content = text,
                                                CreationUser = utente,
                                                CreationDate = DateTime.Now
                                            },
                                            Active = true,
                                            TextType = type,
                                            TextTypeId = type != null ? type.Id : null,
                                            Website = "*".Equals(vendor) ? null : vendor,
                                            Country = "*".Equals(nation) ? null : nation,
                                            Site = "*".Equals(istance) ? null : istance,
                                        };

                                        list.Add(dto);
                                    }
                                }
                                else
                                {
                                    // E' finito il file..
                                    i = end;
                                }
                            }
                        }
                    }
                    
                }
            }catch(Exception ex)
            {
                _logger.LogError(ex, "GETTEXTSFROMSTREAM");
            }
            return list;
        }

        public async Task<byte[]> GetExcelFileStream(IList<PageDTO> list)
        {
            var basePath = _options.TempDirectory;
            var filePath = basePath + (basePath.EndsWith("/") ? string.Empty : "/") + "Pages" + Guid.NewGuid() + ".xlsx";
            try
            {
                var header = new Dictionary<string, int?>();
                header.Add("Title", 44);
                header.Add("Website", 14);
                header.Add("Site", 14);
                header.Add("Language", null);
                header.Add("Content", 800);

                Dictionary<KeyValuePair<int, int>, string> cells = new Dictionary<KeyValuePair<int, int>, string>();

                for (int i = 0; i < list.Count; i++)
                {
                    var w = list.ElementAt(i);

                    cells.Add(new KeyValuePair<int, int>(i + 2, 1), w.Name);
                    cells.Add(new KeyValuePair<int, int>(i + 2, 2), !string.IsNullOrWhiteSpace(w.Website) ? w.Website : "All websites");
                    cells.Add(new KeyValuePair<int, int>(i + 2, 3), !string.IsNullOrWhiteSpace(w.Pricelist) ? w.Pricelist : "All sites");
                    cells.Add(new KeyValuePair<int, int>(i + 2, 4), w.Language);
                    cells.Add(new KeyValuePair<int, int>(i + 2, 5), w.Content);
                }

                await CreateExcelFile(filePath, "Pages Export", new List<string>() { "Pages" }, new List<Dictionary<string, int?>>() { header }, new List<Dictionary<KeyValuePair<int, int>, string>> { cells });
                return await GetStream (filePath);
            }
            catch (Exception e)
            {
                throw e;
            }
            finally
            {
                FileInfo file = new FileInfo(filePath);

                if (file.Exists)
                {
                    file.Delete();
                }
            }
        }

        public async Task<byte[]> GetExcelFileStream(IList<WidgetDTO> list)
        {
            var basePath = _options.TempDirectory;
            var filePath = basePath + (basePath.EndsWith("/") ? string.Empty : "/") + "Widgets" + Guid.NewGuid() + ".xlsx";
            try
            {
                var header = new Dictionary<string, int?>();
                header.Add("Key", 44);
                header.Add("Website", 14);
                header.Add("Site", 14);
                header.Add("Language", null);
                header.Add("Content", 800);

                Dictionary<KeyValuePair<int, int>, string> cells = new Dictionary<KeyValuePair<int, int>, string>();

                for (int i = 0; i < list.Count; i++)
                {
                    var w = list.ElementAt(i);

                    cells.Add(new KeyValuePair<int, int>(i + 2, 1), w.Key);
                    cells.Add(new KeyValuePair<int, int>(i + 2, 2), !string.IsNullOrWhiteSpace(w.Website) ? w.Website : "All websites");
                    cells.Add(new KeyValuePair<int, int>(i + 2, 3), !string.IsNullOrWhiteSpace(w.Site) ? w.Site : "All sites");
                    cells.Add(new KeyValuePair<int, int>(i + 2, 4), w.Language);
                    cells.Add(new KeyValuePair<int, int>(i + 2, 5), w.Content);
                }

                await CreateExcelFile(filePath, "Widgets Export", new List<string>() { "Widgets" }, new List<Dictionary<string, int?>>() { header }, new List<Dictionary<KeyValuePair<int, int>, string>> { cells });
                return await GetStream(filePath);
            }
            catch (Exception e)
            {
                throw e;
            }
            finally
            {
                FileInfo file = new FileInfo(filePath);

                if (file.Exists)
                {
                    file.Delete();
                }
            }
        }

        public async Task<byte[]> GetExcelFileStream(IList<TextDTO> list)
        {
            var basePath = _options.TempDirectory;
            var filePath = basePath + (basePath.EndsWith("/") ? string.Empty : "/") + "Texts" + Guid.NewGuid() + ".xlsx";
            try
            {
                var header = new Dictionary<string, int?>();
                header.Add("Key", 44);
                header.Add("Type", 14);
                header.Add("Website", 14);
                header.Add("Site", 14);
                header.Add("Country", 14);
                header.Add("Language", null);
                header.Add("Text", 800);

                Dictionary<KeyValuePair<int, int>, string> cells = new Dictionary<KeyValuePair<int, int>, string>();

                for (int i = 0; i < list.Count; i++)
                {
                    var w = list.ElementAt(i);

                    cells.Add(new KeyValuePair<int, int>(i + 2, 1), w.Name);
                    cells.Add(new KeyValuePair<int, int>(i + 2, 2), w.TextType != null ? w.TextType.Name : "*");
                    cells.Add(new KeyValuePair<int, int>(i + 2, 3), !string.IsNullOrWhiteSpace(w.Website) ? w.Website : "*");
                    cells.Add(new KeyValuePair<int, int>(i + 2, 4), !string.IsNullOrWhiteSpace(w.Site) ? w.Site : "*");
                    cells.Add(new KeyValuePair<int, int>(i + 2, 5), !string.IsNullOrWhiteSpace(w.Country) ? w.Country : "*");
                    cells.Add(new KeyValuePair<int, int>(i + 2, 6), w.Language);
                    cells.Add(new KeyValuePair<int, int>(i + 2, 7), w.TextRevision.Content);
                }

                await CreateExcelFile(filePath, "Resources", new List<string>() { "Resources" }, new List<Dictionary<string, int?>>() { header }, new List<Dictionary<KeyValuePair<int, int>, string>> { cells });
                return await GetStream(filePath);
            }
            catch (Exception e)
            {
                throw e;
            }
            finally
            {
                FileInfo file = new FileInfo(filePath);

                if (file.Exists)
                {
                    file.Delete();
                }
            }
        }

        public async Task<byte[]> GetExcelFileStream(IDictionary<KeyValuePair<string, string>, IList<TextDTO>> resourcesPerInstanceLang)
        {
            var basePath = _options.TempDirectory;
            var filePath = basePath + (basePath.EndsWith("/") ? string.Empty : "/") + "Texts" + Guid.NewGuid() + ".xlsx";
            try
            {
                List<string> sheetNames = new List<string>();
                List<Dictionary<string, int?>> sheetHeaders = new List<Dictionary<string, int?>>();
                List<Dictionary<KeyValuePair<int, int>, string>> sheetCells = new List<Dictionary<KeyValuePair<int, int>, string>>();

                foreach (var instance in resourcesPerInstanceLang)
                {
                    var instanceName = instance.Key.Key;
                    var lang = instance.Key.Value;

                    sheetNames.Add(string.Format("{0}-{1}", instanceName, lang));

                    var header = new Dictionary<string, int?>();
                    header.Add("Name", 44);
                    header.Add("Type", 14);
                    header.Add("Website", 14);
                    header.Add("Site", 14);
                    header.Add("Language", null);
                    header.Add("Content", 800);

                    sheetHeaders.Add(header);

                    Dictionary<KeyValuePair<int, int>, string> cells = new Dictionary<KeyValuePair<int, int>, string>();

                    var list = instance.Value;
                    for (int i = 0; i < list.Count; i++)
                    {
                        var w = list.ElementAt(i);

                        cells.Add(new KeyValuePair<int, int>(i + 2, 1), w.Name);
                        cells.Add(new KeyValuePair<int, int>(i + 2, 2), w.TextType != null ? w.TextType.Name : (!string.IsNullOrWhiteSpace(w.Site) ? w.Website : "Channel file"));
                        cells.Add(new KeyValuePair<int, int>(i + 2, 3), !string.IsNullOrWhiteSpace(w.Website) ? w.Website : "All channels");
                        cells.Add(new KeyValuePair<int, int>(i + 2, 4), !string.IsNullOrWhiteSpace(w.Site) ? w.Site : "All sites");
                        cells.Add(new KeyValuePair<int, int>(i + 2, 5), w.Language);
                        cells.Add(new KeyValuePair<int, int>(i + 2, 6), w.TextRevision.Content);
                    }
                    sheetCells.Add(cells);

                }

                await CreateExcelFile(filePath, "Translations", sheetNames, sheetHeaders, sheetCells);
                return await GetStream(filePath);
            }
            catch (Exception e)
            {
                throw e;
            }
            finally
            {
                FileInfo file = new FileInfo(filePath);

                if (file.Exists)
                {
                    file.Delete();
                }
            }
        }

        private async Task CreateExcelFile(string filePath, string title, IList<string> sheetNames, List<Dictionary<string, int?>> sheetHeaders, List<Dictionary<KeyValuePair<int, int>, string>> sheetCells)
        {
            using (var stream = System.IO.File.Open(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                using (Workbook workbook = new Workbook())
                {
                    for (int s = 0; s < sheetNames.Count(); s++)
                    {
                        using (var sheet = workbook.Worksheets[s])
                        {
                            //sheet.Name = title;
                            var header = sheetHeaders[s];
                            SetHeader(sheet, header);

                            var cells = sheetCells[s];

                            for (int i = 0; i < cells.Count; i++)
                            {
                                var cell = cells.ElementAt(i);

                                var row = cell.Key.Key;
                                var column = cell.Key.Value;
                                var value = cell.Value;

                                sheet.Range[row, column].Text = value ?? string.Empty;
                            }
                        }
                    }
                    workbook.SaveToStream(stream, FileFormat.Version2016);
                }
                stream.Close();
            }
            await Task.FromResult(0);
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

        private void SetHeader(Worksheet sheet, IDictionary<string, int?> headerStrings)
        {
            for (int i = 0; i < headerStrings.Count; i++)
            {
                var h = headerStrings.ElementAt(i);

                sheet.Range[1, i + 1].Value = h.Key.ToUpper();
                sheet.Range[1, i + 1].Style.Font.IsBold = true;
            }
        }
    }
}
