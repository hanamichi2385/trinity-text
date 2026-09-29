using AutoMapper;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Resulz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TrinityText.Business;
using TrinityText.Business.Services.Impl;
using TrinityText.Domain;

namespace TrinityText.UnitTests.Offline
{
    /// <summary>The real services on EF Core and NHibernate (SQLite in memory), no SQL Server needed.</summary>
    [TestClass]
    [TestCategory("Offline")]
    public class ServiceBehaviorTests
    {
        public static IEnumerable<object[]> Providers => new[] { new object[] { "EF" }, new object[] { "NH" } };

        private static IMapper Mapper { get; } =
            new MapperConfiguration(cfg => cfg.AddProfile<BusinessMapperProfile>(), NullLoggerFactory.Instance).CreateMapper();

        private static TextService Texts(ProviderScope scope)
            => new(scope.Repo<Text>(), scope.Repo<TextRevision>(), scope.Repo<TextType>(), Mapper, NullLogger<TextService>.Instance);

        private static TextDTO NewText(string name, string content, string language = "it", string website = null, string site = null, int? textTypeId = null)
            => new()
            {
                Name = name,
                Language = language,
                Website = website,
                Site = site,
                TextTypeId = textTypeId,
                Active = true,
                TextRevision = new TextRevisionDTO { Content = content, CreationUser = "me" },
            };

        private static async Task<int> CreateTextType(ProviderFixture db, string name)
        {
            using var scope = db.NewScope();
            var created = await scope.Repo<TextType>().Create(new TextType { CONTENTTYPE = name });
            return created.ID;
        }

        // ---------------------------------------------------------------- texts

        [DataTestMethod]
        [DynamicData(nameof(Providers))]
        public async Task Text_SaveCreate_ThenUpdate_AddsARevisionOnlyWhenTheContentChanges(string provider)
        {
            using var db = ProviderFixture.Create(provider);
            var typeId = await CreateTextType(db, "labels");

            int id;
            using (var scope = db.NewScope())
            {
                var rs = await Texts(scope).Save(NewText("key", "v1", textTypeId: typeId));
                Assert.IsTrue(rs.Success, string.Join(",", rs.Errors?.Select(e => e.Description) ?? []));
                id = rs.Value.Id.Value;
                Assert.AreEqual("KEY", rs.Value.Name);
            }

            using (var scope = db.NewScope())
            {
                var dto = (await Texts(scope).Get(id)).Value;
                Assert.AreEqual("v1", dto.TextRevision.Content);
                Assert.AreEqual(typeId, dto.TextTypeId);

                dto.TextRevision.Content = "v2";
                var rs = await Texts(scope).Save(dto);
                Assert.IsTrue(rs.Success, string.Join(",", rs.Errors?.Select(e => e.Description) ?? []));
            }

            using (var scope = db.NewScope())
            {
                // same content again: no new revision
                var dto = (await Texts(scope).Get(id)).Value;
                Assert.AreEqual("v2", dto.TextRevision.Content);
                Assert.IsTrue((await Texts(scope).Save(dto)).Success);
            }

            using (var scope = db.NewScope())
            {
                var revisions = await scope.Repo<TextRevision>().ToListAsync(
                    scope.Repo<TextRevision>().Repository.Where(r => r.FK_TEXT == id).OrderBy(r => r.REVISION_NUMBER));
                CollectionAssert.AreEqual(new[] { 1, 2 }, revisions.Select(r => r.REVISION_NUMBER).ToArray());
                CollectionAssert.AreEqual(new[] { "v1", "v2" }, revisions.Select(r => r.CONTENT).ToArray());
            }
        }

        [DataTestMethod]
        [DynamicData(nameof(Providers))]
        public async Task Text_ChangingTheTypeToNone_ClearsTheForeignKey(string provider)
        {
            using var db = ProviderFixture.Create(provider);
            var typeId = await CreateTextType(db, "labels");

            int id;
            using (var scope = db.NewScope())
            {
                id = (await Texts(scope).Save(NewText("key", "v1", textTypeId: typeId))).Value.Id.Value;
            }

            using (var scope = db.NewScope())
            {
                var dto = (await Texts(scope).Get(id)).Value;
                dto.TextTypeId = null;
                dto.TextType = null;
                Assert.IsTrue((await Texts(scope).Save(dto)).Success);
            }

            using (var scope = db.NewScope())
            {
                Assert.IsNull((await scope.Repo<Text>().Read(id)).FK_TEXTTYPE);
            }
        }

