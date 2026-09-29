using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Resulz;
using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using TrinityText.Business;
using TrinityText.Business.Services.Impl;
using TrinityText.ServiceBus.MassTransit.Services;
using TrinityText.Utilities;

namespace TrinityText.UnitTests.Offline
{
    /// <summary>The whole export (texts, pages, files -> ZIP) of a publication, with fake data services.</summary>
    [TestClass]
    [TestCategory("Offline")]
    public class PublicationExportTests
    {
        private sealed class Scenario : IDisposable
        {
            public string Root { get; } = Path.Combine(Path.GetTempPath(), "trinity-export-" + Guid.NewGuid().ToString("N"));

            public int WidgetLookups { get; private set; }

            public bool FilesFail { get; set; }

            public byte[] StoredZip { get; set; }

            public List<string> Uploaded { get; } = new();

            public MassTransitPublicationSupportService Service { get; }

            public Scenario()
            {
                Directory.CreateDirectory(Root);

                var texts = Fake.Of<ITextService>((m, a) =>
                {
                    var site = new Dictionary<string, ReadOnlyCollection<TextDTO>>
                    {
                        ["S1"] = new List<TextDTO>
                        {
                            new() { Name = "HELLO", Language = "it", TextRevision = new TextRevisionDTO { Content = "ciao \u0001 <b>" } },
                            new() { Name = "BYE", Language = "it", Country = "IT", TextType = new TextTypeDTO { Id = 1, Name = "labels", Subfolder = "sub" }, TextRevision = new TextRevisionDTO { Content = "addio" } },
                        }.AsReadOnly(),
                    };
                    return Task.FromResult(OperationResult<FrozenDictionary<string, ReadOnlyCollection<TextDTO>>>.MakeSuccess(site.ToFrozenDictionary()));
                });

                var schemaType = new PageTypeDTO { Id = 5, Name = "News", Schema = "<root id=\"news\"><content id=\"item\"><textatom id=\"title\"/></content></root>", OutputFilename = "news" };
                var pages = Fake.Of<IPageService>((m, a) =>
                {
                    var site = new Dictionary<string, ReadOnlyCollection<PageDTO>>
                    {
                        ["S1"] = new List<PageDTO>
                        {
                            new() { Id = 1, Title = "p1", Language = "it", PageType = schemaType, Content = "<item><title><![CDATA[@[WIDGET(HEAD)] one]]></title></item>" },
                            new() { Id = 2, Title = "p2", Language = "it", PageType = schemaType, Content = "<item><title><![CDATA[@[WIDGET(HEAD)] two]]></title></item>" },
                        }.AsReadOnly(),
                    };
                    return Task.FromResult(OperationResult<FrozenDictionary<string, ReadOnlyCollection<PageDTO>>>.MakeSuccess(site.ToFrozenDictionary()));
                });

                var widgetService = Fake.Of<IWidgetService>((m, a) =>
                {
                    WidgetLookups++;
                    return Task.FromResult(OperationResult<WidgetDTO>.MakeSuccess(new WidgetDTO { Content = "HEAD[" + a[3] + "]" }));
                });

                var fileId = Guid.NewGuid();
                var fileManager = Fake.Of<IFileManagerService>((m, a) =>
                {
                    switch (m.Name)
                    {
                        case nameof(IFileManagerService.GetAllFoldersByWebsite):
                            return Task.FromResult(FilesFail
                                ? OperationResult<FolderDTO>.MakeFailure([ErrorMessage.Create("GET", "GENERIC_ERROR")])
                                : OperationResult<FolderDTO>.MakeSuccess(new FolderDTO
                                {
                                    Id = 1,
                                    Name = "W",
                                    SubFolders = [new FolderDTO { Id = 2, Name = "Images" }],
                                }));
                        case nameof(IFileManagerService.GetFilesByFolder):
                            var folderId = (int)a[1];
                            return Task.FromResult(OperationResult<IReadOnlyCollection<FileDTO>>.MakeSuccess(
                                folderId == 2 ? [new FileDTO { Id = fileId, Filename = "logo.png" }] : []));
                        case nameof(IFileManagerService.GetFileContent):
                            return Task.FromResult(OperationResult<byte[]>.MakeSuccess([1, 2, 3]));
                        default:
                            throw new NotImplementedException(m.Name);
                    }
                });

                var publications = Fake.Of<IPublicationService>((m, a) =>
                {
                    if (m.Name == nameof(IPublicationService.CopyZipTo))
                    {
                        if (StoredZip == null)
                        {
                            return Task.FromResult(OperationResult.MakeFailure([ErrorMessage.Create("COPY_ZIP", "NOT_FOUND")]));
                        }

                        ((Stream)a[1]).Write(StoredZip);
                        return Task.FromResult(OperationResult.MakeSuccess());
                    }

                    throw new NotImplementedException(m.Name);
                });
                var transfers = Fake.Of<ITransferServiceCoordinator>((m, a) =>
                {
                    if (m.Name == nameof(ITransferServiceCoordinator.Upload))
                    {
                        // the local copy is removed right after the upload: record what was there
                        var directory = (DirectoryInfo)a[2];
                        Uploaded.AddRange(directory.GetFiles("*", SearchOption.AllDirectories)
                            .Select(f => Path.GetRelativePath(directory.FullName, f.FullName).Replace(Path.DirectorySeparatorChar, '/')));
                        return Task.FromResult(OperationResult.MakeSuccess());
                    }

                    throw new NotImplementedException(m.Name);
                });

                Service = new MassTransitPublicationSupportService(
                    publications,
                    texts,
                    pages,
                    new PageSchemaService(new WidgetUtilities(fileManager, widgetService)),
                    fileManager,
                    new ZipCompressionService(NullLogger<ZipCompressionService>.Instance),
                    transfers,
                    NullLogger<MassTransitPublicationSupportService>.Instance,
                    Options.Create(new PublicationSupportOptions { LocalDirectory = Root }));
            }

