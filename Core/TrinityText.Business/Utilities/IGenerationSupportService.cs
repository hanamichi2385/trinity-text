using System.Threading.Tasks;

namespace TrinityText.Business
{
    public interface IGenerationSupportService
    {
        Task<string> Generate(PublicationDTO filesGenerationSetting);
    }
}