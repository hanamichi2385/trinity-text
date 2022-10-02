using Resulz;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TrinityText.Business
{
    public interface IFTPServerService
    {
        Task<OperationResult<FTPServerDTO>> Get(int id);
        Task<OperationResult<IList<FTPServerDTO>>> GetAll();
        Task<OperationResult> Remove(int id);
        Task<OperationResult<FTPServerDTO>> Save(FTPServerDTO dto);
    }
}