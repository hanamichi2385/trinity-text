using Resulz;
using System.Collections.Frozen;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace TrinityText.Business
{
    public interface IPageService
    {
        Task<OperationResult<PagedResult<PageDTO>>> Search(SearchPageDTO search, int page, int size);

        Task<OperationResult<PageDTO>> Get(int id);

        Task<OperationResult<PageDTO>> Save(PageDTO dto);

        Task<OperationResult<FrozenDictionary<string, ReadOnlyCollection<PageDTO>>>> GetPublishablePages(string website, string site, string[] languages);

        Task<OperationResult> Remove(int id);
    }
}
