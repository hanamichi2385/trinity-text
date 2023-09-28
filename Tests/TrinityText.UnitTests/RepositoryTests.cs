using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrinityText.Domain;
using TrinityText.Domain.EF;

namespace TrinityText.UnitTests
{
    [TestClass]
    public class RepositoryTests
    {
        public static IConfiguration InitConfiguration()
        {
            var config = new ConfigurationBuilder()
               .AddUserSecrets<EFDomainTests>()
               .Build();

            return config;
        }

        public static IServiceProvider InitServices()
        {
            var config = InitConfiguration();

            var connectionString = config.GetSection("ConnectionString").Value;

            var services = new ServiceCollection();
            services.AddDbContextPool<TrinityEFContext>(s => s.UseSqlServer(connectionString));
            services.AddTransient(typeof(IRepository<>), typeof(EFRepository<>));

            var serviceProvider = services.BuildServiceProvider();

            return serviceProvider;
        }

        [TestMethod]
        public void RepositoriesTest()
        {
            //var config = InitConfiguration();
            var services = InitServices();

            var r1 = services.GetService<IRepository<CdnServer>>();
            var r2 = services.GetService<IRepository<CacheSettings>>();
            var r3 = services.GetService<IRepository<FtpServer>>();
            var r4 = services.GetService<IRepository<FtpServerPerCdnServer>>();
            var r5 = services.GetService<IRepository<Domain.File>>();
            var r6 = services.GetService<IRepository<Folder>>();
            var r7 = services.GetService<IRepository<PageType>>();
            var r8 = services.GetService<IRepository<Page>>();
            var r9 = services.GetService<IRepository<Publication>>();
            var r10 = services.GetService<IRepository<Text>>();
            var r11 = services.GetService<IRepository<TextRevision>>();
            var r12 = services.GetService<IRepository<TextType>>();
            var r13 = services.GetService<IRepository<TextTypePerWebsite>>();
            var r14 = services.GetService<IRepository<Widget>>();
            var r15 = services.GetService<IRepository<WebsiteConfiguration>>();

            var l1 = r1.Repository.ToList();
            var l2 = r2.Repository.ToList();
            var l3 = r3.Repository.ToList();
            var l4 = r4.Repository.ToList();
            var l5 = r5.Repository.ToList();
            var l6 = r6.Repository.ToList();
            var l7 = r7.Repository.ToList();
            var l8 = r8.Repository.ToList();
            var l9 = r9.Repository.ToList();
            var l10 = r10.Repository.ToList();
            var l11 = r11.Repository.ToList();
            var l12 = r12.Repository.ToList();
            var l13 = r13.Repository.ToList();
            var l14 = r14.Repository.ToList();
            var l15 = r15.Repository.ToList();
        }

        [TestMethod]
        public async Task CdnServersDomainTests()
        {
            var kernel = InitServices();

            var repo = kernel.GetService<IRepository<CdnServer>>();

            var cs0 = new CdnServer()
            {
                BASEURL = "base",
                NAME = "Test",
                TYPE = 0,
            };

            var r1 = await repo.Create(cs0);

            var servers = repo.Repository.ToList();

            var cs1 = servers.ElementAt(0);

            cs1.BASEURL = "base2";

            var r2 = await repo.Update(cs1);

            await repo.Delete(r2);

            var settings2 = repo.Repository
                .Where(cs => cs.ID == r2.ID)
                .ToList();

            Assert.IsTrue(settings2.Count == 0);
        }

        [TestMethod]
        public async Task TransactionTests()
        {
            var kernel = InitServices();

            var repo = kernel.GetService<IRepository<CdnServer>>();

            await repo.BeginTransaction();

            var cs0 = new CdnServer()
            {
                BASEURL = "base",
                NAME = "Test",
                TYPE = 0,
            };

            var r1 = await repo.Create(cs0);

            var servers = repo.Repository.ToList();

            var cs1 = servers.ElementAt(0);

            cs1.BASEURL = "base2";

            var r2 = await repo.Update(cs1);

            await repo.Delete(r2);

            await repo.CommitTransaction();

            var settings2 = repo.Repository
                .Where(cs => cs.ID == r2.ID)
                .ToList();

            Assert.IsTrue(settings2.Count == 0);
        }


        [TestMethod]
        public async Task TextsTests()
        {
            var kernel = InitServices();

            var repo = kernel.GetService<IRepository<TextType>>();

            var list = repo.Repository.ToList();

        }
    }
}
