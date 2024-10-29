using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        [TestMethod]
        public async Task Export()
        {
            try
            {

                var kernel = InitServices();

                var generationService = kernel.GetService<IPublicationSupportService>();
                var cdnService = kernel.GetService<ICDNSettingService>();

                var cdn = await cdnService.Get(10);

                var pp = @"{""Sites"":[{""Tenant"":""KARTELL"",""Website"":""KARTELL"",""Site"":""KARTELLAE"",""CurrencyId"":""AED"",""Languages"":[""en""],""Countries"":[""AE""],""Description"":""Kartell AE"",""Enabled"":false},{""Tenant"":""KARTELL"",""Website"":""KARTELL"",""Site"":""KARTELLAT"",""CurrencyId"":""EUR"",""Languages"":[""de"",""en""],""Countries"":[""AT""],""Description"":""Kartell AT"",""Enabled"":false},{""Tenant"":""KARTELL"",""Website"":""KARTELL"",""Site"":""KARTELLBE"",""CurrencyId"":""EUR"",""Languages"":[""en"",""fr""],""Countries"":[""BE"",""LU"",""NL""],""Description"":""Kartell BE"",""Enabled"":false},{""Tenant"":""KARTELL"",""Website"":""KARTELL"",""Site"":""KARTELLCA"",""CurrencyId"":""CAD"",""Languages"":[""en"",""fr""],""Countries"":[""CA""],""Description"":""Kartell CA"",""Enabled"":false},{""Tenant"":""KARTELL"",""Website"":""KARTELL"",""Site"":""KARTELLCH"",""CurrencyId"":""CHF"",""Languages"":[""de"",""en"",""fr"",""it""],""Countries"":[""CH""],""Description"":""Kartell CH"",""Enabled"":false},{""Tenant"":""KARTELL"",""Website"":""KARTELL"",""Site"":""KARTELLCM"",""CurrencyId"":""EUR"",""Languages"":[""en""],""Countries"":[""CY"",""MT""],""Description"":""Kartell CM"",""Enabled"":false},{""Tenant"":""KARTELL"",""Website"":""KARTELL"",""Site"":""KARTELLDE"",""CurrencyId"":""EUR"",""Languages"":[""de"",""en""],""Countries"":[""DE""],""Description"":""Kartell DE"",""Enabled"":false},{""Tenant"":""KARTELL"",""Website"":""KARTELL"",""Site"":""KARTELLDK"",""CurrencyId"":""DKK"",""Languages"":[""en""],""Countries"":[""DK""],""Description"":""Kartell DK"",""Enabled"":false},{""Tenant"":""KARTELL"",""Website"":""KARTELL"",""Site"":""KARTELLEEU"",""CurrencyId"":""EUR"",""Languages"":[""en""],""Countries"":[],""Description"":""Kartell EEU"",""Enabled"":false},{""Tenant"":""KARTELL"",""Website"":""KARTELL"",""Site"":""KARTELLES"",""CurrencyId"":""EUR"",""Languages"":[""en"",""es""],""Countries"":[""ES""],""Description"":""Kartell ES"",""Enabled"":false},{""Tenant"":""KARTELL"",""Website"":""KARTELL"",""Site"":""KARTELLEU2"",""CurrencyId"":""EUR"",""Languages"":[""en""],""Countries"":[],""Description"":""Kartell EU2"",""Enabled"":false},{""Tenant"":""KARTELL"",""Website"":""KARTELL"",""Site"":""KARTELLEU3"",""CurrencyId"":""EUR"",""Languages"":[""en""],""Countries"":[],""Description"":""Kartell EU3"",""Enabled"":false},{""Tenant"":""KARTELL"",""Website"":""KARTELL"",""Site"":""KARTELLFI"",""CurrencyId"":""EUR"",""Languages"":[""en""],""Countries"":[""FI""],""Description"":""Kartell FI"",""Enabled"":false},{""Tenant"":""KARTELL"",""Website"":""KARTELL"",""Site"":""KARTELLFR"",""CurrencyId"":""EUR"",""Languages"":[""en"",""fr""],""Countries"":[""FR""],""Description"":""Kartell FR"",""Enabled"":false},{""Tenant"":""KARTELL"",""Website"":""KARTELL"",""Site"":""KARTELLGB"",""CurrencyId"":""GBP"",""Languages"":[""en""],""Countries"":[""GB""],""Description"":""Kartell GB"",""Enabled"":false},{""Tenant"":""KARTELL"",""Website"":""KARTELL"",""Site"":""KARTELLGR"",""CurrencyId"":""EUR"",""Languages"":[""en""],""Countries"":[""GR""],""Description"":""Kartell GR"",""Enabled"":false},{""Tenant"":""KARTELL"",""Website"":""KARTELL"",""Site"":""KARTELLHU"",""CurrencyId"":""EUR"",""Languages"":[""en""],""Countries"":[""HU""],""Description"":""Kartell HU"",""Enabled"":false},{""Tenant"":""KARTELL"",""Website"":""KARTELL"",""Site"":""KARTELLIE"",""CurrencyId"":""EUR"",""Languages"":[""en""],""Countries"":[""IE""],""Description"":""Kartell IE"",""Enabled"":false},{""Tenant"":""KARTELL"",""Website"":""KARTELL"",""Site"":""KARTELLIT"",""CurrencyId"":""EUR"",""Languages"":[""en"",""it""],""Countries"":[""IT""],""Description"":""Kartell IT"",""Enabled"":false},{""Tenant"":""KARTELL"",""Website"":""KARTELL"",""Site"":""KARTELLNO"",""CurrencyId"":""NOK"",""Languages"":[""en""],""Countries"":[""NO""],""Description"":""Kartell NO"",""Enabled"":false},{""Tenant"":""KARTELL"",""Website"":""KARTELL"",""Site"":""KARTELLPL"",""CurrencyId"":""EUR"",""Languages"":[""en""],""Countries"":[""PL""],""Description"":""Kartell PL"",""Enabled"":false},{""Tenant"":""KARTELL"",""Website"":""KARTELL"",""Site"":""KARTELLPT"",""CurrencyId"":""EUR"",""Languages"":[""en"",""es""],""Countries"":[""PT""],""Description"":""Kartell PT"",""Enabled"":false},{""Tenant"":""KARTELL"",""Website"":""KARTELL"",""Site"":""KARTELLROW"",""CurrencyId"":""USD"",""Languages"":[""en"",""es"",""fr""],""Countries"":[""IN""],""Description"":""Kartell ROW"",""Enabled"":false},{""Tenant"":""KARTELL"",""Website"":""KARTELL"",""Site"":""KARTELLSE"",""CurrencyId"":""SEK"",""Languages"":[""en""],""Countries"":[""SE""],""Description"":""Kartell SE"",""Enabled"":false},{""Tenant"":""KARTELL"",""Website"":""KARTELL"",""Site"":""KARTELLUS"",""CurrencyId"":""USD"",""Languages"":[""en""],""Countries"":[""US""],""Description"":""Kartell US"",""Enabled"":false}],""Website"":""KARTELL"",""Tenant"":""KARTELL"",""TextTypes"":[{""Id"":2,""Name"":""Labels"",""Note"":""Resources form Omni Shop"",""Subfolder"":null,""HasSubfolder"":false,""TextNumbers"":0},{""Id"":3,""Name"":""Panel"",""Note"":""Resources for Omni Panel"",""Subfolder"":null,""HasSubfolder"":false,""TextNumbers"":0},{""Id"":4,""Name"":""Payments"",""Note"":""Resource for Omni Shop Payment modules"",""Subfolder"":null,""HasSubfolder"":false,""TextNumbers"":0},{""Id"":8,""Name"":""PIM"",""Note"":""Resources for Omni PIM modules"",""Subfolder"":null,""HasSubfolder"":false,""TextNumbers"":0}]}";
                var payload = JsonSerializer.Deserialize<PayloadDTO>(pp);
                var stop = new Stopwatch();
                stop.Start();
                var generateRs = await generationService.CreateExportFile(1, payload, PublicationType.All, PublicationFormat.XML, DateTime.Today.AddDays(-1), true, "davide.chiarella", cdn.Value);
                stop.Stop();
            }
            catch (Exception ex)
            {

            }
        }
    }
}
