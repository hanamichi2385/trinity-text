using Resulz;
using System;
using System.Threading.Tasks;

namespace TrinityText.Business
{
    public interface IPublicationSupportService
    {
        //Task<OperationResult<string>> CreateExportFile(int id, PayloadDTO payload, PublicationType exportType, PublishType publishType, DateTime filesGenerationDate, bool compressFileOutput, string user, CdnServerDTO cdnServer);
        Task<OperationResult> Generate(PublicationDTO filesGenerationSettings);
        Task<OperationResult> Publish(PublicationDTO filesGenerationSettings);
    }
}