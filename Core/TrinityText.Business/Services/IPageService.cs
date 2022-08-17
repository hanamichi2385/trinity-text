using Resulz;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TrinityText.Business.Services
{
    public interface IPageService
    {
        Task<OperationResult<PagedResult<PageDTO>>> Search(SearchPageDTO search, int page, int size);

        Task<OperationResult<PageDTO>> Get(int id);

        Task<OperationResult<PageDTO>> Save(PageDTO dto);

        Task<OperationResult<Dictionary<string, List<PageDTO>>>> GetPublishablePages(string website, string site, string[] languages);

        Task<OperationResult> Remove(int id);
    }
}
