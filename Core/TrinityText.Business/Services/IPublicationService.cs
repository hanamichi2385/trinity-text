using Resulz;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TrinityText.Business
{
    public interface IPublicationService
    {
        Task<OperationResult<IList<PublicationDTO>>> GetAll(string[] websites = null);

        Task<OperationResult<PublicationDTO>> Get(int id, bool withContent);

        Task<OperationResult<PublicationDTO>> Create(PublicationDTO dto);

        Task<OperationResult> Update(int id, PublicationStatus status, string message, byte[] file);

        Task<OperationResult> Remove(int id);
    }
}
