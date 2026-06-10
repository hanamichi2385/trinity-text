using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.IO.Compression;
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
                string folderName = Path.GetFileName(folder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

                string fileZipName = Path.Combine(destinationFilePath, $"{folderName}.zip");

                await Task.Run(() => ZipFile.CreateFromDirectory(folder, fileZipName, CompressionLevel.Optimal, includeBaseDirectory: false));

                return fileZipName;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante la compressione della cartella: {Folder}", folder);
                return string.Empty;
            }
        }

        public Task DecompressFolder(string basePath, byte[] zipFileByteArray)
        {
            try
            {
                Directory.CreateDirectory(basePath);

                using var stream = new MemoryStream(zipFileByteArray, writable: false);
                using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
                archive.ExtractToDirectory(basePath, overwriteFiles: true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during Decompress folder: {BasePath}", basePath);
            }
            return Task.CompletedTask;
        }
    }
}
