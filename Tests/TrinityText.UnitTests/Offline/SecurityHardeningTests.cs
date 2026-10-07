using Ganss.Excel;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Resulz;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TrinityText.Business;
using TrinityText.Business.Services.Impl;
using TrinityText.Utilities;
using TrinityText.Utilities.Excel;
using TrinityText.Utilities.Transfer;

namespace TrinityText.UnitTests.Offline
{
    /// <summary>Regression tests for the hardening of the audit (round 2, phase E).</summary>
    [TestClass]
    [TestCategory("Offline")]
    public class SecurityHardeningTests
    {

        // ------------------------------------------------------------ PathSafety

        [DataTestMethod]
        [DataRow("CON")]
        [DataRow("con")]
        [DataRow("NUL.txt")]
        [DataRow("COM1")]
        [DataRow("lpt9.log")]
        [DataRow("AUX")]
        public void PathSafety_RejectsWindowsDeviceNames(string name) => Assert.IsFalse(PathSafety.IsValidSegment(name));

        [DataTestMethod]
        [DataRow("console")]
        [DataRow("COM10")]
        [DataRow("config.xml")]
        [DataRow("my.con.txt")]
        public void PathSafety_AcceptsNamesThatOnlyLookReserved(string name) => Assert.IsTrue(PathSafety.IsValidSegment(name));

        [TestMethod]
        public void PathSafety_RejectsControlCharactersTrailingDotsAndLongNames()
        {
            Assert.IsFalse(PathSafety.IsValidSegment("a\nb"), "line feed");
            Assert.IsFalse(PathSafety.IsValidSegment("a\rb"), "carriage return");
            Assert.IsFalse(PathSafety.IsValidSegment("name."), "trailing dot");
            Assert.IsFalse(PathSafety.IsValidSegment(new string('a', 256)), "too long");
            Assert.IsTrue(PathSafety.IsValidSegment(new string('a', 255)));
        }

        // ------------------------------------------------------------ widgets

        private sealed class Widgets
        {
            public Dictionary<string, string> Content { get; } = new();

            public WidgetUtilities Utilities { get; }

            public Widgets()
            {
                var widgetService = Fake.Of<IWidgetService>((m, a) => Task.FromResult(Content.TryGetValue((string)a[0], out var c)
                    ? OperationResult<WidgetDTO>.MakeSuccess(new WidgetDTO { Content = c })
                    : OperationResult<WidgetDTO>.MakeFailure([ErrorMessage.Create("GET_BYKEYS", "NOT_FOUND")])));
                var fileManager = Fake.Of<IFileManagerService>((m, a) => throw new NotImplementedException(m.Name));
                Utilities = new WidgetUtilities(fileManager, widgetService);
            }
        }

        [TestMethod]
        public async Task Widgets_ComposedFromSafeFragments_CannotCloseTheCdata()
        {
            // "]" + "]>" are harmless on their own; nested they form the CDATA terminator
            var w = new Widgets();
            w.Content["A"] = "]@[WIDGET(B)]";
            w.Content["B"] = "]>";

            var result = await w.Utilities.ReplaceWidget("<![CDATA[@[WIDGET(A)]]]>", "S", "W", "T", "it");

            var element = System.Xml.Linq.XElement.Parse("<r>" + result + "</r>");
            Assert.AreEqual("]]>", element.Value);
        }

        [TestMethod]
        public async Task Widgets_ManyUnclosedOpeners_AreProcessedInLinearTime()
        {
            var w = new Widgets();
            var text = string.Concat(Enumerable.Repeat("@[WIDGET(", 200_000)); // ~1.8 MB, no closing ")]"

            var watch = Stopwatch.StartNew();
            var result = await w.Utilities.ReplaceWidget(text, "S", "W", "T", "it");
            watch.Stop();

            Assert.AreEqual(text, result);
            Assert.IsTrue(watch.Elapsed < TimeSpan.FromSeconds(8), $"took {watch.Elapsed}");
        }

        [TestMethod]
        public async Task Widgets_OversizedInput_IsRejected()
        {
            var w = new Widgets();

            await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                () => w.Utilities.ReplaceWidget(new string('x', 6 * 1024 * 1024), "S", "W", "T", "it"));
        }

