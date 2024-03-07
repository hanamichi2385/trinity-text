using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using TrinityText.Domain;
using TrinityText.Domain.EF;

namespace TrinityText.UnitTests
{
    [TestClass]
    public class EFDomainTests
    {
        public static IConfiguration InitConfiguration()
        {
            var config = new ConfigurationBuilder()
               .AddUserSecrets<EFDomainTests>()
               .Build();
            return config;
        }

        [TestMethod]
        public void CdnServersDomainTests()
        {
            var config = InitConfiguration();

            var connectionString = config.GetSection("ConnectionString").Value;

            var options = new DbContextOptionsBuilder<TrinityEFContext>()
                .UseSqlServer(connectionString)
                .Options;

            using (TrinityEFContext ctx = new TrinityEFContext(options))
            {
                var cs0 = new CdnServer()
                {
                    BASEURL = "base",
                    NAME = "Test",
                    TYPE = 0,
                };

                ctx.Add(cs0);

                ctx.SaveChanges();

                var servers = ctx.Set<CdnServer>().ToList();

                var cs1 = servers.ElementAt(0);

                cs1.BASEURL = "base2";

                ctx.Update(cs1);

                ctx.SaveChanges();

                var cst0 = new CacheSettings()
                {
                    PAYLOAD = "anc",
                    TYPE = "aws",
                    CDNSERVER = cs1
                };

                ctx.Add(cst0);

                ctx.SaveChanges();

                var settings = ctx.Set<CacheSettings>().ToList();

                var cst1 = settings.ElementAt(0);

                cst1.PAYLOAD = "anc2";

                ctx.Update(cst1);

                ctx.SaveChanges();

                ctx.Remove(cst1);

                ctx.SaveChanges();

                var cw = new CdnServersPerWebsite()
                {
                    CDNSERVER = cs1,
                    FK_WEBSITE = "TRINITY"
                };

                ctx.Add(cw);

                ctx.SaveChanges();

                ctx.SaveChanges();

                var settings2 = ctx.Set<CacheSettings>()
                    .Where(cs => cs.ID == cst1.ID)
                    .ToList();

                Assert.IsTrue(settings2.Count == 0);

                ctx.Remove(cs1);

                ctx.SaveChanges();

                var servers2 = ctx.Set<CdnServer>().ToList();

            }
        }

        [TestMethod]
        public void FoldersAndFilesDomainTests()
        {
            var config = InitConfiguration();

            var connectionString = config.GetSection("ConnectionString").Value;

            var options = new DbContextOptionsBuilder<TrinityEFContext>()
                .UseSqlServer(connectionString)
                .Options;

            using (TrinityEFContext ctx = new TrinityEFContext(options))
            {

                var cs0 = new Folder()
                {
                    FK_WEBSITE = "TRINITY",
                    NAME = "Folder 1",
                    NOTE = "Note",
                };

                ctx.Add(cs0);

                ctx.SaveChanges();

                var cs1 = new Folder()
                {
                    FK_WEBSITE = "TRINITY",
                    NAME = "Folder 2",
                    NOTE = "Note",
                    FK_PARENT = cs0.FK_PARENT
                };

                ctx.Add(cs1);

                ctx.SaveChanges();

                var f0 = new File()
                {
                    CONTENT = System.Array.Empty<byte>(),
                    CREATION_DATE = System.DateTime.Now,
                    FK_FOLDER = cs1.ID,
                    FILENAME = "File 1",
                    FK_WEBSITE = "TRINITY",
                    CREATION_USER = "hanamichi2385",
                    LASTUPDATE_DATE = System.DateTime.Now,
                    LASTUPDATE_USER = "hanamichi2385"
                };

                ctx.Add(f0);

                ctx.SaveChanges();

                var files = ctx.Set<File>().Where(f => f.FK_FOLDER == cs1.ID).ToList();

                Assert.IsTrue(files.Count == 1);

                ctx.RemoveRange(files);
                ctx.Remove(cs1);
                ctx.Remove(cs0);

                ctx.SaveChanges();
            }
        }

