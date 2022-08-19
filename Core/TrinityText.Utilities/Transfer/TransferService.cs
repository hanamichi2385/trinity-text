using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using TrinityText.Business;

namespace TrinityText.Utilities.Transfer
{
    public class TransferService : ITransferServiceCoordinator
    {
        public IDictionary<string, ITransferService> Services { get; private set; }

        public TransferService(IDictionary<string, ITransferService> services)
        {
            Services = services;
        }

        public async Task<byte[]> GetFile(string tenant, string website, string file, string host, string username, string password)
        {
            try
            {
                var uri = new Uri(host);

                var directories = host
                    .Replace(uri.Scheme + "://", "")
                    .Replace(uri.Host, "");

                var service = GetService(uri);
                var ftpfile = await service.GetFile(tenant, website, file, uri.Host, username, password, directories);
            }
            catch
            {

            }
            return null;
        }

        public async Task<string> Upload(string tenant, string website, DirectoryInfo baseDirectory, string host, string username, string password)
        {
            StringBuilder operationLog = new StringBuilder();

            string baseFtpDirectoryPath = host;

            try
            {
                var uri = new Uri(host);

                var directories = host
                    .Replace(uri.Scheme + "://", "")
                    .Replace(uri.Host, "");

                var service = GetService(uri);

                var uploadlog = await service.Upload(tenant, website, baseDirectory, host, username, password, directories);

                if (!string.IsNullOrWhiteSpace(uploadlog))
                {
                    operationLog.AppendLine(uploadlog);
                }
            }
            catch (Exception e)
            {
                operationLog.AppendLine("--ex: " + e.Message);
                if (e.InnerException != null && !string.IsNullOrEmpty(e.InnerException.Message))
                {
                    operationLog.AppendLine("--innerex: " + e.InnerException.Message);
                }
                return operationLog.ToString();
            }
            finally
            {
                baseDirectory.Delete(true);
            }
            return operationLog.ToString();
        }

        private ITransferService GetService(Uri host)
        {
            var key = host.Scheme.ToLower();

            if (Services.ContainsKey(key))
            {
                return Services[key];
            }
            throw new NotSupportedException(host.Scheme);
        }
    }
}
