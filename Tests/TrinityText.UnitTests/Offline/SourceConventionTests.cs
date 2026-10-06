using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace TrinityText.UnitTests.Offline
{
    /// <summary>
    /// Mechanical conventions of CLAUDE.md checked on the sources of Core/Business (no reviewer needed).
    /// </summary>
    [TestClass]
    [TestCategory("Offline")]
    public class SourceConventionTests
    {
        private static string BusinessRoot()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "Trinity.sln")))
            {
                dir = dir.Parent;
            }

            Assert.IsNotNull(dir, "Trinity.sln not found above " + AppContext.BaseDirectory);
            return Path.Combine(dir.FullName, "Core", "TrinityText.Business");
        }

        private static IEnumerable<string> Violations(string pattern)
        {
            var regex = new Regex(pattern, RegexOptions.Compiled);
            foreach (var file in Directory.EnumerateFiles(BusinessRoot(), "*.cs", SearchOption.AllDirectories))
            {
                if (file.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar) ||
                    file.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar))
                {
                    continue;
                }

                var lines = File.ReadAllLines(file);
                for (int i = 0; i < lines.Length; i++)
                {
                    var line = lines[i].TrimStart();
                    if (line.StartsWith("//") || line.StartsWith("*") || line.StartsWith("///"))
                    {
                        continue;
                    }

                    if (regex.IsMatch(line))
                    {
                        yield return $"{Path.GetFileName(file)}:{i + 1}: {line.Trim()}";
                    }
                }
            }
        }

        [TestMethod]
        public void NoRuntimeIOrderedQueryableTypeChecks()
        {
            // EF / NH root queryables already implement IOrderedQueryable: compare Expression.Type instead (see PaginationExtensions.Sort)
            var found = Violations(@"\bis\s+IOrderedQueryable|\bas\s+IOrderedQueryable").ToList();
            Assert.AreEqual(0, found.Count, string.Join(Environment.NewLine, found));
        }

        [TestMethod]
        public void NoSyncToListWrappedInTaskFromResult()
        {
            var found = Violations(@"Task\.FromResult\(.*\.ToList\(").ToList();
            Assert.AreEqual(0, found.Count, string.Join(Environment.NewLine, found));
        }

        [TestMethod]
        public void BusinessDoesNotReferenceProviderTypes()
        {
            var found = Violations(@"^\s*using\s+(Microsoft\.EntityFrameworkCore|NHibernate)\b").ToList();
            Assert.AreEqual(0, found.Count, string.Join(Environment.NewLine, found));
        }
    }
}