        [TestMethod]
        public void PageDomainTests()
        {
            var config = InitConfiguration();

            var connectionString = config.GetSection("ConnectionString").Value;

            var options = new DbContextOptionsBuilder<TrinityEFContext>()
                .UseSqlServer(connectionString)
                .Options;

            using (TrinityEFContext ctx = new TrinityEFContext(options))
            {

                var cs0 = new PageType()
                {
                    NAME = "Type 1",
                    OUTPUT_FILENAME = "filename",
                    PATH_PREVIEWPAGE = "preview",
                    PRINT_ELEMENT_NAME = "caio",
                    SUBFOLDER = "subfolder",
                    VISIBILITY = "user",
                    SCHEMA = "Schema",
                };

                ctx.Add(cs0);

                ctx.SaveChanges();

                var cs1 = new Page()
                {
                    ACTIVE = true,
                    CONTENT = "ciao",
                    CREATION_DATE = System.DateTime.Now,
                    LASTUPDATE_DATE = System.DateTime.Now,
                    FK_LANGUAGE = "it",
                    PAGETYPE = cs0,
                    FK_PRICELIST = "TRINITY 2",
                    FK_WEBSITE = "TRINITY 2",
                    GENERATE_PDF = true,
                    TITLE = "titolo",
                    CREATION_USER = "utente",
                    LASTUPDATE_USER = "utente"
                };

                ctx.Add(cs1);

                ctx.SaveChanges();

                ctx.Remove(cs1);
                ctx.Remove(cs0);

                ctx.SaveChanges();
            }
        }

        [TestMethod]
        public void TextDomainTests()
        {
            var config = InitConfiguration();

            var connectionString = config.GetSection("ConnectionString").Value;

            var options = new DbContextOptionsBuilder<TrinityEFContext>()
                .UseSqlServer(connectionString)
                .Options;

            using (TrinityEFContext ctx = new TrinityEFContext(options))
            {

                var cs0 = new TextType()
                {
                    NOTE = "note",
                    CONTENTTYPE = "Type"
                };

                var tt0 = new TextTypePerWebsite()
                {
                    TEXTTYPE = cs0,
                    FK_WEBSITE = "1"
                };

                ctx.Add(cs0);
                ctx.Add(tt0);

                ctx.SaveChanges();

                var cs1 = new Text()
                {
                    ACTIVE = true,
                    FK_LANGUAGE = "it",
                    FK_COUNTRY = "IT",
                    FK_PRICELIST = "PRICELIST",
                    TEXTTYPE = cs0,
                    FK_WEBSITE = "WEBSITE",
                    NAME = "NAME",
                };

                var cs2 = new TextRevision()
                {
                    CREATION_DATE = System.DateTime.Now,
                    TEXT = cs1,
                    CREATION_USER = "hanamichi2385",
                    CONTENT = "text",
                    //REVISION_NUMBER = 1,
                };

                ctx.Add(cs1);
                ctx.Add(cs2);

                ctx.SaveChanges();

                ctx.Remove(cs2);
                ctx.Remove(cs1);
                ctx.Remove(cs0);

                ctx.SaveChanges();
            }
        }

        [TestMethod]
        public void WidgetDomainTests()
        {
            var config = InitConfiguration();

            var connectionString = config.GetSection("ConnectionString").Value;

            var options = new DbContextOptionsBuilder<TrinityEFContext>()
                .UseSqlServer(connectionString)
                .Options;

            using (TrinityEFContext ctx = new TrinityEFContext(options))
            {

                var cs0 = new Widget()
                {
                    KEY = "key",
                    CONTENT = "text",
                    CREATION_DATE = System.DateTime.Now,
                    LASTUPDATE_DATE = System.DateTime.Now,
                    FK_LANGUAGE = "it",
                    FK_PRICELIST = "pricelist",
                    FK_WEBSITE = "website",
                    CREATION_USER = "user",
                    LASTUPDATE_USER = "user",
                };

                ctx.Add(cs0);

                ctx.SaveChanges();

                ctx.Remove(cs0);
                ctx.SaveChanges();
            }
        }

        [TestMethod]
        public void PublicationDomainTests()
        {
            var config = InitConfiguration();

            var connectionString = config.GetSection("ConnectionString").Value;

            var options = new DbContextOptionsBuilder<TrinityEFContext>()
                .UseSqlServer(connectionString)
                .Options;

            using (TrinityEFContext ctx = new TrinityEFContext(options))
            {
                var cs0 = new CdnServer()
                {
                    BASEURL = "base",
                    NAME = "Test",
                    TYPE = 0,
                };
                var cs1 = new Publication()
                {
                    CDNSERVER = cs0,
                    FILTERDATA_DATE = System.DateTime.Now,
                    EMAIL = "da",
                    FTPSERVER = null,
                    PAYLOAD = "payload",
                    MANUALDELETE = true,
                    STATUS_MESSAGE = "status",
                    STATUS_CODE = 404,
                    DATATYPE = 1,
                    FORMAT =1,
                    LASTUPDATE_DATE = System.DateTime.Now,
                    //WEBSITE = "ciao",
                    //ZIP_FILE = new byte[0],
                    CREATION_USER = "user",
                    FK_WEBSITE = "website"
                };

                ctx.Add(cs1);
                ctx.Add(cs0);

                ctx.SaveChanges();

                ctx.Remove(cs0);
                ctx.Remove(cs1);
                ctx.SaveChanges();
            }
        }
    }
}