        [DataTestMethod]
        [DynamicData(nameof(Providers))]
        public async Task Text_DuplicateKey_IsRejected(string provider)
        {
            using var db = ProviderFixture.Create(provider);

            using var scope = db.NewScope();
            Assert.IsTrue((await Texts(scope).Save(NewText("key", "a", website: "W"))).Success);

            var duplicate = await Texts(scope).Save(NewText("key", "b", website: "W"));

            Assert.IsFalse(duplicate.Success);
            Assert.AreEqual("DUPLICATED", duplicate.Errors.First().Description);
        }

        [DataTestMethod]
        [DynamicData(nameof(Providers))]
        public async Task Import_CreatesNewTexts_CollapsesRepeatedRows_AndOnlyOverridesWhenAsked(string provider)
        {
            using var db = ProviderFixture.Create(provider);

            using (var scope = db.NewScope())
            {
                var rs = await Texts(scope).ImportTexts(null, [NewText("a", "1"), NewText("b", "2"), NewText("A", "1 again")], false);
                Assert.IsTrue(rs.Success);
                Assert.AreEqual(2, rs.Value, "A and a are the same key: one text");
            }

            using (var scope = db.NewScope())
            {
                var rs = await Texts(scope).ImportTexts(null, [NewText("a", "changed"), NewText("c", "3")], false);
                Assert.AreEqual(1, rs.Value, "existing texts are left alone without override");
            }

            using (var scope = db.NewScope())
            {
                var rs = await Texts(scope).ImportTexts(null, [NewText("a", "changed")], true);
                Assert.AreEqual(1, rs.Value);
            }

            using (var scope = db.NewScope())
            {
                var all = await scope.Repo<Text>().ToListAsync(scope.Repo<Text>().Repository.OrderBy(t => t.NAME));
                CollectionAssert.AreEqual(new[] { "A", "B", "C" }, all.Select(t => t.NAME).ToArray());

                var a = all.Single(t => t.NAME == "A");
                var revisions = await scope.Repo<TextRevision>().ToListAsync(scope.Repo<TextRevision>().Repository.Where(r => r.FK_TEXT == a.ID));
                Assert.AreEqual(2, revisions.Count, "overriding with different content adds a revision");
            }
        }

        [DataTestMethod]
        [DynamicData(nameof(Providers))]
        public async Task CleanRevisions_KeepsTheLatestRevisionsOfEveryText(string provider)
        {
            using var db = ProviderFixture.Create(provider);

            int id;
            using (var scope = db.NewScope())
            {
                id = (await Texts(scope).Save(NewText("key", "v1"))).Value.Id.Value;
            }

            for (var version = 2; version <= 5; version++)
            {
                using var scope = db.NewScope();
                var dto = (await Texts(scope).Get(id)).Value;
                dto.TextRevision.Content = "v" + version;
                Assert.IsTrue((await Texts(scope).Save(dto)).Success);
            }

            using (var scope = db.NewScope())
            {
                Assert.IsFalse((await Texts(scope).CleanRevisions(0)).Success);
                Assert.IsTrue((await Texts(scope).CleanRevisions(2)).Success);
            }

            using (var scope = db.NewScope())
            {
                var revisions = await scope.Repo<TextRevision>().ToListAsync(
                    scope.Repo<TextRevision>().Repository.Where(r => r.FK_TEXT == id).OrderBy(r => r.REVISION_NUMBER));
                CollectionAssert.AreEqual(new[] { "v4", "v5" }, revisions.Select(r => r.CONTENT).ToArray());
            }
        }

