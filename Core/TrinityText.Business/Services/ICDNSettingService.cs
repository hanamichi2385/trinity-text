using Resulz;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TrinityText.Business
{
    public interface ICDNSettingService
    {
        Task<OperationResult<CdnServerDTO>> Get(int id);
        Task<OperationResult<IList<CdnServerDTO>>> GetAll();
        Task<OperationResult<IList<CdnServerDTO>>> GetAllByWebsite(string website);
        Task<OperationResult> Remove(int id);
        Task<OperationResult<CdnServerDTO>> Save(CdnServerDTO dto, IList<int> ftpList);
    }
}