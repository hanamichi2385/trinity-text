using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using TrinityText.Business;
using TrinityText.Domain;

namespace TrinityText.UnitTests.Offline
{
    [TestClass]
    [TestCategory("Offline")]
    public class BusinessMapperTests
    {
        [TestMethod]
        public void NullSource_ReturnsNull()
        {
            Assert.IsNull(BusinessMapper.ToDto((Text)null));
            Assert.IsNull(BusinessMapper.ToDto((PageType)null));
            Assert.IsNull(BusinessMapper.ToDto((File)null));
            Assert.IsNull(BusinessMapper.ToDto((CdnServer)null));
            Assert.IsNull(BusinessMapper.ToEntity((TextDTO)null));
            Assert.IsNull(BusinessMapper.ToEntity((PageTypeDTO)null));
        }

        [TestMethod]
        public void Text_ToDto_UsesLatestRevisionAndUpperCasesTheName()
        {
            var text = new Text
            {
                ID = 1, NAME = "info", FK_WEBSITE = "w", FK_PRICELIST = "s", FK_LANGUAGE = "it", FK_COUNTRY = "IT", FK_TEXTTYPE = 4, ACTIVE = true,
                TEXTTYPE = new TextType { ID = 4, CONTENTTYPE = "type" },
                REVISIONS = new List<TextRevision>
                {
                    new() { ID = 10, REVISION_NUMBER = 1, CONTENT = "old", CREATION_USER = "u1", CREATION_DATE = new DateTime(2020, 1, 1) },
                    new() { ID = 11, REVISION_NUMBER = 2, CONTENT = "new", CREATION_USER = "u2", CREATION_DATE = new DateTime(2021, 1, 1) },
                },
            };

            var dto = BusinessMapper.ToDto(text);

            Assert.AreEqual("INFO", dto.Name);
            Assert.AreEqual("w", dto.Website);
            Assert.AreEqual("s", dto.Site);
            Assert.AreEqual("it", dto.Language);
            Assert.AreEqual("IT", dto.Country);
            Assert.AreEqual(4, dto.TextTypeId);
            Assert.IsTrue(dto.Active);
            Assert.AreEqual("type", dto.TextType.Name);
            Assert.AreEqual(2, dto.TextRevision.Index);
            Assert.AreEqual("new", dto.TextRevision.Content);
            Assert.AreEqual("u2", dto.TextRevision.CreationUser);
            Assert.AreEqual(new DateTime(2021, 1, 1), dto.TextRevision.CreationDate);
        }

        [TestMethod]
        public void Text_ToDto_WithoutRevisions_HasNoRevision()
        {
            Assert.IsNull(BusinessMapper.ToDto(new Text { NAME = "a" }).TextRevision);
            Assert.IsNull(BusinessMapper.ToDto(new Text { NAME = "a", REVISIONS = new List<TextRevision>() }).TextRevision);
        }

        [TestMethod]
        public void Revision_EmptyContentIsMappedToEmptyString()
        {
            Assert.AreEqual(string.Empty, BusinessMapper.ToDto(new TextRevision { CONTENT = "  " }).Content);
            Assert.AreEqual(string.Empty, BusinessMapper.ToEntity(new TextRevisionDTO { Content = null }).CONTENT);
        }

        [TestMethod]
        public void Revision_DtoToEntity_MapsIndexAndAudit()
        {
            var entity = BusinessMapper.ToEntity(new TextRevisionDTO { Id = 5, Index = 3, Content = "c", CreationUser = "u", CreationDate = new DateTime(2022, 2, 2) });

            Assert.AreEqual(5, entity.ID);
            Assert.AreEqual(3, entity.REVISION_NUMBER);
            Assert.AreEqual("u", entity.CREATION_USER);
            Assert.AreEqual(new DateTime(2022, 2, 2), entity.CREATION_DATE);
        }