        [DataTestMethod]
        [DynamicData(nameof(Providers))]
        public async Task PublishableTexts_PickTheMostSpecificText(string provider)
        {
            using var db = ProviderFixture.Create(provider);

            using (var scope = db.NewScope())
            {
                var texts = Texts(scope);
                Assert.IsTrue((await texts.Save(NewText("greeting", "global"))).Success);
                Assert.IsTrue((await texts.Save(NewText("greeting", "website", website: "W"))).Success);
                Assert.IsTrue((await texts.Save(NewText("greeting", "site one", website: "W", site: "S1"))).Success);
                Assert.IsTrue((await texts.Save(NewText("only-global", "g"))).Success);
            }

            using (var scope = db.NewScope())
            {
                var rs = await Texts(scope).GetPublishableTextsByWebsite("W", new Dictionary<string, string[]> { ["S1"] = ["it"], ["S2"] = ["it"] }, []);

                Assert.IsTrue(rs.Success);
                var s1 = rs.Value["S1"].ToDictionary(t => t.Name, t => t.TextRevision.Content);
                var s2 = rs.Value["S2"].ToDictionary(t => t.Name, t => t.TextRevision.Content);

                Assert.AreEqual("site one", s1["GREETING"]);
                Assert.AreEqual("website", s2["GREETING"]);
                Assert.AreEqual("g", s1["ONLY-GLOBAL"]);
            }
        }

        // ---------------------------------------------------------------- CDN / FTP / publications

        [DataTestMethod]
        [DynamicData(nameof(Providers))]
        public async Task Cdn_SaveCreate_ThenUpdate_SyncsTheFtpServers(string provider)
        {
            using var db = ProviderFixture.Create(provider);

            int ftp1;
            int ftp2;
            using (var scope = db.NewScope())
            {
                ftp1 = (await scope.Repo<FtpServer>().Create(new FtpServer { NAME = "f1", HOST = "h", USERNAME = "u", PASSWORD = "p", TYPE = 1 })).ID;
                ftp2 = (await scope.Repo<FtpServer>().Create(new FtpServer { NAME = "f2", HOST = "h", USERNAME = "u", PASSWORD = "p", TYPE = 1 })).ID;
            }

            CdnServerDTO created;
            using (var scope = db.NewScope())
            {
                var logger = new CollectingLogger<CDNSettingService>();
                var service = new CDNSettingService(scope.Repo<CdnServer>(), scope.Repo<FtpServerPerCdnServer>(), Mapper, logger);
                var rs = await service.Save(new CdnServerDTO { Name = "cdn", BaseUrl = "https://cdn", Type = EnvironmentType.Production }, [ftp1]);
                Assert.IsTrue(rs.Success, logger.ToString());
                created = rs.Value;
                Assert.IsTrue(created.Id > 0);
            }

            using (var scope = db.NewScope())
            {
                var logger = new CollectingLogger<CDNSettingService>();
                var service = new CDNSettingService(scope.Repo<CdnServer>(), scope.Repo<FtpServerPerCdnServer>(), Mapper, logger);
                var rs = await service.Save(new CdnServerDTO { Id = created.Id, Name = "cdn2", BaseUrl = "https://cdn2", Type = EnvironmentType.Production }, [ftp2]);
                Assert.IsTrue(rs.Success, logger.ToString());
                Assert.AreEqual("cdn2", rs.Value.Name);
            }

            using (var scope = db.NewScope())
            {
                var joins = await scope.Repo<FtpServerPerCdnServer>().ToListAsync(
                    scope.Repo<FtpServerPerCdnServer>().Repository.Where(x => x.FK_CDNSERVER == created.Id));
                CollectionAssert.AreEqual(new[] { ftp2 }, joins.Select(j => j.FK_FTPSERVER).ToArray());
                Assert.AreEqual("https://cdn2", (await scope.Repo<CdnServer>().Read(created.Id.Value)).BASEURL);
            }
        }

