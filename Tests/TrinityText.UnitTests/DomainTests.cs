using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TrinityText.Domain;
using TrinityText.Domain.EF;
using System.Linq;

namespace TrinityText.UnitTests
{
    [TestClass]
    public class DomainTests
    {
        public static IConfiguration InitConfiguration()
        {
            var config = new ConfigurationBuilder()
               .AddUserSecrets<DomainTests>()
               .Build();
            return config;
        }

        [TestMethod]
        public void CdnServersDomainTests()
        {
            var config = InitConfiguration();

            var connectionString = config.GetSection("ConnectionString").Value;

            var options = new DbContextOptionsBuilder<TrinityDbContext>()
                .UseSqlServer(connectionString)
                .Options;

            using (TrinityDbContext ctx = new TrinityDbContext(options))
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

                var ftp = new FtpServer()
                {
                    HOST = "host 1",
                    NAME = "name 1",
                    PASSWORD = "abc",
                    PORT = 21,
                    TYPE = 0,
                    USERNAME = "pippo",
                };

                ctx.Add(ftp);

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
    }
}