        [TestMethod]
        public void PageType_VisibilityIsSplitAndJoined()
        {
            var dto = BusinessMapper.ToDto(new PageType { ID = 2, NAME = "n", VISIBILITY = "a|b||c", FK_WEBSITE = "w", OUTPUT_FILENAME = "o", PATH_PREVIEWPAGE = "p", PRINT_ELEMENT_NAME = "e",
                PAGES = new List<Page> { new(), new() } });

            CollectionAssert.AreEqual(new[] { "a", "b", "c" }, dto.Visibility.ToArray());
            Assert.AreEqual(2, dto.PageTotals);
            Assert.AreEqual("w", dto.Website);
            Assert.AreEqual("o", dto.OutputFilename);
            Assert.IsNull(dto.PathPreviewPage); // not mapped, as with AutoMapper
            Assert.AreEqual("e", dto.PrintElementName);

            Assert.AreEqual(0, BusinessMapper.ToDto(new PageType { VISIBILITY = " " }).Visibility.Count);
            Assert.AreEqual(0, BusinessMapper.ToDto(new PageType()).PageTotals);

            var entity = BusinessMapper.ToEntity(dto);
            Assert.AreEqual("a|b|c", entity.VISIBILITY);
            Assert.IsNull(entity.PATH_PREVIEWPAGE);
            Assert.AreEqual(string.Empty, BusinessMapper.ToEntity(new PageTypeDTO()).VISIBILITY);
        }

        [TestMethod]
        public void Page_RoundTrip()
        {
            var page = new Page
            {
                ID = 3, TITLE = "t", CONTENT = "<x/>", FK_WEBSITE = "w", FK_PRICELIST = "s", FK_LANGUAGE = "it", FK_PAGETYPE = 7, ACTIVE = true, GENERATE_PDF = true,
                CREATION_USER = "c", LASTUPDATE_USER = "l", CREATION_DATE = new DateTime(2020, 1, 1), LASTUPDATE_DATE = new DateTime(2021, 1, 1),
                PAGETYPE = new PageType { ID = 7, NAME = "pt", VISIBILITY = "x" },
            };

            var dto = BusinessMapper.ToDto(page);
            var back = BusinessMapper.ToEntity(dto);

            Assert.AreEqual(7, dto.PageTypeId);
            Assert.AreEqual("pt", dto.PageType.Name);
            Assert.IsFalse(dto.GeneratePdf); // not mapped, as with AutoMapper
            Assert.AreEqual(new DateTime(2021, 1, 1), dto.LastUpdate);
            Assert.AreEqual("s", back.FK_PRICELIST);
            Assert.AreEqual("w", back.FK_WEBSITE);
            Assert.AreEqual("t", back.TITLE);
            Assert.AreEqual("<x/>", back.CONTENT);
            Assert.IsTrue(back.ACTIVE);
            Assert.IsFalse(back.GENERATE_PDF);
            Assert.AreEqual("l", back.LASTUPDATE_USER);
        }

        [TestMethod]
        public void Widget_RoundTrip()
        {
            var dto = BusinessMapper.ToDto(new Widget { ID = 1, KEY = "k", CONTENT = "c", FK_WEBSITE = "w", FK_PRICELIST = "s", FK_LANGUAGE = "it", CREATION_USER = "u" });
            var back = BusinessMapper.ToEntity(dto);

            Assert.AreEqual("k", dto.Key);
            Assert.AreEqual("it", dto.Language);
            Assert.AreEqual("k", back.KEY);
            Assert.AreEqual("s", back.FK_PRICELIST);
            Assert.AreEqual("u", back.CREATION_USER);
        }

        [TestMethod]
        public void File_HasThumbnailFollowsTheBlob_AndReverseDoesNotTouchThumbnail()
        {
            var id = Guid.NewGuid();
            Assert.IsTrue(BusinessMapper.ToDto(new File { ID = id, THUMBNAIL = new byte[] { 1 } }).HasThumbnail);
            Assert.IsFalse(BusinessMapper.ToDto(new File { ID = id }).HasThumbnail);

            var dto = BusinessMapper.ToDto(new File { ID = id, FILENAME = "f.png", CONTENT = new byte[] { 1, 2 }, FK_FOLDER = 9, FK_WEBSITE = "w" });
            Assert.AreEqual("f.png", dto.Filename);
            CollectionAssert.AreEqual(new byte[] { 1, 2 }, dto.Content);

            var entity = BusinessMapper.ToEntity(dto);
            Assert.AreEqual(id, entity.ID);
            Assert.IsNull(entity.THUMBNAIL);
        }

