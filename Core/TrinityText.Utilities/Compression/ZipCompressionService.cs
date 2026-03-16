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

                await Task.Run(() => ZipFile.CreateFromDirectory(folder, fileZipName));

                return fileZipName;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Errore durante la compressione della cartella: {Folder}", folder);
                return string.Empty;
            }

            //try
            //{
            //    string fileZipName = $"{destinationFilePath}\\{folder.Split('\\').Last()}.zip";

            //    ZipFile.CreateFromDirectory(folder, fileZipName);

            //    return await Task.FromResult(fileZipName);
            //}
            //catch (Exception ex)
            //{
            //    _logger.LogError(ex, "COMPRESSFOLDER");
            //}
            //return await Task.FromResult(string.Empty);
        }

        public Task DecompressFolder(string basePath, byte[] zipFileByteArray)
        {
            try
            {
                Directory.CreateDirectory(basePath);

                using (var stream = new MemoryStream(zipFileByteArray))
                {
                    using (var archive = new ZipArchive(stream))
                    {
                        archive.ExtractToDirectory(basePath, overwriteFiles: true);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during Decompress folder: {BasePath}", basePath);
            }
            return Task.CompletedTask;

            //try
            //{
            //    var f = new DirectoryInfo(basePath);
            //    if (f.Exists == false)
            //    {
            //        f.Create();
            //    }
            //    var filename = $"{basePath}\\{Guid.NewGuid().ToString().Replace("-", string.Empty)}.zip";
            //    using (var stream = new MemoryStream(zipFileByteArray))
            //    {
            //        using (var file = new FileStream(filename, FileMode.Create, System.IO.FileAccess.Write))
            //        {
            //            await stream.CopyToAsync(file);
            //            //byte[] bytes = new byte[stream.Length];
            //            //await stream.ReadAsync(bytes, 0, (int)stream.Length);
            //            //await file.WriteAsync(bytes, 0, bytes.Length);
            //            //stream.Close();
            //            //file.Close();
            //            file.Close();
            //        }
            //    }
            //    ZipFile.ExtractToDirectory(filename, basePath);

            //    File.Delete(filename);

            //}
            //catch (Exception ex)
            //{
            //    _logger.LogError(ex, "DECOMPRESSFOLDER");
            //}
        }
    }
}
