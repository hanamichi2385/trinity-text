using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NHibernate.Dialect;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using TrinityText.Domain;
using TrinityText.Domain.EF;
using TrinityText.Domain.NH;

namespace TrinityText.UnitTests.Offline
{
    /// <summary>
    /// EF Core model, NHibernate mapping and the database script must describe the same tables and columns:
    /// a column missing from one mapping is silently read as null / dropped on write by that provider.
    /// </summary>
    [TestClass]
    [TestCategory("Offline")]
    public class PersistenceParityTests
    {
        private static readonly Type[] EntityTypes = typeof(IEntity).Assembly
            .GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(IEntity).IsAssignableFrom(t))
            .ToArray();

        private static IEqualityComparer<string> Comparer => StringComparer.OrdinalIgnoreCase;

        private static Dictionary<Type, (string Table, HashSet<string> Columns)> ReadEf()
        {
            var options = new DbContextOptionsBuilder<TrinityEFContext>()
                .UseSqlServer("Server=127.0.0.1,1;Database=x;Encrypt=False")
                .Options;

            using var context = new TrinityEFContext(options);
            var result = new Dictionary<Type, (string, HashSet<string>)>();
            foreach (var entityType in context.Model.GetEntityTypes().Where(e => EntityTypes.Contains(e.ClrType)))
            {
                var table = entityType.GetTableName();
                var id = StoreObjectIdentifier.Table(table, entityType.GetSchema());
                var columns = entityType.GetProperties()
                    .Select(p => p.GetColumnName(id))
                    .Where(c => c != null)
                    .ToHashSet(Comparer);
                result[entityType.ClrType] = (table, columns);
            }

            return result;
        }

        private static Dictionary<Type, (string Table, HashSet<string> Columns)> ReadNh()
        {
            var cfg = new NHibernate.Cfg.Configuration()
                .DataBaseIntegration(db =>
                {
                    db.Dialect<MsSql2012Dialect>();
                    db.Driver<NHibernate.Driver.MicrosoftDataSqlClientDriver>();
                    db.ConnectionString = "Server=127.0.0.1,1;Database=x;Encrypt=False";
                });
            cfg.AddMapping(new TrinityModelMapper().CompileMappingForAllExplicitlyAddedEntities());
            cfg.BuildMappings();

            return cfg.ClassMappings
                .Where(c => EntityTypes.Contains(c.MappedClass))
                .ToDictionary(
                    c => c.MappedClass,
                    c => (c.Table.Name, c.Table.ColumnIterator.Select(col => col.Name).ToHashSet(Comparer)));
        }

