using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using TrinityText.Business;
using TrinityText.Business.Services.Impl;
using TrinityText.Domain;
using TrinityText.Domain.EF;
using TrinityText.ServiceBus.MassTransit.Services;
using TrinityText.Utilities;
using TrinityText.Utilities.Transfer;

namespace TrinityText.UnitTests
{
    [TestClass]
    public class ServiceTests
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
            services.AddDbContextPool<TrinityEFContext>(s => s.UseSqlServer(connectionString)
                .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
                );
            services.AddTransient(typeof(IRepository<>), typeof(EFRepository<>));
            services.AddTransient<ITextService, TextService>();
            services.AddTransient<ITextTypeService, TextTypeService>();
            services.AddTransient<IPageSchemaService, PageSchemaService>();
            services.AddTransient<IPageService, PageService>();
            services.AddTransient<IPageTypeService, PageTypeService>();
            services.AddTransient<IWebsiteConfigurationService, WebsiteConfigurationService>();
            services.AddTransient<IWidgetService, WidgetService>();
            services.AddTransient<IFTPServerService, FTPServerService>();
            services.AddTransient<ICDNSettingService, CDNSettingService>();
            services.AddTransient<IWidgetUtilities, WidgetUtilities>();
            services.AddTransient<IPublicationSupportService, MassTransitPublicationSupportService>();

            services.AddTransient<IFileManagerService, FileManagerService>();
            services.AddScoped<ITransferServiceCoordinator>((service) =>
            {
                var dictionary = new Dictionary<string, ITransferService>();
                var sftp = service.GetServices<ITransferService>();


                var ts = new TransferService(sftp.ToList());

                return ts;
            });
            services.AddTransient<ICompressionFileService, ZipCompressionService>();
            services.AddTransient<IPublicationService, PublicationService>();
            services.AddTransient<IImageDrawingService, WebPImageDrawingService>();
            services.AddTransient<ITransferService, SFTPTransferService>();

