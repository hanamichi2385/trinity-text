using Resulz;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace TrinityText.Business
{
    public interface ITextService
    {
        Task<OperationResult<TextDTO>> Get(int id);
        Task<OperationResult<IList<TextRevisionDTO>>> GetAllRevisions(int textId);
        Task<OperationResult<TextDTO>> Save(TextDTO dto);
        Task<OperationResult<PagedResult<TextDTO>>> Search(SearchTextDTO search, int page, int size);
        Task<OperationResult<FrozenDictionary<string, ReadOnlyCollection<TextDTO>>>> GetPublishableTexts(string website, string site, string[] languages, IReadOnlyList<TextTypeDTO> textTypes);
        Task<OperationResult<FrozenDictionary<string, ReadOnlyCollection<TextDTO>>>> GetPublishableTextsByWebsite(string website, Dictionary<string, string[]> sitesLanguages, IReadOnlyList<TextTypeDTO> textTypes);
        Task<OperationResult> Remove(int id);
        Task<OperationResult> CleanRevisions(int revisionToMantain);
        /// <param name="allowedWebsites">When given, texts of any other website are refused (the import data comes from a file).</param>
        Task<OperationResult<int>> ImportTexts(TextTypeDTO type, IList<TextDTO> texts, bool @override, IReadOnlyCollection<string> allowedWebsites = null);
    }
}