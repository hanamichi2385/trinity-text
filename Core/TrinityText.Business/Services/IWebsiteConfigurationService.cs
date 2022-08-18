using Resulz;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TrinityText.Business.Services
{
    public interface IWebsiteConfigurationService
    {
        Task<OperationResult<WebsiteConfigurationDTO>> Get(int id);
        Task<OperationResult<IList<WebsiteConfigurationDTO>>> GetAll(string website);
        Task<OperationResult> Remove(int id);
        Task<OperationResult<WebsiteConfigurationDTO>> Save(WebsiteConfigurationDTO dto);
    }
}