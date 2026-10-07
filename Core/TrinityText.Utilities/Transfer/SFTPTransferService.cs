using Microsoft.Extensions.Logging;
using Renci.SshNet;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TrinityText.Business;

namespace TrinityText.Utilities
{
    public class SFTPTransferService : ITransferService
    {
        private readonly ILogger<SFTPTransferService> _logger;

        public string Key => "sftp";

        public SFTPTransferService(ILogger<SFTPTransferService> logger)
        {
            _logger = logger;
        }

        public async Task<string> Upload(string tenant, string vendor, DirectoryInfo baseDirectory, string host, string username, string password, string path, int? port)
        {
            var operationLog = new StringBuilder();

            SftpClient ftp = null;
            try
            {
                var h = new Uri(host);

                ftp = new SftpClient(h.Host, port ?? 22, username, password);

                await ftp.ConnectAsync(CancellationToken.None);

                var directories = path.Split('/', StringSplitOptions.RemoveEmptyEntries).ToList().AsReadOnly();

                var currentDirectory = ftp.WorkingDirectory;

                foreach (var d in directories)
                {
                    currentDirectory = await NavigateTo(d, ftp, operationLog);
                }

                currentDirectory = await NavigateTo(tenant, ftp, operationLog);
                currentDirectory = await NavigateTo(vendor, ftp, operationLog);

                await UploadFilesPerDirectory(currentDirectory, ftp, baseDirectory, operationLog);

                foreach (var d in baseDirectory.GetDirectories())
                {
                    try
                    {
                        await ftp.ChangeDirectoryAsync(currentDirectory, CancellationToken.None);

                        await UploadDirectory(d.FullName, d.Name, ftp, operationLog);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "UPLOAD");
                        operationLog.AppendLine($"Problem with folder: {currentDirectory}/{d.Name}");
                        operationLog.AppendLine("--ex: " + e.Message);
                        if (e.InnerException != null && !string.IsNullOrEmpty(e.InnerException.Message))
                        {
                            operationLog.AppendLine("--innerex: " + e.InnerException.Message);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "UPLOAD");
                operationLog.AppendLine("--ex: " + e.Message);
                if (e.InnerException != null && !string.IsNullOrEmpty(e.InnerException.Message))
                {
                    operationLog.AppendLine("--innerex: " + e.InnerException.Message);
                }
            }
            finally
            {
                //baseDirectory.Delete(true);
                try
                {
                    if (ftp != null && ftp.IsConnected)
                    {
                        ftp.Disconnect();
                    }
                }
                catch(Exception ex)
                {
                    operationLog.AppendLine("Error during SFTP Disconnect");
                    operationLog.AppendLine("--ex: " + ex.Message);
                    if (ex.InnerException != null && !string.IsNullOrEmpty(ex.InnerException.Message))
                    {
                        operationLog.AppendLine("--innerex: " + ex.InnerException.Message);
                    }
                }

                ftp?.Dispose();
            }
            return operationLog.ToString();
        }

        public async Task<byte[]> GetFile(string tenant, string vendor, string file, string host, string username, string password, string path, int? port)
        {
            var operationLog = new StringBuilder();

            //string baseFtpDirectoryPath = host;

            SftpClient ftp = null;
            try
            {
                var directories = path.Split('/', StringSplitOptions.RemoveEmptyEntries).ToList().AsReadOnly();

                ftp = new SftpClient(host, port ?? 22,  username, password);

                await ftp.ConnectAsync(CancellationToken.None);

                var currentDirectory = ftp.WorkingDirectory;

                foreach (var d in directories)
                {
                    currentDirectory = await NavigateTo(d, ftp, operationLog);
                }

                currentDirectory = await NavigateTo(tenant, ftp, operationLog);
                currentDirectory = await NavigateTo(vendor, ftp, operationLog);

                if (await ftp.ExistsAsync($"{currentDirectory}/{file}", CancellationToken.None))
                {
                    using var ms = new MemoryStream();
                    await ftp.DownloadFileAsync(file, ms, CancellationToken.None);
                    return ms.ToArray();
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "GETFILE");
            }
            finally
            {
                if (ftp?.IsConnected ?? false)
                {
                    ftp.Disconnect();
                }

                ftp?.Dispose();
            }

            // "not found" is a null result, not a null Task (awaiting it threw a NullReferenceException)
            return null;
        }

        private async Task<string> NavigateTo(string directoryName, SftpClient ftp, StringBuilder operationLog)
        {
            var currentDirectory = $"{ftp.WorkingDirectory}/{directoryName}";

            if (!await ftp.ExistsAsync(currentDirectory, CancellationToken.None))
            {
                try
                {
                    await ftp.CreateDirectoryAsync(directoryName, CancellationToken.None);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "NAVIGATETO");
                    operationLog.AppendLine($"Problem with folder {directoryName}, path: {currentDirectory}");
                    operationLog.AppendLine("--ex: " + e.Message);
                    if (e.InnerException != null && !string.IsNullOrEmpty(e.InnerException.Message))
                    {
                        operationLog.AppendLine("--innerex: " + e.InnerException.Message);
                    }
                }
            }
            await ftp.ChangeDirectoryAsync(directoryName, CancellationToken.None);

            return currentDirectory;
        }

        private async Task UploadFilesPerDirectory(string currentDirectory, SftpClient ftp, DirectoryInfo directory, StringBuilder operationLog)
        {
            try
            {
                if (!ftp.WorkingDirectory.Equals(currentDirectory, StringComparison.InvariantCultureIgnoreCase))
                {
                    await ftp.ChangeDirectoryAsync(currentDirectory, CancellationToken.None);
                }

                var filesToUpload = directory.GetFiles();

                if (filesToUpload.Length > 0)
                {
                    foreach (var f in filesToUpload)
                    {
                        using var stream = f.OpenRead();
                        await ftp.UploadFileAsync(stream, f.Name, CancellationToken.None);
                    }
                }

            }
            catch (Exception e)
            {
                _logger.LogError(e, "UPLOADFILESPERDIRECTORY");
                operationLog.AppendLine($"Problem with file in path: {currentDirectory}");
                operationLog.AppendLine("--ex: " + e.Message);
                if (e.InnerException != null && !string.IsNullOrEmpty(e.InnerException.Message))
                {
                    operationLog.AppendLine("--innerex: " + e.InnerException.Message);
                }
            }
        }

        private async Task UploadDirectory(string localDirectoryPath, string ftpDirectoryPath, SftpClient ftp, StringBuilder operationLog)
        {
            try
            {
                var directory = new DirectoryInfo(localDirectoryPath);

                var currentDirectory = await NavigateTo(ftpDirectoryPath, ftp, operationLog);

                foreach (var sub in directory.GetDirectories())
                {
                    await UploadDirectory(sub.FullName, sub.Name, ftp, operationLog);
                    await ftp.ChangeDirectoryAsync(currentDirectory, CancellationToken.None);
                }

                await UploadFilesPerDirectory(currentDirectory, ftp, directory, operationLog);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "UPLOADDIRECTORY");
                operationLog.AppendLine($"Problem with folder: {ftpDirectoryPath}");
                operationLog.AppendLine("--ex: " + e.Message);
                if (e.InnerException != null && !string.IsNullOrEmpty(e.InnerException.Message))
                {
                    operationLog.AppendLine("--innerex: " + e.InnerException.Message);
                }
            }
        }
    }
}
