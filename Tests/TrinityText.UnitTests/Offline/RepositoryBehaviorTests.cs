using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TrinityText.Domain;

namespace TrinityText.UnitTests.Offline
{
    /// <summary>
    /// The same repository behavior on EF Core and NHibernate (SQLite in memory): Create / Read / Update / Delete
    /// without an explicit transaction, cascades, nullable foreign keys, set-based update / delete, transactions.
    /// </summary>
    [TestClass]
    [TestCategory("Offline")]
    public class RepositoryBehaviorTests
    {
        public static IEnumerable<object[]> Providers => new[] { new object[] { "EF" }, new object[] { "NH" } };

        [DataTestMethod]
        [DynamicData(nameof(Providers))]
        public async Task Create_ReturnsTheEntity_AndItCanBeReadInANewScope(string provider)
        {
            using var db = ProviderFixture.Create(provider);

            int id;
            using (var scope = db.NewScope())
            {
                var created = await scope.Repo<FtpServer>().Create(new FtpServer { NAME = "ftp", HOST = "h", USERNAME = "u", PASSWORD = "p", TYPE = 1, PORT = 21 });
                Assert.IsTrue(created.ID > 0);
                id = created.ID;
            }

            using (var scope = db.NewScope())
            {
                var read = await scope.Repo<FtpServer>().Read(id);
                Assert.IsNotNull(read);
                Assert.AreEqual("ftp", read.NAME);
                Assert.AreEqual("p", read.PASSWORD, "PASSWORD must be mapped by both providers");
                Assert.AreEqual(21, read.PORT);
            }
        }

        [DataTestMethod]
        [DynamicData(nameof(Providers))]
        public async Task Update_WithoutTransaction_IsPersisted(string provider)
        {
            using var db = ProviderFixture.Create(provider);

            int id;
            using (var scope = db.NewScope())
            {
                id = (await scope.Repo<FtpServer>().Create(new FtpServer { NAME = "old", HOST = "h", USERNAME = "u", PASSWORD = "p", TYPE = 1 })).ID;
            }

            using (var scope = db.NewScope())
            {
                var repository = scope.Repo<FtpServer>();
                var entity = await repository.Read(id);
                entity.NAME = "new";
                await repository.Update(entity);
            }

            using (var scope = db.NewScope())
            {
                Assert.AreEqual("new", (await scope.Repo<FtpServer>().Read(id)).NAME);
            }
        }

        [DataTestMethod]
        [DynamicData(nameof(Providers))]
        public async Task Delete_WithoutTransaction_IsPersisted(string provider)
        {
            using var db = ProviderFixture.Create(provider);

            int id;
            using (var scope = db.NewScope())
            {
                id = (await scope.Repo<FtpServer>().Create(new FtpServer { NAME = "x", HOST = "h", USERNAME = "u", PASSWORD = "p", TYPE = 1 })).ID;
            }

            using (var scope = db.NewScope())
            {
                var repository = scope.Repo<FtpServer>();
                await repository.Delete(await repository.Read(id));
            }

            using (var scope = db.NewScope())
            {
                Assert.IsNull(await scope.Repo<FtpServer>().Read(id));
            }
        }

        [DataTestMethod]
        [DynamicData(nameof(Providers))]
        public async Task Text_WithRevision_IsSavedTogether_AndTheTypeIsOptional(string provider)
        {
            using var db = ProviderFixture.Create(provider);

            int id;
            using (var scope = db.NewScope())
            {
                var text = new Text
                {
                    NAME = "KEY",
                    FK_LANGUAGE = "it",
                    ACTIVE = true,
                };
                // owned children reference their parent (NHibernate does not infer it from the collection)
                text.REVISIONS = [new TextRevision { CONTENT = "hello", REVISION_NUMBER = 1, CREATION_DATE = DateTime.Now, CREATION_USER = "me", TEXT = text }];
                id = (await scope.Repo<Text>().Create(text)).ID;
            }

            using (var scope = db.NewScope())
            {
                var text = await scope.Repo<Text>().Read(id);
                Assert.IsNotNull(text);
                Assert.IsNull(text.FK_TEXTTYPE, "a text without type must be storable");

                var revisions = await scope.Repo<TextRevision>().ToListAsync(scope.Repo<TextRevision>().Repository.Where(r => r.FK_TEXT == id));
                Assert.AreEqual(1, revisions.Count, "the revision must be inserted with the text");
                Assert.AreEqual("hello", revisions[0].CONTENT);
            }
        }

