using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;
using TrinityText.Business;
using TrinityText.Business.Services;
using TrinityText.Business.Services.Impl;
using TrinityText.Domain;
using TrinityText.Domain.EF;

namespace TrinityText.UnitTests
{
    [TestClass]
    public class ServiceTests
    {
        public static IConfiguration InitConfiguration()
        {
            var config = new ConfigurationBuilder()
               .AddUserSecrets<DomainTests>()
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
            services.AddTransient<ITextService, TextService>();
            services.AddLogging();

            services.AddAutoMapper((cfg) =>
            {
                cfg.AddProfile(typeof(Business.BusinessMapperProfile));
            });

            var serviceProvider = services.BuildServiceProvider();

            return serviceProvider;
        }

        [TestMethod]
        public async Task TextServiceTest()
        {

            var kernel = InitServices();

            var repo = kernel.GetService<ITextService>();

            var dto = new TextDTO()
            {
                Active = true,
                Country = "IT",
                Language = "it",
                Name = "TEXT_01",
                TextRevision = new TextRevisionDTO()
                {
                    Content = "Text sample 01",
                    CreationDate = DateTime.Now,
                    CreationUser = "test_admin",
                },

            };

            var result = await repo.Save(dto);

            Assert.IsTrue(result.Success);
        }
    }
}
