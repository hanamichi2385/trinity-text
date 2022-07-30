using Resulz;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TrinityText.Business.Services
{
    public interface ITextService
    {
        Task<OperationResult<TextDTO>> Get(int id);
        Task<OperationResult<IList<TextRevisionDTO>>> GetAllRevisions(int textId);
        Task<OperationResult<TextDTO>> Save(TextDTO dto);
        Task<OperationResult<PagedResult<TextDTO>>> Search(SearchTextDTO search, int page, int size);
    }
}