            services.Configure<PublicationSupportOptions>(opt => opt = new PublicationSupportOptions()
            {
                LocalDirectory = "C://Temp"
            });
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
        public async Task PageServiceTest()
        {

            var kernel = InitServices();

            var repo = kernel.GetService<IPageService>();

            var dto = new PageDTO()
            {
                Active = true,
                Id = 10,
                Content = "Text sample 01",
                CreationDate = DateTime.Now,
                CreationUser = "test_admin",
                Language = "it",
                PageTypeId = 14,
                Title = "test 3",
                Website = "ABC",
                LastUpdateUser = "test_admin",
                LastUpdate = DateTime.Now,
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
        public async Task SavePageTypesServiceTest()
        {

            var kernel = InitServices();

            var repo = kernel.GetService<IPageTypeService>();

            var dto = new PageTypeDTO()
            {
                Name = "Ciao2",
                Schema = "<root id=\"Contents\"><content id=\"Content\"><textpart ishtml=\"false\" id=\"Group\" isrequired=\"false\" /><textpart ishtml=\"false\" id=\"Id\" isrequired=\"true\" /><textpart ishtml=\"false\" id=\"Visible_From\" isrequired=\"false\" /><textpart ishtml=\"false\" id=\"Visible_To\" isrequired=\"false\" /><textpart ishtml=\"false\" id=\"SEO_Title\" isrequired=\"false\" /><textpart ishtml=\"false\" id=\"SEO_Description\" isrequired=\"false\" /><textpart ishtml=\"false\" id=\"Title\" isrequired=\"true\" /><textpart ishtml=\"false\" id=\"Subtitle\" isrequired=\"false\" /><textpart ishtml=\"true\" id=\"Body\" isrequired=\"true\" /><textpart ishtml=\"true\" id=\"CSS\" isrequired=\"false\" /><textpart ishtml=\"true\" id=\"Javascript\" isrequired=\"false\" /></content></root>",
                OutputFilename = "Ciao",
                Visibility = new[] { "ROLE1", "ROLE2" }
            };

            var result = await repo.Save(dto);

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
                WebsiteLanguages = new[] { "it" },
            };
            var result = await repo.Search(searc, 0, 100);

            Assert.IsTrue(result.Success);
        }

        [TestMethod]
        public async Task SearchPageServiceTest()
        {

            var kernel = InitServices();

            var repo = kernel.GetService<IPageService>();
            var searc = new SearchPageDTO()
            {
                WebsiteLanguages = new[] { "it" },
                UserWebsites = new[] {"EXPERT", "KARTELL"},
                ExcludeContent = true,
            };
            var result = await repo.Search(searc, 0, 100);

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

            var result = await repo.AddTo("ABC", Array.Empty<int>(), Array.Empty<int>());

            Assert.IsTrue(result.Success);
        }

        [TestMethod]
        public async Task FtpServerServiceTest()
        {

            var kernel = InitServices();

            var repo = kernel.GetService<IFTPServerService>();

            var result = await repo.GetAll();

            Assert.IsTrue(result.Success);
        }

        [TestMethod]
        public async Task SaveFtpServerServiceTest()
        {

            var kernel = InitServices();

            var repo = kernel.GetService<IFTPServerService>();

            var ftp = new FTPServerDTO()
            {
                Host = "host",
                Name = "ftp",
                Password = "password",
                Port = 21,
                Type = EnvironmentType.Development,
                Username = "username"
            };

            var result = await repo.Save(ftp);

            Assert.IsTrue(result.Success);
        }

        [TestMethod]
        public async Task SaveCDNServerServiceTest()
        {

            var kernel = InitServices();

            var repo = kernel.GetService<ICDNSettingService>();

            var ftp = new CdnServerDTO()
            {
                Name = "ftp",
                Type = EnvironmentType.Development,
                BaseUrl = "https://cdn.it",
            };

            var result = await repo.Save(ftp, new[] { 1 });

            Assert.IsTrue(result.Success);
        }

        [TestMethod]
        public async Task CDNServerServiceTest()
        {

            var kernel = InitServices();

            var repo = kernel.GetService<ICDNSettingService>();

            var result = await repo.GetAll();

            Assert.IsTrue(result.Success);
        }


        [TestMethod]
        public async Task WidgetServiceTest()
        {

            var kernel = InitServices();

            var repo = kernel.GetService<IWidgetService>();

            var ftp = new WidgetDTO()
            {
                Key = "W1",
                Language = "it",
                CreationUser = "test_admin",
                LastUpdateUser = "test_admin"
            };

            var result = await repo.Save(ftp);

            Assert.IsTrue(result.Success);
        }

        [TestMethod]
        public async Task WidgetsServiceTest()
        {

            var kernel = InitServices();

            var repo = kernel.GetService<IWidgetService>();

            var search = new SearchWidgetDTO()
            {
                WebsiteLanguages = new[] { "it" },
            };
            var result = await repo.Search(search, 0, 10);

            Assert.IsTrue(result.Success);
        }

        [TestMethod]
        public async Task Generation()
        {
            try
            {

                var kernel = InitServices();

                var generationService = kernel.GetService<IPublicationSupportService>();
                var cdnService = kernel.GetService<ICDNSettingService>();

                var cdn = await cdnService.Get(10);

                var setting = new PublicationDTO()
                {
                    Id = 4217,
                    Website = "BARNINEXYIU",
                    CdnServer = cdn.Value,
                    Format = PublicationFormat.XML,
                    DataType = PublicationType.All,
                    HasZipFile = true,
                    CreationUser = "camilla.rizzolo"
                };

                setting.SetPayload(@"{""Sites"":[{""Tenant"":""BARNI"",""Website"":""BARNINEXYIU"",""Site"":""BARNINEXYIU"",""CurrencyId"":""EUR"",""Languages"":[""it""],""Countries"":[""IT""],""Description"":""Barni Nexyiu"",""Enabled"":false}],""Website"":""BARNINEXYIU"",""Tenant"":""BARNI"",""TextTypes"":[]}");

                var generateRs = await generationService.Generate(setting);
            }catch(Exception ex)
            {

            }
        }
    }
}
