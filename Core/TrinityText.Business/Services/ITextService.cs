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
        Task<OperationResult<Dictionary<string, List<TextDTO>>>> GetPublishableTexts(string website, string site, string[] languages);
        Task<OperationResult> Remove(int id);
        Task<OperationResult> CleanRevisions(int revisionToMantain);
        Task<OperationResult<int>> ImportTexts(TextTypeDTO type, IList<TextDTO> texts, bool @override);
    }
}