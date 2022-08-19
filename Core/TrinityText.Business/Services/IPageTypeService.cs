using Resulz;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TrinityText.Business
{
    public interface IPageTypeService
    {
        Task<OperationResult<IList<PageTypeDTO>>> GetAll();

        Task<OperationResult<PageTypeDTO>> Get(int id);

        Task<OperationResult<IList<PageTypeDTO>>> GetAllByUser(string[] websites, string[] visibilities);

        Task<OperationResult<PageTypeDTO>> Save(PageTypeDTO dto);

        Task<OperationResult> Remove(int id);
    }
}
