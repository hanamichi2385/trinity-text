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
            string fileZipName = null;
            try
            {
                string folderName = Path.GetFileName(folder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

                fileZipName = Path.Combine(destinationFilePath, $"{folderName}.zip");

                await Task.Run(() => ZipFile.CreateFromDirectory(folder, fileZipName, CompressionLevel.Optimal, includeBaseDirectory: false));

                return fileZipName;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante la compressione della cartella: {Folder}", folder);

                // do not leave a truncated archive behind, and do not report success with an empty path
                TryDelete(fileZipName);
                throw;
            }
        }

        public Task DecompressFolder(string basePath, byte[] zipFileByteArray)
        {
            if (zipFileByteArray == null || zipFileByteArray.Length == 0)
            {
                throw new InvalidOperationException("The publication has no ZIP content to extract");
            }

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

                // a broken archive must fail the publish, not upload an empty folder as a "success"
                throw;
            }
            return Task.CompletedTask;
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (IOException)
            {
            }
        }
    }
}