        [DataTestMethod]
        [DynamicData(nameof(Providers))]
        public async Task Publication_Create_Get_UpdateStatus_GetAll_Remove(string provider)
        {
            using var db = ProviderFixture.Create(provider);

            int ftpId;
            using (var scope = db.NewScope())
            {
                ftpId = (await scope.Repo<FtpServer>().Create(new FtpServer { NAME = "ftp-name", HOST = "h", USERNAME = "u", PASSWORD = "p", TYPE = 1 })).ID;
            }

            PublicationService Service(ProviderScope scope)
                => new(scope.Repo<Publication>(), scope.Repo<FtpServer>(), Mapper, NullLogger<PublicationService>.Instance);

            var dto = new PublicationDTO
            {
                Website = "site",
                Email = "me@example.com",
                CreationUser = "me",
                FtpServer = new FTPServerDTO { Id = ftpId, Name = "ftp-name" },
                DataType = PublicationType.All,
                Format = PublicationFormat.XML,
                FilterDataDate = DateTime.Now,
                StatusMessage = "created",
            };
            dto.SetPayload(new PayloadDTO { Website = "site", Tenant = "tenant" });

            int id;
            using (var scope = db.NewScope())
            {
                var rs = await Service(scope).Create(dto);
                Assert.IsTrue(rs.Success, string.Join(",", rs.Errors?.Select(e => e.Description) ?? []));
                Assert.IsNotNull(rs.Value.FtpServer, "the created publication keeps its FTP server");
                id = rs.Value.Id.Value;
            }

            using (var scope = db.NewScope())
            {
                var rs = await Service(scope).Get(id, false);
                Assert.IsTrue(rs.Success, string.Join(",", rs.Errors?.Select(e => e.Description) ?? []));
                Assert.AreEqual("me@example.com", rs.Value.Email);
                Assert.AreEqual("tenant", rs.Value.Payload.Tenant);
                Assert.AreEqual(ftpId, rs.Value.FtpServer.Id);
            }

            using (var scope = db.NewScope())
            {
                Assert.IsTrue((await Service(scope).Update(id, PublicationStatus.Failed, "boom", null)).Success);
            }

            using (var scope = db.NewScope())
            {
                var listed = await Service(scope).GetAll();
                Assert.IsTrue(listed.Success);
                var item = listed.Value.Single();
                Assert.AreEqual(PublicationStatus.Failed, item.StatusCode);
                Assert.AreEqual("boom", item.StatusMessage);
                Assert.AreEqual("ftp-name", item.FtpServer.Name);

                Assert.AreEqual(0, (await Service(scope).GetAll(["another-site"])).Value.Count);
            }

            using (var scope = db.NewScope())
            {
                Assert.IsTrue((await Service(scope).Remove(id)).Success);
            }

            using (var scope = db.NewScope())
            {
                Assert.AreEqual(0, (await Service(scope).GetAll()).Value.Count);
            }
        }

        // ---------------------------------------------------------------- folders

        private sealed class NoImages : IImageDrawingService
        {
            public Task<OperationResult<byte[]>> GenerateThumb(FileDTO dto)
                => Task.FromResult(OperationResult<byte[]>.MakeFailure([ErrorMessage.Create("GENERATE_THUMB", "NOT_OPTIMIZED")]));

            public Task<OperationResult<byte[]>> Compression(FileDTO dto)
                => Task.FromResult(OperationResult<byte[]>.MakeFailure([ErrorMessage.Create("COMPRESSION", "NOT_OPTIMIZED")]));
        }

        private static FileManagerService Files(ProviderScope scope)
            => new(scope.Repo<Folder>(), scope.Repo<TrinityText.Domain.File>(), new NoImages(), Mapper, NullLogger<FileManagerService>.Instance);

