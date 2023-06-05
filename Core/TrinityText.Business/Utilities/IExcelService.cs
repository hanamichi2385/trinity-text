using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace TrinityText.Business
{
    public interface IExcelService
    {
        Task<TextDTO[]> GetTextsFromStream(string user, Stream fileStream);
        Task<byte[]> GetExcelFileStream(PageDTO[] list);
        Task<byte[]> GetExcelFileStream(WidgetDTO[] list);
        Task<byte[]> GetExcelFileStream(TextDTO[] list);
        Task<byte[]> GetExcelFileStream(IDictionary<KeyValuePair<string, string>, TextDTO[]> textsForSiteLang);
    }
}
