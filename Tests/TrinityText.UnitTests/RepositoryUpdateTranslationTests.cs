using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NHibernate.Dialect;
using TrinityText.Domain.NH;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Threading.Tasks;
using TrinityText.Domain;
using TrinityText.Domain.EF;

namespace TrinityText.UnitTests
{
    /// <summary>
    /// Offline check (no database needed): ExecuteUpdateAsync must build a valid EF expression and get
    /// translated to SQL. With an unreachable server the only acceptable failure is the connection error,
    /// raised after translation succeeded.
    /// </summary>
    [TestClass]
    public class RepositoryUpdateTranslationTests
    {
        [TestMethod]
        public async Task EFExecuteUpdateAsync_BuildsAndTranslatesSetters()
        {
            var options = new DbContextOptionsBuilder<TrinityEFContext>()
                .UseSqlServer("Server=127.0.0.1,1;Database=x;User Id=u;Password=p;Connect Timeout=1;Encrypt=False")
                .Options;

            using var ctx = new TrinityEFContext(options);
            var repository = new EFRepository<File>(ctx);
            var id = Guid.NewGuid();
            var now = DateTime.Now;

            try
            {
                await repository.ExecuteUpdateAsync(
                    repository.Repository.Where(f => f.ID == id),
                    set => set
                        .Set(f => f.FILENAME, "new-name.png")
                        .Set(f => f.FK_FOLDER, 3)
                        .Set(f => f.LASTUPDATE_DATE, now)
                        .Set(f => f.LASTUPDATE_USER, "user"));

                Assert.Fail("The server is unreachable: the command cannot succeed");
            }
            catch (SqlException)
            {
                // connection failure = expression built and translated correctly
            }
        }

        [TestMethod]
        public async Task NHExecuteUpdateAsync_BuildsAndTranslatesSetters()
        {
            var cfg = new NHibernate.Cfg.Configuration()
                .DataBaseIntegration(db =>
                {
                    db.Dialect<MsSql2012Dialect>();
                    db.Driver<NHibernate.Driver.MicrosoftDataSqlClientDriver>();
                    db.ConnectionString = "Server=127.0.0.1,1;Database=x;User Id=u;Password=p;Connect Timeout=1;Encrypt=False";
                })
                .CurrentSessionContext<NHibernate.Context.CallSessionContext>();

            // building the session factory must not read keywords from the (unreachable) database
            cfg.SetProperty(NHibernate.Cfg.Environment.Hbm2ddlKeyWords, "none");

            var services = new ServiceCollection();
            services.AddTrinityWithNHibernate(cfg);
            using var provider = services.BuildServiceProvider();
            using var scope = provider.CreateScope();

            var repository = scope.ServiceProvider.GetRequiredService<IRepository<File>>();
            var id = Guid.NewGuid();
            var now = DateTime.Now;

            try
            {
                await repository.ExecuteUpdateAsync(
                    repository.Repository.Where(f => f.ID == id),
                    set => set
                        .Set(f => f.FILENAME, "new-name.png")
                        .Set(f => f.FK_FOLDER, 3)
                        .Set(f => f.LASTUPDATE_DATE, now)
                        .Set(f => f.LASTUPDATE_USER, "user"));

                Assert.Fail("The server is unreachable: the command cannot succeed");
            }
            catch (Exception ex) when (HasSqlException(ex))
            {
                // connection failure = HQL built and translated correctly
            }
        }

        private static bool HasSqlException(Exception ex)
        {
            for (var e = ex; e != null; e = e.InnerException)
            {
                if (e is SqlException)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