        [TestMethod]
        public async Task Widgets_KeyLongerThanTheLimit_IsNotAWidgetToken()
        {
            var w = new Widgets();
            var token = "@[WIDGET(" + new string('k', 300) + ")]";

            Assert.AreEqual(token, await w.Utilities.ReplaceWidget(token, "S", "W", "T", "it"));
        }

        // ------------------------------------------------------------ XML

        private static string Nested(int levels)
            => string.Concat(Enumerable.Repeat("<a>", levels)) + string.Concat(Enumerable.Repeat("</a>", levels));

        [TestMethod]
        public void SafeXml_RejectsDoctypeAndExcessiveDepth()
        {
            Assert.ThrowsException<System.Xml.XmlException>(() => SafeXml.ParseDocument("<!DOCTYPE r [<!ENTITY x \"y\">]><r>&x;</r>"));
            Assert.ThrowsException<System.Xml.XmlException>(() => SafeXml.ParseDocument(Nested(SafeXml.MaxDepth + 1)));
            Assert.IsNotNull(SafeXml.ParseDocument(Nested(SafeXml.MaxDepth)));
        }

        [TestMethod]
        public async Task Page_WithMalformedOrTooDeepContent_IsRefusedOnSave()
        {
            var service = new PageService(null, null, NullLogger<PageService>.Instance);

            foreach (var content in new[] { "<a><b></a>", Nested(SafeXml.MaxDepth + 1), "<!DOCTYPE r [<!ENTITY x \"y\">]><r/>" })
            {
                var rs = await service.Save(new PageDTO { Content = content });

                Assert.IsFalse(rs.Success);
                Assert.AreEqual("INVALID_CONTENT", rs.Errors.First().Description);
            }
        }

        [TestMethod]
        public async Task Widget_TooLarge_IsRefusedOnSave()
        {
            var service = new WidgetService(null, NullLogger<WidgetService>.Instance);

            var rs = await service.Save(new WidgetDTO { Key = "k", Language = "it", Content = new string('x', 1_000_001) });

            Assert.IsFalse(rs.Success);
            Assert.AreEqual("CONTENT_TOO_LARGE", rs.Errors.First().Description);
        }

        // ------------------------------------------------------------ paging

        [TestMethod]
        public void GetPage_IsBounded_AndDoesNotOverflow()
        {
            var numbers = Enumerable.Range(1, 5000).AsQueryable();

            Assert.AreEqual(PaginationExtensions.MaxPageSize, numbers.GetPage(0, int.MaxValue).Count());
            Assert.AreEqual(1, numbers.GetPage(0, 0).Count(), "a non-positive size becomes 1");
            Assert.AreEqual(0, numbers.GetPage(int.MaxValue, 1000).Count(), "page * size overflows int");
            Assert.AreEqual(1, numbers.GetPage(-5, 1).Single(), "negative pages start from the beginning");
        }

        // ------------------------------------------------------------ files and addresses

        [DataTestMethod]
        [DataRow("shell.aspx")]
        [DataRow("handler.ASHX")]
        [DataRow("web.config")]
        [DataRow("run.exe")]
        [DataRow(".htaccess")]
        [DataRow("index.php")]
        public void UploadPolicy_BlocksServerSideAndExecutableFiles(string name) => Assert.IsFalse(FileUploadPolicy.IsAllowed(name));

        [DataTestMethod]
        [DataRow("photo.png")]
        [DataRow("brochure.pdf")]
        [DataRow("style.css")]
        [DataRow("data.json")]
        public void UploadPolicy_AllowsAssets(string name) => Assert.IsTrue(FileUploadPolicy.IsAllowed(name));

        [DataTestMethod]
        [DataRow("javascript:alert(1)")]
        [DataRow("ftp://cdn.example.com")]
        [DataRow("not a url")]
        public async Task CdnBaseUrl_MustBeHttp(string url)
        {
            var service = new CDNSettingService(null, null, NullLogger<CDNSettingService>.Instance);

            var rs = await service.Save(new CdnServerDTO { Name = "cdn", BaseUrl = url }, []);

            Assert.IsFalse(rs.Success);
            Assert.AreEqual("INVALID_URL", rs.Errors.First().Description);
        }