        [DataTestMethod]
        [DynamicData(nameof(Providers))]
        public async Task Folders_DefaultTree_RemoveDeletesFilesAndSubfolders_ButNotSystemFolders(string provider)
        {
            using var db = ProviderFixture.Create(provider);

            int rootId;
            int filesId;
            using (var scope = db.NewScope())
            {
                Assert.IsTrue((await Files(scope).CreateDefaultWebsiteFolders("W")).Success);
                var root = await scope.Repo<Folder>().FirstOrDefaultAsync(scope.Repo<Folder>().Repository.Where(f => f.FK_WEBSITE == "W" && f.FK_PARENT == null));
                rootId = root.ID;
                filesId = (await scope.Repo<Folder>().FirstOrDefaultAsync(scope.Repo<Folder>().Repository.Where(f => f.FK_PARENT == rootId && f.NAME == "Files"))).ID;
            }

            int docsId;
            using (var scope = db.NewScope())
            {
                var docs = await Files(scope).SaveFolder(filesId, new FolderDTO { Name = "docs", Website = "W" });
                Assert.IsTrue(docs.Success, string.Join(",", docs.Errors?.Select(e => e.Description) ?? []));
                docsId = docs.Value.Id.Value;

                var nested = await Files(scope).SaveFolder(docsId, new FolderDTO { Name = "2026", Website = "W" });
                Assert.IsTrue(nested.Success);

                Assert.IsTrue((await Files(scope).AddFile("me", "W", docsId, new FileDTO { Filename = "a.txt", Content = [1, 2, 3] }, false, true)).Success);
                Assert.IsTrue((await Files(scope).AddFile("me", "W", nested.Value.Id.Value, new FileDTO { Filename = "b.txt", Content = [4, 5] }, false, true)).Success);

                // same name under the same parent is refused
                Assert.IsFalse((await Files(scope).SaveFolder(filesId, new FolderDTO { Name = "docs", Website = "W" })).Success);
                // a folder cannot become its own descendant
                Assert.IsFalse((await Files(scope).SaveFolder(nested.Value.Id, new FolderDTO { Id = docsId, Name = "docs", Website = "W" })).Success);
            }

            using (var scope = db.NewScope())
            {
                // system folders cannot be removed
                Assert.IsFalse((await Files(scope).RemoveFolder(filesId)).Success);
                Assert.IsTrue((await Files(scope).RemoveFolder(docsId)).Success);
            }

            using (var scope = db.NewScope())
            {
                Assert.AreEqual(0, await scope.Repo<TrinityText.Domain.File>().CountAsync(scope.Repo<TrinityText.Domain.File>().Repository));
                Assert.AreEqual(0, await scope.Repo<Folder>().CountAsync(scope.Repo<Folder>().Repository.Where(f => f.NAME == "docs" || f.NAME == "2026")));
                Assert.IsNotNull(await scope.Repo<Folder>().Read(filesId));
            }
        }

        [DataTestMethod]
        [DynamicData(nameof(Providers))]
        public async Task Files_RenameMoveLinkAndContent_WorkWithoutLoadingTheBlobs(string provider)
        {
            using var db = ProviderFixture.Create(provider);

            int rootId;
            using (var scope = db.NewScope())
            {
                Assert.IsTrue((await Files(scope).CreateDefaultWebsiteFolders("W")).Success);
                rootId = (await scope.Repo<Folder>().FirstOrDefaultAsync(scope.Repo<Folder>().Repository.Where(f => f.FK_WEBSITE == "W" && f.FK_PARENT == null))).ID;
            }

            int folderA;
            int folderB;
            Guid fileId;
            using (var scope = db.NewScope())
            {
                folderA = (await Files(scope).SaveFolder(rootId, new FolderDTO { Name = "a b", Website = "W" })).Value.Id.Value;
                folderB = (await Files(scope).SaveFolder(rootId, new FolderDTO { Name = "b", Website = "W" })).Value.Id.Value;
                Assert.IsTrue((await Files(scope).AddFile("me", "W", folderA, new FileDTO { Filename = "photo one.png", Content = [9, 8, 7] }, false, true)).Success);
                var listed = await Files(scope).GetFilesByFolder("W", folderA, false, null);
                fileId = listed.Value.Single().Id;
            }

            using (var scope = db.NewScope())
            {
                var renamed = await Files(scope).RenameFile(fileId, "photo two.png", "you");
                Assert.IsTrue(renamed.Success);
                Assert.AreEqual("photo two.png", renamed.Value.Filename);
                Assert.IsNull(renamed.Value.Content, "renaming does not load the content");
            }

            using (var scope = db.NewScope())
            {
                var moved = await Files(scope).MoveFile("you", folderB, fileId);
                Assert.IsTrue(moved.Success);
            }

            using (var scope = db.NewScope())
            {
                var content = await Files(scope).GetFileContent(fileId);
                CollectionAssert.AreEqual(new byte[] { 9, 8, 7 }, content.Value, "the content survives rename and move");

                var link = (await Files(scope).GetFileLink(fileId)).Value;
                Assert.AreEqual($"@/W/b/{Uri.EscapeDataString("photo two.png")}", link);

                var byName = await Files(scope).GetFileIdByFullname(link);
                Assert.IsTrue(byName.Success);
                Assert.AreEqual(fileId, byName.Value);

                Assert.IsTrue((await Files(scope).DeleteFile(fileId)).Success);
                Assert.IsFalse((await Files(scope).DeleteFile(fileId)).Success);
            }
        }

