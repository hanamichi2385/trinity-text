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

        public FTPTransferService(ILogger<FTPTransferService> logger)
        {
            _logger = logger;
        }

        public async Task<string> Upload(string tenant, string website, DirectoryInfo baseDirectory, string ftphost, string username, string password, string path)
        {
            StringBuilder operationLog = new StringBuilder();

            FtpClient ftp = new FtpClient();
            try
            {
                var directories = path.Split(new string[] { "/" }, StringSplitOptions.RemoveEmptyEntries).ToList();
                ftp.Host = ftphost;
                if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
                {
                    NetworkCredential credentials = new NetworkCredential(username, password);
                    ftp.Credentials = credentials;
                }
                ftp.Connect();

                var currentDirectory = ftp.GetWorkingDirectory();

                foreach (var d in directories)
                {
                    currentDirectory = NavigateTo(d, ftp, operationLog);
                }

                currentDirectory = NavigateTo(tenant, ftp, operationLog);
                currentDirectory = NavigateTo(website, ftp, operationLog);

                UploadFilesPerDirectory(currentDirectory, ftp, baseDirectory, operationLog);

                foreach (var d in baseDirectory.GetDirectories())
                {
                    try
                    {
                        ftp.SetWorkingDirectory(currentDirectory);
                        UploadDirectory(d.FullName, d.Name, ftp, operationLog);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, $"NAVIGATE {(currentDirectory + "/" + d.Name)}");
                        operationLog.AppendLine(string.Format("Problem with folder: {0}", (currentDirectory + "/" + d.Name)));
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
                    ftp.Disconnect();
                }
            }
            return operationLog.ToString();
        }

        private string NavigateTo(string directoryName, FtpClient ftp, StringBuilder operationLog)
        {
            var currentDirectory = ftp.GetWorkingDirectory() + "/" + directoryName;

            if (!ftp.DirectoryExists(currentDirectory))
            {
                try
                {
                    ftp.CreateDirectory(directoryName);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "NAVIGATETO");
                    operationLog.AppendLine(string.Format("Problem with folder {0}, path: {1}", directoryName, currentDirectory));
                    operationLog.AppendLine("--ex: " + e.Message);
                    if (e.InnerException != null && !string.IsNullOrEmpty(e.InnerException.Message))
                    {
                        operationLog.AppendLine("--innerex: " + e.InnerException.Message);
                    }
                }
            }
            ftp.SetWorkingDirectory(currentDirectory);

            return currentDirectory;
        }

        private void UploadDirectory(string localDirectoryPath, string ftpDirectoryPath, FtpClient ftp, StringBuilder operationLog)
        {
            try
            {
                DirectoryInfo directory = new DirectoryInfo(localDirectoryPath);

                var currentDirectory = NavigateTo(ftpDirectoryPath, ftp, operationLog);

                foreach (var sub in directory.GetDirectories())
                {
                    UploadDirectory(sub.FullName, sub.Name, ftp, operationLog);
                    ftp.SetWorkingDirectory(currentDirectory);
                }

                UploadFilesPerDirectory(currentDirectory, ftp, directory, operationLog);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "UPLOADDIRECTORY");
                operationLog.AppendLine(string.Format("Problem file in path: {0}", ftpDirectoryPath));
                operationLog.AppendLine("--ex: " + e.Message);
                if (e.InnerException != null && !string.IsNullOrEmpty(e.InnerException.Message))
                {
                    operationLog.AppendLine("--innerex: " + e.InnerException.Message);
                }
            }
        }

        private void UploadFilesPerDirectory(string currentDirectory, FtpClient ftp, DirectoryInfo directory, StringBuilder operationLog)
        {
            try
            {
                ftp.SetWorkingDirectory(currentDirectory);

                var filesToUpload = directory.GetFiles();

                if (filesToUpload != null && filesToUpload.Count() > 0)
                {
                    ftp.UploadFiles(directory.GetFiles(), currentDirectory, FtpRemoteExists.Overwrite, createRemoteDir: true, verifyOptions: FtpVerify.Retry, errorHandling: FtpError.Throw);
                }

            }
            catch (Exception e)
            {
                _logger.LogError(e, "UPLOADFILESPERDIRECTORY");
                operationLog.AppendLine(string.Format("Problem with upload file in path: {0}", currentDirectory));
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

        public async Task<byte[]> GetFile(string tenant, string website, string file, string host, string username, string password, string path)
        {
            StringBuilder operationLog = new StringBuilder();

            string baseFtpDirectoryPath = host;

            FtpClient ftp = new FtpClient();
            try
            {
                var directories = path.Split(new string[] { "/" }, StringSplitOptions.RemoveEmptyEntries).ToList();


                ftp.Host = host;
                if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
                {
                    NetworkCredential credentials = new NetworkCredential(username, password);
                    ftp.Credentials = credentials;
                }
                ftp.Connect();

                var currentDirectory = ftp.GetWorkingDirectory();

                foreach (var d in directories)
                {
                    currentDirectory = NavigateTo(d, ftp, operationLog);
                }

                currentDirectory = NavigateTo(tenant, ftp, operationLog);
                currentDirectory = NavigateTo(website, ftp, operationLog);

                if (ftp.FileExists(file))
                {
                    using (Stream stream = ftp.OpenRead(file))
                    {
                        byte[] b = new byte[(int)stream.Length];
                        await stream.ReadAsync(b, 0, (int)stream.Length);
                        return b;
                    }
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
                    ftp.Disconnect();
                }
            }
            return null;
        }
    }
}
