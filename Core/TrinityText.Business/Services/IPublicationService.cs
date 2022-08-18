using Resulz;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TrinityText.Business.Services
{
    public interface IPublicationService
    {
        Task<OperationResult<IList<PublicationDTO>>> GetAll();

        Task<OperationResult<PublicationDTO>> Get(int id);

        Task<OperationResult<PublicationDTO>> Save(PublicationDTO dto);

        Task<PublicationDTO> Remove(int id);
    }
}
