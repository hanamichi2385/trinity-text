using Resulz;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TrinityText.Business
{
    public interface ITextTypeService
    {
        Task<OperationResult<TextTypeDTO>> Get(int id);
        Task<OperationResult<IList<TextTypeDTO>>> GetAll();
        Task<OperationResult> Remove(int id);
        Task<OperationResult<TextTypeDTO>> Save(TextTypeDTO dto);
        Task<OperationResult<IList<TextTypeDTO>>> GetAllByWebsite(string website);
    }
}