        [DataTestMethod]
        [DynamicData(nameof(Providers))]
        public async Task Publication_KeepsPayloadEmailAndForeignKeys(string provider)
        {
            using var db = ProviderFixture.Create(provider);

            int ftpId;
            int cdnId;
            int publicationId;
            using (var scope = db.NewScope())
            {
                ftpId = (await scope.Repo<FtpServer>().Create(new FtpServer { NAME = "ftp", HOST = "h", USERNAME = "u", PASSWORD = "p", TYPE = 1 })).ID;
                cdnId = (await scope.Repo<CdnServer>().Create(new CdnServer { NAME = "cdn", BASEURL = "https://cdn", TYPE = 0 })).ID;
                publicationId = (await scope.Repo<Publication>().Create(new Publication
                {
                    FK_WEBSITE = "site",
                    EMAIL = "me@example.com",
                    PAYLOAD = "{\"Website\":\"site\"}",
                    FK_FTPSERVER = ftpId,
                    FK_CDNSERVER = cdnId,
                    CREATION_USER = "me",
                    STATUS_MESSAGE = "created",
                    LASTUPDATE_DATE = DateTime.Now,
                    FILTERDATA_DATE = DateTime.Now,
                })).ID;
            }

            using (var scope = db.NewScope())
            {
                var publication = await scope.Repo<Publication>().Read(publicationId);
                Assert.AreEqual("me@example.com", publication.EMAIL);
                Assert.AreEqual("{\"Website\":\"site\"}", publication.PAYLOAD);
                Assert.AreEqual(ftpId, publication.FK_FTPSERVER);
                Assert.AreEqual(cdnId, publication.FK_CDNSERVER);
            }
        }

        [DataTestMethod]
        [DynamicData(nameof(Providers))]
        public async Task ExecuteUpdate_ChangesOnlyTheGivenColumns(string provider)
        {
            using var db = ProviderFixture.Create(provider);

            int id;
            using (var scope = db.NewScope())
            {
                id = (await scope.Repo<FtpServer>().Create(new FtpServer { NAME = "old", HOST = "host", USERNAME = "u", PASSWORD = "secret", TYPE = 1 })).ID;
            }

            using (var scope = db.NewScope())
            {
                var repository = scope.Repo<FtpServer>();
                var affected = await repository.ExecuteUpdateAsync(repository.Repository.Where(f => f.ID == id), set => set.Set(f => f.NAME, "renamed"));
                Assert.AreEqual(1, affected);
            }

            using (var scope = db.NewScope())
            {
                var read = await scope.Repo<FtpServer>().Read(id);
                Assert.AreEqual("renamed", read.NAME);
                Assert.AreEqual("secret", read.PASSWORD);
                Assert.AreEqual("host", read.HOST);
            }
        }

        [DataTestMethod]
        [DynamicData(nameof(Providers))]
        public async Task CleanRevisionsPredicate_KeepsTheLatestOnes(string provider)
        {
            using var db = ProviderFixture.Create(provider);

            int textId;
            using (var scope = db.NewScope())
            {
                var text = new Text
                {
                    NAME = "KEY",
                    FK_LANGUAGE = "it",
                    ACTIVE = true,
                };
                text.REVISIONS = Enumerable.Range(1, 5)
                    .Select(n => new TextRevision { CONTENT = "v" + n, REVISION_NUMBER = n, CREATION_DATE = DateTime.Now, CREATION_USER = "me", TEXT = text })
                    .ToList();
                textId = (await scope.Repo<Text>().Create(text)).ID;
            }

            using (var scope = db.NewScope())
            {
                var repository = scope.Repo<TextRevision>();
                const int keep = 2;
                await repository.ExecuteDeleteAsync(
                    repository.Repository.Where(r => repository.Repository.Count(x => x.FK_TEXT == r.FK_TEXT && x.REVISION_NUMBER > r.REVISION_NUMBER) >= keep));
            }

            using (var scope = db.NewScope())
            {
                var repository = scope.Repo<TextRevision>();
                var left = await repository.ToListAsync(repository.Repository.Where(r => r.FK_TEXT == textId).OrderBy(r => r.REVISION_NUMBER));
                CollectionAssert.AreEqual(new[] { 4, 5 }, left.Select(r => r.REVISION_NUMBER).ToArray());
            }
        }

        [DataTestMethod]
        [DynamicData(nameof(Providers))]
        public async Task Transaction_Rollback_DiscardsTheChanges_AndNestingJoinsTheOuterOne(string provider)
        {
            using var db = ProviderFixture.Create(provider);

            using (var scope = db.NewScope())
            {
                var repository = scope.Repo<FtpServer>();
                await repository.BeginTransaction();
                await repository.BeginTransaction(); // nested
                await repository.Create(new FtpServer { NAME = "rolled-back", HOST = "h", USERNAME = "u", PASSWORD = "p", TYPE = 1 });
                await repository.CommitTransaction(); // inner commit must not commit
                await repository.RollbackTransaction();
            }

            using (var scope = db.NewScope())
            {
                var repository = scope.Repo<FtpServer>();
                Assert.AreEqual(0, await repository.CountAsync(repository.Repository.Where(f => f.NAME == "rolled-back")));
            }

            using (var scope = db.NewScope())
            {
                var repository = scope.Repo<FtpServer>();
                await repository.BeginTransaction();
                await repository.BeginTransaction();
                await repository.Create(new FtpServer { NAME = "committed", HOST = "h", USERNAME = "u", PASSWORD = "p", TYPE = 1 });
                await repository.CommitTransaction();
                await repository.CommitTransaction();
            }

            using (var scope = db.NewScope())
            {
                var repository = scope.Repo<FtpServer>();
                Assert.AreEqual(1, await repository.CountAsync(repository.Repository.Where(f => f.NAME == "committed")));
            }
        }
    }
}
