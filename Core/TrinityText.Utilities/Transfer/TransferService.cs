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
            try
            {
                PathSafety.EnsureValidSegment(tenant, nameof(tenant));
                PathSafety.EnsureValidSegment(website, nameof(website));
                PathSafety.EnsureValidSegment(file, nameof(file));

                var uri = new Uri(host);

                var directories = GetRemoteDirectory(uri);

                var service = GetService(uri);
                var ftpfile = await service.GetFile(tenant, website, file, uri.Host, username, password, directories);

                return ftpfile != null
                    ? OperationResult<byte[]>.MakeSuccess(ftpfile)
                    : OperationResult<byte[]>.MakeFailure([ErrorMessage.Create("GET_FILE", "NOT_FOUND")]);
            }
            catch (Exception ex)
            {
                return OperationResult<byte[]>.MakeFailure([ErrorMessage.Create("GET_FILE", ex.Message)]);
            }
        }

        public async Task<OperationResult> Upload(string tenant, string website, DirectoryInfo baseDirectory, string host, string username, string password)
        {
            var result = OperationResult.MakeSuccess();
            try
            {
                PathSafety.EnsureValidSegment(tenant, nameof(tenant));
                PathSafety.EnsureValidSegment(website, nameof(website));

                var uri = new Uri(host);

                var directories = GetRemoteDirectory(uri);

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
                // the local copy is always removed, but a folder that is already gone must not hide the result
                try
                {
                    if (baseDirectory.Exists)
                    {
                        baseDirectory.Delete(true);
                    }
                }
                catch (IOException)
                {
                }
            }
            return result;
        }

        // Remote folder of the URL ("sftp://host:22/dir/sub" -> "/dir/sub"). The previous string replacement kept
        // the port and the "user:password@" part of the URL as folder names, created on the remote server.
        private static string GetRemoteDirectory(Uri uri)
        {
            var segments = Uri.UnescapeDataString(uri.AbsolutePath).Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Any(s => s == ".." || s == "." || s.Contains((char)92) || s.Any(char.IsControl)))
            {
                throw new ArgumentException("Invalid remote directory in the server address");
            }

            return "/" + string.Join('/', segments);
        }

        private ITransferService GetService(Uri host)
        {
            if (host.Scheme != "ftp" && host.Scheme != "sftp")
            {
                throw new NotSupportedException(host.Scheme);
            }

            if (Services.Count > 1)
            {
                var key = host.Scheme.ToLowerInvariant();

                if (Services.TryGetValue(key, out ITransferService value))
                {
                    return value;
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
