using Resulz;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TrinityText.Business.Services
{
    public interface ICacheSettingsService
    {
        Task<OperationResult<CacheSettingsDTO>> Get(int id);
        Task<OperationResult<IList<CacheSettingsDTO>>> GetAll();
        Task<OperationResult<CacheSettingsDTO>> GetByCdnServer(int cdnServerId);
        Task<OperationResult> Remove(int id);
        Task<OperationResult<CacheSettingsDTO>> Save(CacheSettingsDTO dto);
    }
}