using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using TrinityText.Business;

namespace TrinityText.Utilities
{
    public class ZipCompressionService : ICompressionFileService
    {
        private readonly ILogger<ZipCompressionService> _logger;

        public ZipCompressionService(ILogger<ZipCompressionService> logger)
        {
            _logger = logger;
        }

        public async Task<string> CompressFolder(string folder, string destinationFilePath)
        {
            try
            {
                string fileZipName = destinationFilePath + "\\" + folder.Split('\\').Last() + ".zip";

                ZipFile.CreateFromDirectory(folder, fileZipName);

                return await Task.FromResult(fileZipName);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "COMPRESSFOLDER");
            }
            return await Task.FromResult(string.Empty);
        }

        public async Task DecompressFolder(string basePath, byte[] zipFileByteArray)
        {
            try
            {
                var filename = $"{basePath}\\{Guid.NewGuid().ToString().Replace("-", string.Empty)}.zip";
                using (MemoryStream stream = new MemoryStream(zipFileByteArray))
                {
                    using (FileStream file = new FileStream(filename, FileMode.Create, System.IO.FileAccess.Write))
                    {
                        byte[] bytes = new byte[stream.Length];
                        await stream.ReadAsync(bytes, 0, (int)stream.Length);
                        await file.WriteAsync(bytes, 0, bytes.Length);
                        stream.Close();
                        file.Close();
                    }
                    ZipFile.ExtractToDirectory(filename, basePath);

                    File.Delete(filename);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DECOMPRESSFOLDER");
            }
        }
    }
}
