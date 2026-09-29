using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace TrinityText.Business
{
    public interface IExcelService
    {
        /// <param name="allowedWebsites">When given, a row of any other website makes the import fail (the sheet decides the website of every row).</param>
        Task<TextDTO[]> GetTextsFromStream(string user, Stream fileStream, IReadOnlyCollection<string> allowedWebsites = null);
        Task<byte[]> GetExcelFileStream(PageDTO[] list);
        Task<byte[]> GetExcelFileStream(WidgetDTO[] list);
        Task<byte[]> GetExcelFileStream(TextDTO[] list);
        Task<byte[]> GetExcelFileStream(IDictionary<KeyValuePair<string, string>, TextDTO[]> textsForSiteLang);
    }
}
