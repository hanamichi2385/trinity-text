using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NHibernate.Dialect;
using System;
using System.Linq;
using TrinityText.Domain;
using TrinityText.Domain.NH;

namespace TrinityText.UnitTests
{
    [TestClass]
    public class NHDomainTests
    {
        public static IConfiguration InitConfiguration()
        {
            var config = new ConfigurationBuilder()
               .AddUserSecrets<NHDomainTests>()
               .Build();

            

            return config;
        }

        public static IServiceProvider InitServices()
        {
            var config = InitConfiguration();

            var connectionString = config.GetSection("ConnectionString").Value;

            var services = new ServiceCollection();
            var cfg = new NHibernate.Cfg.Configuration()
                .DataBaseIntegration(db =>
                {
                    db.Dialect<MsSql2012Dialect>();
                    db.ConnectionString = connectionString;
                    db.BatchSize = 100;

                })
                .CurrentSessionContext<NHibernate.Context.CallSessionContext>()
                ;

            //var file = Path.Combine(
            //    AppDomain.CurrentDomain.BaseDirectory,
            //    "Config/Trinity.NHibernate.xml"
            //);
            //cfg.Configure(file);

            services.AddTrinityWithNHibernate(cfg);
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
            var l9= r9.Repository.ToList();
            var l10 = r10.Repository.ToList();
            var l11 = r11.Repository.ToList();
            var l12 = r12.Repository.ToList();
            var l13 = r13.Repository.ToList();
            var l14 = r14.Repository.ToList();
            var l15 = r15.Repository.ToList();
        }

        [TestMethod]
        public void IsNullOrEmptyTest()
        {
            var services = InitServices();
            var textsRepository = services.GetService<IRepository<Text>>();
            var lastTexts = textsRepository
                 .Repository
                 .Where(t => ((t.FK_WEBSITE == null || t.FK_WEBSITE.Trim() == string.Empty)) && t.ACTIVE == true)
                 .OrderByDescending(r => r.REVISIONS.Max(s => s.CREATION_DATE))
                 .Take(5)
                 .ToList();

            Assert.IsTrue(lastTexts.Any());
        }
    }
}
