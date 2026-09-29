using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TrinityText.Business;
using TrinityText.Utilities.Transfer;

namespace TrinityText.UnitTests.Offline
{
    [TestClass]
    [TestCategory("Offline")]
    public class TransferServiceTests
    {
        private sealed class RecordingTransfer : ITransferService
        {
            public string Key => "sftp";

            public List<(string Tenant, string Website, string Path)> Uploads { get; } = new();

            public Task<string> Upload(string tenant, string website, DirectoryInfo baseDirectory, string host, string username, string password, string path)
            {
                Uploads.Add((tenant, website, path));
                return Task.FromResult(string.Empty);
            }

            public Task<byte[]> GetFile(string tenant, string website, string file, string host, string username, string password, string path)
                => Task.FromResult<byte[]>(null);
        }

        private static DirectoryInfo NewDirectory()
            => Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "trinity-tests-" + Guid.NewGuid().ToString("N")));

        [TestMethod]
        public async Task RemoteDirectory_ComesFromTheUrlPath_NotFromCredentialsOrPort()
        {
            var recorder = new RecordingTransfer();
            var service = new TransferService([recorder]);

            var result = await service.Upload("tenant", "website", NewDirectory(), "sftp://user:secret@server:2222/dir/sub", "u", "p");

            Assert.IsTrue(result.Success);
            Assert.AreEqual("/dir/sub", recorder.Uploads.Single().Path);
        }

        [TestMethod]
        public async Task UrlWithoutPath_UsesTheRoot()
        {
            var recorder = new RecordingTransfer();
            var service = new TransferService([recorder]);

            await service.Upload("tenant", "website", NewDirectory(), "sftp://server", "u", "p");

            Assert.AreEqual("/", recorder.Uploads.Single().Path);
        }

        [DataTestMethod]
        [DataRow("..", "website")]
        [DataRow("tenant", "a/b")]
        [DataRow("tenant", "")]
        public async Task InvalidTenantOrWebsite_IsRejected_BeforeConnecting(string tenant, string website)
        {
            var recorder = new RecordingTransfer();
            var service = new TransferService([recorder]);
            var local = NewDirectory();

            var result = await service.Upload(tenant, website, local, "sftp://server/dir", "u", "p");

            Assert.IsFalse(result.Success);
            Assert.AreEqual(0, recorder.Uploads.Count);
            Assert.IsFalse(local.Exists, "the local copy is removed anyway");
        }
    }
}
