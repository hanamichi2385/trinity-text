using Resulz;
using System.Threading.Tasks;

namespace TrinityText.Business
{
    public interface IWidgetService
    {
        Task<OperationResult<PagedResult<WidgetDTO>>> Search(SearchWidgetDTO search, int page, int size);
        Task<OperationResult<WidgetDTO>> Get(int id);
        Task<OperationResult<WidgetDTO>> GetByKeys(string key, string website, string site, string language);
        Task<OperationResult> Remove(int id);
        Task<OperationResult<WidgetDTO>> Save(WidgetDTO dto);
    }
}