        [DataTestMethod]
        [DynamicData(nameof(Providers))]
        public async Task Files_ServerSideAndExecutableFiles_AreRefused(string provider)
        {
            using var db = ProviderFixture.Create(provider);

            int folder;
            using (var scope = db.NewScope())
            {
                Assert.IsTrue((await Files(scope).CreateDefaultWebsiteFolders("W")).Success);
                var root = await scope.Repo<Folder>().FirstOrDefaultAsync(scope.Repo<Folder>().Repository.Where(f => f.FK_WEBSITE == "W" && f.FK_PARENT == null));
                folder = (await Files(scope).SaveFolder(root.ID, new FolderDTO { Name = "uploads", Website = "W" })).Value.Id.Value;
            }

            using (var scope = db.NewScope())
            {
                var rejected = await Files(scope).AddFile("me", "W", folder, new FileDTO { Filename = "shell.aspx", Content = [1] }, false, true);
                Assert.IsFalse(rejected.Success);
                Assert.AreEqual("FILE_NOT_ALLOWED", rejected.Errors.First().Description);

                Assert.IsTrue((await Files(scope).AddFile("me", "W", folder, new FileDTO { Filename = "photo.png", Content = [1] }, false, true)).Success);
                var id = (await Files(scope).GetFilesByFolder("W", folder, false, null)).Value.Single().Id;

                var renamed = await Files(scope).RenameFile(id, "photo.exe", "me");
                Assert.IsFalse(renamed.Success);
            }
        }

        private sealed class FixedImages : IImageDrawingService
        {
            public Task<OperationResult<byte[]>> GenerateThumb(FileDTO dto)
                => Task.FromResult(OperationResult<byte[]>.MakeSuccess([7, 7]));

            public Task<OperationResult<byte[]>> Compression(FileDTO dto)
                => Task.FromResult(OperationResult<byte[]>.MakeFailure([ErrorMessage.Create("COMPRESSION", "NOT_OPTIMIZED")]));
        }

        [DataTestMethod]
        [DynamicData(nameof(Providers))]
        public async Task Files_GetFile_ReadsOnlyTheRequestedBlob_AndOverrideReplacesInPlace(string provider)
        {
            using var db = ProviderFixture.Create(provider);

            FileManagerService Service(ProviderScope scope)
                => new(scope.Repo<Folder>(), scope.Repo<TrinityText.Domain.File>(), new FixedImages(), Mapper, NullLogger<FileManagerService>.Instance);

            int folder;
            using (var scope = db.NewScope())
            {
                Assert.IsTrue((await Service(scope).CreateDefaultWebsiteFolders("W")).Success);
                var root = await scope.Repo<Folder>().FirstOrDefaultAsync(scope.Repo<Folder>().Repository.Where(f => f.FK_WEBSITE == "W" && f.FK_PARENT == null));
                folder = (await Service(scope).SaveFolder(root.ID, new FolderDTO { Name = "img", Website = "W" })).Value.Id.Value;
                Assert.IsTrue((await Service(scope).AddFile("me", "W", folder, new FileDTO { Filename = "a.png", Content = [1, 2, 3] }, false, true)).Success);
            }

            Guid id;
            using (var scope = db.NewScope())
            {
                id = (await Service(scope).GetFilesByFolder("W", folder, false, null)).Value.Single().Id;

                var full = (await Service(scope).GetFile(id, false)).Value;
                CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, full.Content);
                Assert.IsTrue(full.HasThumbnail);

                var thumb = (await Service(scope).GetFile(id, true)).Value;
                CollectionAssert.AreEqual(new byte[] { 7, 7 }, thumb.Content);

                Assert.IsFalse((await Service(scope).GetFile(Guid.NewGuid(), false)).Success);
            }

