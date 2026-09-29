using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Threading.Tasks;
using TrinityText.Utilities;

namespace TrinityText.UnitTests.Offline
{
    [TestClass]
    [TestCategory("Offline")]
    public class ZipCompressionServiceTests
    {
        private static string NewTempDirectory()
        {
            var path = Path.Combine(Path.GetTempPath(), "trinity-tests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);
            return path;
        }

        private static ZipCompressionService CreateService() => new(NullLogger<ZipCompressionService>.Instance);

        [TestMethod]
        public async Task CompressThenDecompress_RoundTrip()
        {
            var work = NewTempDirectory();
            try
            {
                var source = Path.Combine(work, "export");
                Directory.CreateDirectory(Path.Combine(source, "sub"));
                await File.WriteAllTextAsync(Path.Combine(source, "sub", "a.txt"), "hello");

                var service = CreateService();
                var zip = await service.CompressFolder(source, work);

                Assert.IsTrue(File.Exists(zip));

                var target = Path.Combine(work, "out");
                await service.DecompressFolder(target, await File.ReadAllBytesAsync(zip));

                Assert.AreEqual("hello", await File.ReadAllTextAsync(Path.Combine(target, "sub", "a.txt")));
            }
            finally
            {
                Directory.Delete(work, true);
            }
        }

        [TestMethod]
        public async Task Decompress_MissingZip_Throws()
        {
            var service = CreateService();
            var never = Path.Combine(Path.GetTempPath(), "never");

            await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => service.DecompressFolder(never, null));
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => service.DecompressFolder(never, []));
        }

        [TestMethod]
        public async Task Decompress_CorruptZip_Throws()
        {
            var work = NewTempDirectory();
            try
            {
                var service = CreateService();

                await Assert.ThrowsExceptionAsync<InvalidDataException>(() => service.DecompressFolder(Path.Combine(work, "out"), [1, 2, 3, 4, 5, 6, 7, 8]));
            }
            finally
            {
                Directory.Delete(work, true);
            }
        }

        [TestMethod]
        public async Task Compress_MissingFolder_ThrowsAndLeavesNoArchive()
        {
            var work = NewTempDirectory();
            try
            {
                var service = CreateService();

                await Assert.ThrowsExceptionAsync<DirectoryNotFoundException>(() => service.CompressFolder(Path.Combine(work, "missing"), work));
                Assert.AreEqual(0, Directory.GetFiles(work, "*.zip").Length);
            }
            finally
            {
                Directory.Delete(work, true);
            }
        }
    }
}