        [DataTestMethod]
        [DataRow("http://server/dir")]
        [DataRow("sftp://user:secret@server/dir")]
        [DataRow("server")]
        public async Task FtpHost_MustBeAnFtpOrSftpAddressWithoutCredentials(string host)
        {
            var service = new FTPServerService(null, NullLogger<FTPServerService>.Instance);

            var rs = await service.Save(new FTPServerDTO { Name = "ftp", Host = host, Username = "u", Password = "p" });

            Assert.IsFalse(rs.Success);
            Assert.AreEqual("INVALID_HOST", rs.Errors.First().Description);
        }

        private sealed class NeverCalled : ITransferService
        {
            public string Key => "sftp";

            public Task<string> Upload(string tenant, string website, DirectoryInfo baseDirectory, string host, string username, string password, string path, int? port)
                => throw new InvalidOperationException("must not be reached");

            public Task<byte[]> GetFile(string tenant, string website, string file, string host, string username, string password, string path, int? port)
                => throw new InvalidOperationException("must not be reached");
        }

        [TestMethod]
        public async Task Transfer_OnlyFtpAndSftpSchemes()
        {
            var service = new TransferService([new NeverCalled()]);
            var local = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "trinity-tests-" + Guid.NewGuid().ToString("N")));

            var rs = await service.Upload("tenant", "website", local, "http://server/dir", "u", "p", null);

            Assert.IsFalse(rs.Success);
        }

        // ------------------------------------------------------------ import scope

        [TestMethod]
        public async Task ImportTexts_OfAnotherWebsite_IsRefused_BeforeAnyWrite()
        {
            var service = new TextService(null, null, null, NullLogger<TextService>.Instance);
            var texts = new List<TextDTO>
            {
                new() { Name = "a", Language = "it", Website = "mine", TextRevision = new TextRevisionDTO { Content = "x" } },
                new() { Name = "b", Language = "it", Website = "victim", TextRevision = new TextRevisionDTO { Content = "y" } },
            };

            var rs = await service.ImportTexts(null, texts, true, ["mine"]);

            Assert.IsFalse(rs.Success);
            Assert.AreEqual("FORBIDDEN_WEBSITE", rs.Errors.First().Description);
        }

        private static async Task<byte[]> Workbook(params (string Key, string Website, string Site, string Language)[] rows)
        {
            var data = rows.Select(r => new { KEY = r.Key, TYPE = "*", WEBSITE = r.Website, SITE = r.Site, COUNTRY = "*", LANGUAGE = r.Language, TEXT = "content" }).ToArray();
            using var stream = new MemoryStream();
            await new ExcelMapper().SaveAsync(stream, data, "texts", xlsx: true);
            return stream.ToArray();
        }

        private static ExcelMapperService ExcelService()
        {
            var textTypes = Fake.Of<ITextTypeService>((m, a) => Task.FromResult(OperationResult<IList<TextTypeDTO>>.MakeSuccess(new List<TextTypeDTO>())));
            return new ExcelMapperService(textTypes, Options.Create(new ExcelOptions()), NullLogger<ExcelMapperService>.Instance);
        }

        [TestMethod]
        public async Task ExcelImport_RowsOfAnotherWebsite_AreRefused()
        {
            var bytes = await Workbook(("k1", "mine", "*", "it"), ("k2", "victim", "*", "it"));

            using (var stream = new MemoryStream(bytes))
            {
                await Assert.ThrowsExceptionAsync<UnauthorizedAccessException>(() => ExcelService().GetTextsFromStream("me", stream, ["mine"]));
            }

            using (var stream = new MemoryStream(bytes))
            {
                var texts = await ExcelService().GetTextsFromStream("me", stream, ["mine", "victim"]);
                Assert.AreEqual(2, texts.Length);
            }
        }

        [TestMethod]
        public async Task ExcelImport_SiteOrLanguageThatIsNotAFolderName_IsRefused()
        {
            var bytes = await Workbook(("k1", "mine", "../up", "it"));

            using var stream = new MemoryStream(bytes);
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => ExcelService().GetTextsFromStream("me", stream, ["mine"]));
        }

        [TestMethod]
        public async Task ExcelImport_BlankScopeCells_BecomeNull()
        {
            var bytes = await Workbook(("k1", "*", "*", "it"));

            using var stream = new MemoryStream(bytes);
            var text = (await ExcelService().GetTextsFromStream("me", stream)).Single();

            Assert.IsNull(text.Website);
            Assert.IsNull(text.Site);
            Assert.IsNull(text.Country);
        }
    }
}