            using (var scope = db.NewScope())
            {
                // same name + override: the row is updated in place
                Assert.IsTrue((await Service(scope).AddFile("you", "W", folder, new FileDTO { Filename = "a.png", Content = [9, 9, 9, 9] }, true, true)).Success);
            }

            using (var scope = db.NewScope())
            {
                var files = (await Service(scope).GetFilesByFolder("W", folder, false, null)).Value;
                Assert.AreEqual(1, files.Count);
                Assert.AreEqual(id, files.Single().Id, "the file keeps its id");
                Assert.AreEqual("you", files.Single().LastUpdateUser);
                CollectionAssert.AreEqual(new byte[] { 9, 9, 9, 9 }, (await Service(scope).GetFileContent(id)).Value);
            }
        }

        [DataTestMethod]
        [DynamicData(nameof(Providers))]
        public async Task Text_Revisions_AreListedInOrder_AndTheImportSkipsUnchangedRows(string provider)
        {
            using var db = ProviderFixture.Create(provider);

            int id;
            using (var scope = db.NewScope())
            {
                id = (await Texts(scope).Save(NewText("key", "v1"))).Value.Id.Value;
            }

            using (var scope = db.NewScope())
            {
                var dto = (await Texts(scope).Get(id)).Value;
                dto.TextRevision.Content = "v2";
                Assert.IsTrue((await Texts(scope).Save(dto)).Success);
            }

            using (var scope = db.NewScope())
            {
                var revisions = (await Texts(scope).GetAllRevisions(id)).Value;
                CollectionAssert.AreEqual(new[] { "v1", "v2" }, revisions.Select(r => r.Content).ToArray());
                Assert.IsFalse((await Texts(scope).GetAllRevisions(id + 1000)).Success);
            }

            // re-importing the same values with override changes nothing, a different value adds one revision
            using (var scope = db.NewScope())
            {
                var same = await Texts(scope).ImportTexts(null, [NewText("key", "v2")], true);
                Assert.AreEqual(1, same.Value, "processed rows are still counted");
            }

            using (var scope = db.NewScope())
            {
                Assert.AreEqual(2, (await Texts(scope).GetAllRevisions(id)).Value.Count);
                Assert.IsTrue((await Texts(scope).ImportTexts(null, [NewText("key", "v3")], true)).Success);
            }

            using (var scope = db.NewScope())
            {
                Assert.AreEqual(3, (await Texts(scope).GetAllRevisions(id)).Value.Count);
            }
        }

        [DataTestMethod]
        [DynamicData(nameof(Providers))]
        public async Task Texts_MoreThanOneInListChunk_AreAllLoaded(string provider)
        {
            using var db = ProviderFixture.Create(provider);

            var many = Enumerable.Range(0, 2500).Select(n => NewText("key" + n, "content " + n)).ToList();
            using (var scope = db.NewScope())
            {
                var rs = await Texts(scope).ImportTexts(null, many, false);
                Assert.IsTrue(rs.Success);
                Assert.AreEqual(2500, rs.Value);
            }

            using (var scope = db.NewScope())
            {
                // existing texts found through the chunked lookup: nothing new is created
                var again = await Texts(scope).ImportTexts(null, many, false);
                Assert.AreEqual(0, again.Value);
            }

            using (var scope = db.NewScope())
            {
                var rs = await Texts(scope).GetPublishableTextsByWebsite("W", new Dictionary<string, string[]> { ["S1"] = ["it"] }, []);
                Assert.IsTrue(rs.Success);
                var published = rs.Value["S1"];
                Assert.AreEqual(2500, published.Count);
                Assert.IsTrue(published.All(t => !string.IsNullOrEmpty(t.TextRevision?.Content)), "every text has its latest revision");
            }
        }
    }
}
