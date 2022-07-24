using Microsoft.Extensions.Logging;
using Renci.SshNet;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrinityText.Business;

namespace TrinityText.Utilities
{
    public class SFTPTransferService : ITransferService
    {
        private readonly ILogger<SFTPTransferService> _logger;

        public SFTPTransferService(ILogger<SFTPTransferService> logger)
        {
            _logger = logger;
        }

        public async Task<string> Upload(string tenant, string vendor, DirectoryInfo baseDirectory, string host, string username, string password, string path)
        {
            StringBuilder operationLog = new StringBuilder();

            SftpClient ftp = null;
            try
            {
                ftp = new SftpClient(host, username, password);

                ftp.Connect();

                var directories = path.Split(new string[] { "/" }, StringSplitOptions.RemoveEmptyEntries).ToList();


                var currentDirectory = ftp.WorkingDirectory;

                foreach (var d in directories)
                {
                    currentDirectory = NavigateTo(d, ftp, operationLog);
                }

                currentDirectory = NavigateTo(tenant, ftp, operationLog);
                currentDirectory = NavigateTo(vendor, ftp, operationLog);

                UploadFilesPerDirectory(currentDirectory, ftp, baseDirectory, operationLog);

                foreach (var d in baseDirectory.GetDirectories())
                {
                    try
                    {
                        ftp.ChangeDirectory(currentDirectory);

                        UploadDirectory(d.FullName, d.Name, ftp, operationLog);
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, "UPLOAD");
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
                //baseDirectory.Delete(true);

                if (ftp != null && ftp.IsConnected)
                {
                    ftp.Disconnect();
                }
            }
            return await Task.FromResult(operationLog.ToString());
        }

        public async Task<byte[]> GetFile(string tenant, string vendor, string file, string host, string username, string password, string path)
        {
            StringBuilder operationLog = new StringBuilder();

            string baseFtpDirectoryPath = host;

            SftpClient ftp = null;
            try
            {
                var directories = path.Split(new string[] { "/" }, StringSplitOptions.RemoveEmptyEntries).ToList();

                ftp = new SftpClient(host, username, password);

                ftp.Connect();

                var currentDirectory = ftp.WorkingDirectory;

                foreach (var d in directories)
                {
                    currentDirectory = NavigateTo(d, ftp, operationLog);
                }

                currentDirectory = NavigateTo(tenant, ftp, operationLog);
                currentDirectory = NavigateTo(vendor, ftp, operationLog);

                if (ftp.Exists(currentDirectory + "/" + file))
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
                _logger.LogError(e, "GETFILE");
            }
            finally
            {
                if (ftp != null && ftp.IsConnected)
                {
                    ftp.Disconnect();
                }
            }
            return null;
        }

        private string NavigateTo(string directoryName, SftpClient ftp, StringBuilder operationLog)
        {
            var currentDirectory = ftp.WorkingDirectory + "/" + directoryName;

            if (!ftp.Exists(currentDirectory))
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
            ftp.ChangeDirectory(directoryName);

            return currentDirectory;
        }

        private void UploadFilesPerDirectory(string currentDirectory, SftpClient ftp, DirectoryInfo directory, StringBuilder operationLog)
        {
            try
            {
                if (!ftp.WorkingDirectory.Equals(currentDirectory, StringComparison.InvariantCultureIgnoreCase))
                {
                    ftp.ChangeDirectory(currentDirectory);
                }


                var filesToUpload = directory.GetFiles();

                if (filesToUpload != null && filesToUpload.Count() > 0)
                {
                    //ftp.UploadFiles(directory.GetFiles(), currentDirectory, FtpRemoteExists.Overwrite, createRemoteDir: true, verifyOptions: FtpVerify.Retry, errorHandling: FtpError.Throw);
                    foreach (var f in directory.GetFiles())
                    {
                        using (var stream = f.Open(FileMode.Open))
                        {
                            ftp.UploadFile(stream, f.Name);
                            stream.Close();
                        }
                    }
                }

            }
            catch (Exception e)
            {
                _logger.LogError(e, "UPLOADFILESPERDIRECTORY");
                operationLog.AppendLine(string.Format("Problem with file in path: {0}", currentDirectory));
                operationLog.AppendLine("--ex: " + e.Message);
                if (e.InnerException != null && !string.IsNullOrEmpty(e.InnerException.Message))
                {
                    operationLog.AppendLine("--innerex: " + e.InnerException.Message);
                }
            }
        }

        private void UploadDirectory(string localDirectoryPath, string ftpDirectoryPath, SftpClient ftp, StringBuilder operationLog)
        {
            try
            {
                DirectoryInfo directory = new DirectoryInfo(localDirectoryPath);

                var currentDirectory = NavigateTo(ftpDirectoryPath, ftp, operationLog);

                foreach (var sub in directory.GetDirectories())
                {
                    UploadDirectory(sub.FullName, sub.Name, ftp, operationLog);
                    ftp.ChangeDirectory(currentDirectory);
                }

                UploadFilesPerDirectory(currentDirectory, ftp, directory, operationLog);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "UPLOADDIRECTORY");
                operationLog.AppendLine(string.Format("Problem with folder: {0}", ftpDirectoryPath));
                operationLog.AppendLine("--ex: " + e.Message);
                if (e.InnerException != null && !string.IsNullOrEmpty(e.InnerException.Message))
                {
                    operationLog.AppendLine("--innerex: " + e.InnerException.Message);
                }
            }
        }
    }
}