            public PayloadDTO Payload(string[] languages = null) => new()
            {
                Website = "W",
                Tenant = "T",
                Sites = [new SiteConfiguration { Site = "S1", Languages = languages ?? ["it"] }],
                TextTypes = [new TextTypeDTO { Id = 1, Name = "labels" }],
            };

            public void Dispose()
            {
                if (Directory.Exists(Root))
                {
                    Directory.Delete(Root, true);
                }
            }
        }

        private static Dictionary<string, string> ReadZip(string path)
        {
            using var archive = ZipFile.OpenRead(path);
            return archive.Entries
                .Where(e => e.Length > 0)
                .ToDictionary(
                    e => e.FullName.Replace('\\', '/'),
                    e =>
                    {
                        using var reader = new StreamReader(e.Open());
                        return reader.ReadToEnd();
                    });
        }

        [DataTestMethod]
        [DataRow(PublicationFormat.XML)]
        [DataRow(PublicationFormat.JSON)]
        public async Task Export_ProducesTextsPagesAndFiles_InAPortableLayout(PublicationFormat format)
        {
            using var scenario = new Scenario();

            var rs = await scenario.Service.CreateExportFile(1, scenario.Payload(), PublicationType.All, format, DateTime.MinValue, true, "me", null);

            Assert.IsTrue(rs.Success, string.Join(",", rs.Errors?.Select(e => e.Description) ?? []));
            var zip = ReadZip(rs.Value);
            var extension = format.ToString().ToLowerInvariant();

            // no directory or file name contains a backslash (that would be a single odd name on Linux)
            Assert.IsFalse(zip.Keys.Any(k => k.Contains('\\')));
            CollectionAssert.Contains(zip.Keys.ToList(), $"Text/S1/it/W.{extension}");
            CollectionAssert.Contains(zip.Keys.ToList(), $"Text/S1/it/sub/labels.{extension}");
            CollectionAssert.Contains(zip.Keys.ToList(), $"Text/S1/it/news.{extension}");
            CollectionAssert.Contains(zip.Keys.ToList(), "W/Images/logo.png".Replace("W/", string.Empty));
            CollectionAssert.Contains(zip.Keys.ToList(), "trinity-text.txt");

            // an invalid XML character in a text does not abort the export
            Assert.IsFalse(zip[$"Text/S1/it/W.{extension}"].Contains('\u0001'));
            StringAssert.Contains(zip[$"Text/S1/it/news.{extension}"], "HEAD[it] one");

            // the temporary folder is gone, only the ZIP is left
            Assert.AreEqual(0, Directory.GetDirectories(scenario.Root).Length);
        }

