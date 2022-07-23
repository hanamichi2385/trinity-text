using System.Threading.Tasks;

namespace TrinityText.Business
{
    public interface ICompressionFileService
    {
        Task<string> CompressFolder(string folder, string destinationFilePath);

        Task DecompressFolder(string basePath, byte[] zipFileByteArray);
    }
}
