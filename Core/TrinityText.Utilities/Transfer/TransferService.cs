using Resulz;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TrinityText.Business;

namespace TrinityText.Utilities.Transfer
{
    public class TransferService : ITransferServiceCoordinator
    {
        public IDictionary<string, ITransferService> Services { get; private set; }

        public TransferService(IList<ITransferService> services)
        {
            Services = services.ToDictionary(s => s.Key, s => s);
        }

        public async Task<OperationResult<byte[]>> GetFile(string tenant, string website, string file, string host, string username, string password)
        {
            var result = OperationResult.MakeSuccess();
            try
            {
                var uri = new Uri(host);

                var directories = host
                    .Replace(uri.Scheme + "://", "")
                    .Replace(uri.Host, "");

                var service = GetService(uri);
                var ftpfile = await service.GetFile(tenant, website, file, uri.Host, username, password, directories);
            }
            catch (Exception ex)
            {
                result.AppendError("GET_FILE", ex.Message);
            }
            return result;
        }

        public async Task<OperationResult> Upload(string tenant, string website, DirectoryInfo baseDirectory, string host, string username, string password)
        {
            var result = OperationResult.MakeSuccess();
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
                    result.AppendError("UPLOAD", uploadlog);
                }
            }
            catch (Exception ex)
            {
                result.AppendError("UPLOAD", ex.Message);
            }
            finally
            {
                baseDirectory.Delete(true);
            }
            return result;
        }

        private ITransferService GetService(Uri host)
        {
            if (Services.Count > 1)
            {
                var key = host.Scheme.ToLower();

                if (Services.ContainsKey(key))
                {
                    return Services[key];
                }
                throw new NotSupportedException(host.Scheme);
            }
            else
            {
                return Services.Select(s => s.Value)?.FirstOrDefault();
            }
        }
    }
}
