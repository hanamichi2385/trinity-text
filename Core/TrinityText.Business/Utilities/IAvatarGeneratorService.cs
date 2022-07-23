using System.Threading.Tasks;

namespace TrinityText.Business
{
    public interface IAvatarGeneratorService
    {
        Task<byte[]> Generate(string email);
    }
}