        [TestMethod]
        public async Task Export_ResolvesEachWidgetOnce_ForAllThePagesOfTheExport()
        {
            using var scenario = new Scenario();

            var rs = await scenario.Service.CreateExportFile(1, scenario.Payload(), PublicationType.Pages, PublicationFormat.XML, DateTime.MinValue, true, "me", null);

            Assert.IsTrue(rs.Success);
            Assert.AreEqual(1, scenario.WidgetLookups, "two pages use the widget HEAD: one lookup");
        }

        [TestMethod]
        public async Task Export_WithUnreadableFiles_FailsAndLeavesNothingBehind()
        {
            using var scenario = new Scenario { FilesFail = true };

            // Generate() turns the exception into a failed publication
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                () => scenario.Service.CreateExportFile(1, scenario.Payload(), PublicationType.All, PublicationFormat.XML, DateTime.MinValue, true, "me", null));

            Assert.AreEqual(0, Directory.GetFileSystemEntries(scenario.Root).Length, "no partial export, no partial ZIP");
        }

        [TestMethod]
        public async Task Export_WithADangerousPayload_IsRefused()
        {
            using var scenario = new Scenario();
            var publication = new PublicationDTO { Id = 1, Website = "W", DataType = PublicationType.All, Format = PublicationFormat.XML, CreationUser = "me" };
            var payload = scenario.Payload();
            payload.Sites[0].Site = "..";
            publication.SetPayload(payload);

            var rs = await scenario.Service.Generate(publication);

            Assert.IsFalse(rs.Success);
            Assert.AreEqual(0, Directory.GetFileSystemEntries(scenario.Root).Length);
        }

        [TestMethod]
        public async Task Publish_StreamsTheStoredZip_ExtractsItAndUploadsTheFiles()
        {
            using var scenario = new Scenario();

            // build a real export, keep its ZIP as "the one stored in the database"
            var export = await scenario.Service.CreateExportFile(1, scenario.Payload(), PublicationType.All, PublicationFormat.XML, DateTime.MinValue, true, "me", null);
            scenario.StoredZip = await File.ReadAllBytesAsync(export.Value);
            File.Delete(export.Value);

            var publication = new PublicationDTO
            {
                Id = 1,
                Website = "W",
                FtpServer = new FTPServerDTO { Host = "sftp://server/dir", Username = "u", Password = "p" },
            };
            publication.SetPayload(scenario.Payload());

            var rs = await scenario.Service.Publish(publication);

            Assert.IsTrue(rs.Success, string.Join(",", rs.Errors?.Select(e => e.Description) ?? []));
            CollectionAssert.Contains(scenario.Uploaded, "Text/S1/it/W.xml");
            CollectionAssert.Contains(scenario.Uploaded, "Images/logo.png");

            // no temporary ZIP or extraction folder left in the working directory
            Assert.AreEqual(0, Directory.GetFileSystemEntries(scenario.Root, "*", SearchOption.AllDirectories).Count(e => e.EndsWith(".zip")));
        }

        [TestMethod]
        public async Task Publish_WithoutAnyStoredZip_Fails()
        {
            using var scenario = new Scenario();
            var publication = new PublicationDTO { Id = 1, Website = "W", FtpServer = new FTPServerDTO { Host = "sftp://server", Username = "u", Password = "p" } };
            publication.SetPayload(scenario.Payload());

            var rs = await scenario.Service.Publish(publication);

            Assert.IsFalse(rs.Success);
            Assert.AreEqual(0, scenario.Uploaded.Count);
        }
    }
}
