using System.IO;
using System.Threading.Tasks;

namespace TrinityText.Business
{
    public interface ITransferService
    {
        Task<string> Upload(string tenant, string website, DirectoryInfo baseDirectory, string host, string username, string password, string path);
        Task<byte[]> GetFile(string tenant, string website, string file, string host, string username, string password, string path);
    }
}
