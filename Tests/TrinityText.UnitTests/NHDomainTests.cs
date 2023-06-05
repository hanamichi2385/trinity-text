using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NHibernate.Dialect;
using NHibernate.NetCore;
using System;
using System.IO;
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

                });
                cfg.Properties.Add("current_session_context_class", "thread");

            //var file = Path.Combine(
            //    AppDomain.CurrentDomain.BaseDirectory,
            //    "Config/Trinity.NHibernate.xml"
            //);
            //cfg.Configure(file);

            var modelMapper = new TrinityModelMapper();
            cfg.AddMapping(modelMapper.CompileMappingForAllExplicitlyAddedEntities());

            services.AddHibernate(cfg);
            services.AddTransient(typeof(IRepository<>), typeof(NHRepository<>));
            var serviceProvider = services.BuildServiceProvider();

            return serviceProvider;
        }

        [TestMethod]
        public void CdnServersDomainTests()
        {
            //var config = InitConfiguration();
            var services = InitServices();

            var r1 = services.GetService<IRepository<CdnServer>>();
            var r2 = services.GetService<IRepository<CacheSettings>>();
            var r3 = services.GetService<IRepository<FtpServer>>();

            var l1 = r1.Repository.ToList();
            var l2 = r2.Repository.ToList();
            var l3 = r3.Repository.ToList();
        }
    }
}
