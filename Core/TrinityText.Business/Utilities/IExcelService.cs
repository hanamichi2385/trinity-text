using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace TrinityText.Business
{
    public interface IExcelService
    {
        Task<IList<TextDTO>> GetTextsFromStream(string utente, Stream fileStream);

        Task<byte[]> GetExcelFileStream(IList<PageDTO> list);

        Task<byte[]> GetExcelFileStream(IList<WidgetDTO> list);

        Task<byte[]> GetExcelFileStream(IList<TextDTO> list);
        Task<byte[]> GetExcelFileStream(IDictionary<KeyValuePair<string, string>, IList<TextDTO>> resourcesPerInstanceLang);
    }
}
