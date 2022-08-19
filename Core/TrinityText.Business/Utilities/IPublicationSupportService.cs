using Resulz;
using System;
using System.Threading.Tasks;

namespace TrinityText.Business
{
    public interface IPublicationSupportService
    {
        Task<OperationResult<string>> ExportFiles(int id, PayloadDTO payload, PublicationType exportType, PublishType publishType, DateTime filesGenerationDate, bool compressFileOutput, string user, CdnServerDTO cdnServer);
        Task<string> Generate(PublicationDTO filesGenerationSettings);
        Task<string> Publish(PublicationDTO filesGenerationSettings);
    }
}