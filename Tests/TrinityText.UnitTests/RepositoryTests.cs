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
            services.AddDbContextPool<TrinityDbContext>(s => s.UseSqlServer(connectionString));
            services.AddTransient(typeof(IRepository<>), typeof(EFRepository<>));

            var serviceProvider = services.BuildServiceProvider();

            return serviceProvider;
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