        [TestMethod]
        public void Folder_MapsParentAndWebsite()
        {
            var dto = BusinessMapper.ToDto(new Folder { ID = 2, NAME = "n", FK_PARENT = 1, FK_WEBSITE = "w", DELETABLE = true, NOTE = "x" });
            var back = BusinessMapper.ToEntity(dto);

            Assert.AreEqual(1, dto.ParentId);
            Assert.IsTrue(dto.Deletable);
            Assert.AreEqual(0, dto.SubFolders.Count);
            Assert.AreEqual(1, back.FK_PARENT);
            Assert.AreEqual("w", back.FK_WEBSITE);
            Assert.IsTrue(back.DELETABLE);
        }

        [TestMethod]
        public void Cdn_MapsFtpServersAndEnumTypes()
        {
            var cdn = new CdnServer
            {
                ID = 1, NAME = "c", BASEURL = "https://cdn", TYPE = 2,
                FTPSERVERS = new List<FtpServerPerCdnServer> { new() { FTPSERVER = new FtpServer { ID = 5, NAME = "f", HOST = "h", PORT = 21, TYPE = 1 } } },
            };

            var dto = BusinessMapper.ToDto(cdn);

            Assert.AreEqual(EnvironmentType.Production, dto.Type);
            Assert.AreEqual("https://cdn", dto.BaseUrl);
            Assert.AreEqual(1, dto.FtpServers.Count);
            Assert.AreEqual(EnvironmentType.Stage, dto.FtpServers[0].Type);
            Assert.AreEqual(21, dto.FtpServers[0].Port);
            Assert.AreEqual(0, BusinessMapper.ToDto(new CdnServer()).FtpServers.Count);

            var back = BusinessMapper.ToEntity(dto);
            Assert.AreEqual(2, back.TYPE);
            Assert.AreEqual(0, back.FTPSERVERS.Count);
        }

        [TestMethod]
        public void Publication_MapsServersAndStatus()
        {
            var dto = BusinessMapper.ToDto(new Publication
            {
                ID = 8, CREATION_USER = "u", EMAIL = "e@x", FK_WEBSITE = "w", DATATYPE = (int)PublicationType.Pages, STATUS_CODE = (int)PublicationStatus.Success,
                FORMAT = (int)PublicationFormat.XML, MANUALDELETE = true, PAYLOAD = "{}", FTPSERVER = new FtpServer { ID = 3 },
            });

            Assert.AreEqual(8, dto.Id);
            Assert.AreEqual("u", dto.CreationUser);
            Assert.AreEqual(PublicationType.Pages, dto.DataType);
            Assert.AreEqual(PublicationStatus.Success, dto.StatusCode);
            Assert.AreEqual(PublicationFormat.XML, dto.Format);
            Assert.IsTrue(dto.ManualDelete);
            Assert.AreEqual(3, dto.FtpServer.Id);
            Assert.IsNull(dto.CdnServer);
        }

        [TestMethod]
        public void CacheSettings_AndWebsiteConfiguration_RoundTrip()
        {
            var cache = BusinessMapper.ToDto(new CacheSettings { ID = 1, FK_CDNSERVER = 4, TYPE = "aws", PAYLOAD = "{}" });
            Assert.AreEqual(4, cache.CdnServerId);
            Assert.AreEqual(4, BusinessMapper.ToEntity(cache).FK_CDNSERVER);

            var cfg = BusinessMapper.ToDto(new WebsiteConfiguration { ID = 2, FK_WEBSITE = "w", TYPE = 1, URL = "u", NOTE = "n" });
            Assert.AreEqual(EnvironmentType.Stage, cfg.Type);
            Assert.AreEqual("w", cfg.Website);
            Assert.AreEqual("w", BusinessMapper.ToEntity(cfg).FK_WEBSITE);
        }

        [TestMethod]
        public void Lists_ArePreservedInOrder()
        {
            var list = BusinessMapper.ToDtoList(new[] { new Widget { KEY = "a" }, new Widget { KEY = "b" } });
            CollectionAssert.AreEqual(new[] { "a", "b" }, list.Select(w => w.Key).ToArray());
        }
    }
}