        private static Dictionary<string, HashSet<string>> ReadSql()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null && !System.IO.File.Exists(Path.Combine(directory.FullName, "Trinity.sln")))
            {
                directory = directory.Parent;
            }

            Assert.IsNotNull(directory, "Trinity.sln not found");
            var script = System.IO.File.ReadAllText(Path.Combine(directory.FullName, "Core", "TrinityText.Domain", "Db", "create_table.sql"));

            var tables = new Dictionary<string, HashSet<string>>(Comparer);
            foreach (Match table in Regex.Matches(script, @"CREATE TABLE \[dbo\]\.\[(\w+)\]\((.*?)\n\s*(?:CONSTRAINT|\) ON)", RegexOptions.Singleline))
            {
                tables[table.Groups[1].Value] = Regex.Matches(table.Groups[2].Value, @"^\s*\[(\w+)\]\s+\[", RegexOptions.Multiline)
                    .Select(m => m.Groups[1].Value)
                    .ToHashSet(Comparer);
            }

            return tables;
        }

        [TestMethod]
        public void EveryEntity_IsMappedByBothProviders()
        {
            var ef = ReadEf();
            var nh = ReadNh();

            var problems = new StringBuilder();
            foreach (var type in EntityTypes)
            {
                if (!ef.ContainsKey(type)) problems.AppendLine($"{type.Name}: not mapped by EF Core");
                if (!nh.ContainsKey(type)) problems.AppendLine($"{type.Name}: not mapped by NHibernate");
            }

            Assert.AreEqual(string.Empty, problems.ToString());
        }

        [TestMethod]
        public void EfCore_NHibernate_AndTheDatabaseScript_UseTheSameTablesAndColumns()
        {
            var ef = ReadEf();
            var nh = ReadNh();
            var sql = ReadSql();

            var problems = new StringBuilder();
            foreach (var type in EntityTypes.Where(t => ef.ContainsKey(t) && nh.ContainsKey(t)))
            {
                var (efTable, efColumns) = ef[type];
                var (nhTable, nhColumns) = nh[type];

                if (!Comparer.Equals(efTable, nhTable))
                {
                    problems.AppendLine($"{type.Name}: table EF '{efTable}' != NH '{nhTable}'");
                }

                if (!sql.TryGetValue(efTable, out var sqlColumns))
                {
                    problems.AppendLine($"{type.Name}: table '{efTable}' not in create_table.sql");
                    continue;
                }

                Report(problems, type, "in EF, missing in NH", efColumns.Except(nhColumns, Comparer));
                Report(problems, type, "in NH, missing in EF", nhColumns.Except(efColumns, Comparer));
                Report(problems, type, "mapped by EF but not in the script", efColumns.Except(sqlColumns, Comparer));
                Report(problems, type, "mapped by NH but not in the script", nhColumns.Except(sqlColumns, Comparer));
            }

            Assert.AreEqual(string.Empty, problems.ToString());
        }

        private static bool IsScalar(Type type)
        {
            type = Nullable.GetUnderlyingType(type) ?? type;
            return type.IsPrimitive || type.IsEnum || type == typeof(string) || type == typeof(decimal)
                || type == typeof(DateTime) || type == typeof(Guid) || type == typeof(byte[]);
        }

        [TestMethod]
        public void EveryScalarProperty_IsMappedByNHibernate()
        {
            var cfg = new NHibernate.Cfg.Configuration()
                .DataBaseIntegration(db =>
                {
                    db.Dialect<MsSql2012Dialect>();
                    db.Driver<NHibernate.Driver.MicrosoftDataSqlClientDriver>();
                    db.ConnectionString = "Server=127.0.0.1,1;Database=x;Encrypt=False";
                });
            cfg.AddMapping(new TrinityModelMapper().CompileMappingForAllExplicitlyAddedEntities());
            cfg.BuildMappings();

            var problems = new StringBuilder();
            foreach (var type in EntityTypes)
            {
                var persistentClass = cfg.GetClassMapping(type);
                var mapped = persistentClass.PropertyClosureIterator.Select(p => p.Name).ToHashSet(Comparer);
                if (persistentClass.IdentifierProperty != null)
                {
                    mapped.Add(persistentClass.IdentifierProperty.Name);
                }
                if (persistentClass.Identifier is NHibernate.Mapping.Component composite)
                {
                    foreach (var p in composite.PropertyIterator)
                    {
                        mapped.Add(p.Name);
                    }
                }

                var unmapped = type
                    .GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                    .Where(p => p.CanWrite && IsScalar(p.PropertyType) && !mapped.Contains(p.Name))
                    .Select(p => p.Name)
                    .OrderBy(n => n)
                    .ToList();

                if (unmapped.Count > 0)
                {
                    problems.AppendLine($"{type.Name}: properties not mapped by NHibernate (never read / written): {string.Join(", ", unmapped)}");
                }
            }

            Assert.AreEqual(string.Empty, problems.ToString());
        }

        private static void Report(StringBuilder problems, Type type, string what, IEnumerable<string> columns)
        {
            var list = columns.OrderBy(c => c).ToList();
            if (list.Count > 0)
            {
                problems.AppendLine($"{type.Name}: {what}: {string.Join(", ", list)}");
            }
        }
    }
}
