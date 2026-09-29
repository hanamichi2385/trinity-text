using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NHibernate;
using NHibernate.Dialect;
using NHibernate.Tool.hbm2ddl;
using System;
using TrinityText.Domain;
using TrinityText.Domain.EF;
using TrinityText.Domain.NH;

namespace TrinityText.UnitTests.Offline
{
    /// <summary>
    /// An in-memory SQLite database reached through one of the two persistence providers, so the same repository
    /// tests can run against EF Core and NHibernate without a SQL Server. Every scope is a fresh context / session
    /// (like a request), the data survives across scopes.
    /// </summary>
    public abstract class ProviderFixture : IDisposable
    {
        public abstract string Name { get; }

        public abstract ProviderScope NewScope();

        public abstract void Dispose();

        public static ProviderFixture Create(string provider) => provider switch
        {
            "EF" => new EfFixture(),
            "NH" => new NhFixture(),
            _ => throw new ArgumentException(provider),
        };

        private static SqliteConnection OpenMemoryDatabase()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            connection.Open();
            return connection;
        }

        private sealed class EfFixture : ProviderFixture
        {
            private readonly SqliteConnection _connection = OpenMemoryDatabase();
            private readonly DbContextOptions<TrinityEFContext> _options;

            public EfFixture()
            {
                _options = new DbContextOptionsBuilder<TrinityEFContext>().UseSqlite(_connection).Options;
                using var context = new TrinityEFContext(_options);
                context.Database.EnsureCreated();
            }

            public override string Name => "EF";

            public override ProviderScope NewScope()
            {
                var context = new TrinityEFContext(_options);
                return new ProviderScope(context, new EfRepositoryFactory(context), context.Dispose);
            }

            public override void Dispose() => _connection.Dispose();

            private sealed class EfRepositoryFactory(TrinityEFContext context) : IRepositoryFactory
            {
                public IRepository<T> Of<T>() where T : class, IEntity => new EFRepository<T>(context);
            }
        }

        private sealed class NhFixture : ProviderFixture
        {
            private readonly SqliteConnection _connection = OpenMemoryDatabase();
            private readonly ISessionFactory _factory;

            public NhFixture()
            {
                var cfg = new NHibernate.Cfg.Configuration()
                    .DataBaseIntegration(db =>
                    {
                        db.Dialect<SQLiteDialect>();
                        db.Driver<MicrosoftDataSqliteDriver>();
                        db.ConnectionString = "Data Source=:memory:";
                    })
                    .CurrentSessionContext<NHibernate.Context.CallSessionContext>();
                // Microsoft.Data.Sqlite has no "DataTypes" schema collection: do not read keywords from the database
                cfg.SetProperty(NHibernate.Cfg.Environment.Hbm2ddlKeyWords, "none");
                cfg.AddMapping(new TrinityModelMapper().CompileMappingForAllExplicitlyAddedEntities());
                cfg.BuildMappings();

                // SQLite has no schemas: the mappings use "dbo"
                foreach (var mapping in cfg.ClassMappings)
                {
                    mapping.Table.Schema = null;
                }

                _factory = cfg.BuildSessionFactory();
                new SchemaExport(cfg).Execute(false, true, false, _connection, null);
            }

            public override string Name => "NH";

            public override ProviderScope NewScope()
            {
                var session = _factory.WithOptions().Connection(_connection).OpenSession();
                var context = new TrinityNHContext(session);
                return new ProviderScope(context, new NhRepositoryFactory(context), session.Dispose);
            }

            public override void Dispose()
            {
                _factory.Dispose();
                _connection.Dispose();
            }

            private sealed class NhRepositoryFactory(TrinityNHContext context) : IRepositoryFactory
            {
                public IRepository<T> Of<T>() where T : class, IEntity => new NHRepository<T>(context);
            }
        }
    }

    public interface IRepositoryFactory
    {
        IRepository<T> Of<T>() where T : class, IEntity;
    }

    public sealed class ProviderScope : IDisposable
    {
        private readonly IRepositoryFactory _factory;
        private readonly Action _dispose;

        public ProviderScope(object context, IRepositoryFactory factory, Action dispose)
        {
            Context = context;
            _factory = factory;
            _dispose = dispose;
        }

        public object Context { get; }

        public IRepository<T> Repo<T>() where T : class, IEntity => _factory.Of<T>();

        public void Dispose() => _dispose();
    }
}

namespace TrinityText.UnitTests.Offline
{
    /// <summary>NHibernate 5.6 ships no driver for Microsoft.Data.Sqlite: minimal reflection-based one (tests only).</summary>
    public class MicrosoftDataSqliteDriver : NHibernate.Driver.ReflectionBasedDriver
    {
        public MicrosoftDataSqliteDriver()
            : base("Microsoft.Data.Sqlite", "Microsoft.Data.Sqlite", "Microsoft.Data.Sqlite.SqliteConnection", "Microsoft.Data.Sqlite.SqliteCommand")
        {
        }

        public override bool UseNamedPrefixInSql => true;

        public override bool UseNamedPrefixInParameter => true;

        public override string NamedPrefix => "@";

        public override bool SupportsMultipleOpenReaders => false;
    }
}
