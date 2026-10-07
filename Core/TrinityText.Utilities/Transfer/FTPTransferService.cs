using FluentFTP;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using TrinityText.Business;

namespace TrinityText.Utilities
{
    public class FTPTransferService : ITransferService
    {
        private readonly ILogger<FTPTransferService> _logger;

        public string Key => "ftp";

        public FTPTransferService(ILogger<FTPTransferService> logger)
        {
            _logger = logger;
        }

        public async Task<string> Upload(string tenant, string website, DirectoryInfo baseDirectory, string ftphost, string username, string password, string path, int? port)
        {
            var operationLog = new StringBuilder();

            await using var ftp = new AsyncFtpClient(ftphost, port ?? 21);
            try
            {
                var directories = path.Split('/', StringSplitOptions.RemoveEmptyEntries).ToList().AsReadOnly();
                if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
                {
                    var credentials = new NetworkCredential(username, password);
                    ftp.Credentials = credentials;
                }
                await ftp.Connect();

                var currentDirectory = await ftp.GetWorkingDirectory();

                foreach (var d in directories)
                {
                    currentDirectory = await NavigateTo(d, ftp, operationLog);
                }

                currentDirectory = await NavigateTo(tenant, ftp, operationLog);
                currentDirectory = await NavigateTo(website, ftp, operationLog);

                await UploadFilesPerDirectory(currentDirectory, ftp, baseDirectory, operationLog);

                foreach (var d in baseDirectory.GetDirectories())
                {
                    try
                    {
                        await ftp.SetWorkingDirectory(currentDirectory);
                        await UploadDirectory(d.FullName, d.Name, ftp, operationLog);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "NAVIGATE {folder}", $"{currentDirectory}/{d.Name}");
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
                return operationLog.ToString();
            }
            finally
            {
                if (ftp.IsConnected)
                {
                    await ftp.Disconnect();
                }
            }
            return operationLog.ToString();
        }

        private async Task<string> NavigateTo(string directoryName, AsyncFtpClient ftp, StringBuilder operationLog)
        {
            var currentDirectory = $"{await ftp.GetWorkingDirectory()}/{directoryName}";

            if (!await ftp.DirectoryExists(currentDirectory))
            {
                try
                {
                    await ftp.CreateDirectory(directoryName);
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
            await ftp.SetWorkingDirectory(currentDirectory);

            return currentDirectory;
        }

        private async Task UploadDirectory(string localDirectoryPath, string ftpDirectoryPath, AsyncFtpClient ftp, StringBuilder operationLog)
        {
            try
            {
                var directory = new DirectoryInfo(localDirectoryPath);

                var currentDirectory = await NavigateTo(ftpDirectoryPath, ftp, operationLog);

                foreach (var sub in directory.GetDirectories())
                {
                    await UploadDirectory(sub.FullName, sub.Name, ftp, operationLog);
                    await ftp.SetWorkingDirectory(currentDirectory);
                }

                await UploadFilesPerDirectory(currentDirectory, ftp, directory, operationLog);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "UPLOADDIRECTORY");
                operationLog.AppendLine($"Problem file in path: {ftpDirectoryPath}");
                operationLog.AppendLine("--ex: " + e.Message);
                if (e.InnerException != null && !string.IsNullOrEmpty(e.InnerException.Message))
                {
                    operationLog.AppendLine("--innerex: " + e.InnerException.Message);
                }
            }
        }

        private async Task UploadFilesPerDirectory(string currentDirectory, AsyncFtpClient ftp, DirectoryInfo directory, StringBuilder operationLog)
        {
            try
            {
                await ftp.SetWorkingDirectory(currentDirectory);

                var filesToUpload = directory.GetFiles();

                if (filesToUpload.Length > 0)
                {
                    await ftp.UploadFiles(filesToUpload, currentDirectory, FtpRemoteExists.Overwrite, createRemoteDir: true, verifyOptions: FtpVerify.Retry, errorHandling: FtpError.Throw);
                }

            }
            catch (Exception e)
            {
                _logger.LogError(e, "UPLOADFILESPERDIRECTORY");
                operationLog.AppendLine($"Problem with upload file in path: {currentDirectory}");
                operationLog.AppendLine("--ex: " + e.Message);
                if (e.InnerException != null && !string.IsNullOrEmpty(e.InnerException.Message))
                {
                    operationLog.AppendLine("--innerex: " + e.InnerException.Message);
                }
            }
        }

        //private void UploadFile(FileInfo file, FtpClient ftp, bool fileExist, StringBuilder operationLog)
        //{
        //    try
        //    {
        //        var remotePath = ftp.GetWorkingDirectory() + "/" + file.Name;

        //        ftp.UploadFile(file.FullName, remotePath, FtpRemoteExists.Overwrite, verifyOptions: FtpVerify.Retry);
        //    }
        //    catch (Exception e)
        //    {
        //        operationLog.AppendLine(string.Format("Eccezione upload file {0}, path: {1}", file.Name, file.FullName));
        //        operationLog.AppendLine("--ex: " + e.Message);
        //        if (e.InnerException != null && !string.IsNullOrEmpty(e.InnerException.Message))
        //        {
        //            operationLog.AppendLine("--innerex: " + e.InnerException.Message);
        //        }
        //    }
        //}

        public async Task<byte[]> GetFile(string tenant, string website, string file, string host, string username, string password, string path, int? port)
        {
            var operationLog = new StringBuilder();

            //string baseFtpDirectoryPath = host;

            await using var ftp = new AsyncFtpClient(host, port ?? 21);
            try
            {
                var directories = path.Split('/', StringSplitOptions.RemoveEmptyEntries).ToList().AsReadOnly();
                if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
                {
                    var credentials = new NetworkCredential(username, password);
                    ftp.Credentials = credentials;
                }
                await ftp.Connect();

                var currentDirectory = await ftp.GetWorkingDirectory();

                foreach (var d in directories)
                {
                    currentDirectory = await NavigateTo(d, ftp, operationLog);
                }

                currentDirectory = await NavigateTo(tenant, ftp, operationLog);
                currentDirectory = await NavigateTo(website, ftp, operationLog);

                if (await ftp.FileExists(file))
                {
                    await using var stream = await ftp.OpenRead(file);
                    using var ms = new MemoryStream();
                    await stream.CopyToAsync(ms);
                    return ms.ToArray();
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "GETFILES");
            }
            finally
            {
                if (ftp.IsConnected)
                {
                    await ftp.Disconnect();
                }
            }
            return null;
        }
    }
}
