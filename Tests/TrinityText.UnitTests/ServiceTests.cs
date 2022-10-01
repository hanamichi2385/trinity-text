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
            services.AddTransient<ITextTypeService, TextTypeService>();
            services.AddTransient<IPageTypeService, PageTypeService>();
            services.AddTransient<IWebsiteConfigurationService, WebsiteConfigurationService>();
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

        [TestMethod]
        public async Task TextTypeServiceTest()
        {

            var kernel = InitServices();

            var repo = kernel.GetService<ITextTypeService>();

            var result = await repo.GetAll();

            Assert.IsTrue(result.Success);
        }

        [TestMethod]
        public async Task PageTypesServiceTest()
        {

            var kernel = InitServices();

            var repo = kernel.GetService<IPageTypeService>();

            var result = await repo.GetAll();

            Assert.IsTrue(result.Success);
        }

        [TestMethod]
        public async Task PageTypesUserServiceTest()
        {

            var kernel = InitServices();

            var repo = kernel.GetService<IPageTypeService>();

            var websites = new[] { "ABC", "CDE" };
            var visibilies = new[] { "role1", "role2" };

            var result = await repo.GetAllByUser(websites, visibilies);

            Assert.IsTrue(result.Success);
        }

        [TestMethod]
        public async Task SearchTextServiceTest()
        {

            var kernel = InitServices();

            var repo = kernel.GetService<ITextService>();
            var searc = new SearchTextDTO()
            {
                WebsiteLanguages = new [] {"it"},
            };
            var result = await repo.Search(searc, 0, 10);

            Assert.IsTrue(result.Success);
        }

        [TestMethod]
        public async Task WebsiteConfigurationServiceTest()
        {

            var kernel = InitServices();

            var repo = kernel.GetService<IWebsiteConfigurationService>();
            
            var result = await repo.GetAll("ABC");

            Assert.IsTrue(result.Success);
        }

        [TestMethod]
        public async Task WebsiteConfigurationAddToServiceTest()
        {

            var kernel = InitServices();

            var repo = kernel.GetService<IWebsiteConfigurationService>();

            var result = await repo.AddTo("ABC", new int[0], new int[0]);

            Assert.IsTrue(result.Success);
        }
    }
}
