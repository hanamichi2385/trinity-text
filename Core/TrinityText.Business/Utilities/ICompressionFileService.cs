using System.Threading.Tasks;

namespace TrinityText.Business
{
    public interface ICompressionFileService
    {
        Task<string> CompressFolder(string folder, string destinationFilePath);

        Task DecompressFolder(string basePath, byte[] zipFileByteArray);

        /// <summary>Extracts a ZIP file on disk (no need to hold the archive in memory).</summary>
        Task DecompressFile(string basePath, string zipFilePath);
    }
}
