using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using TrinityText.Business;

namespace TrinityText.UnitTests.Offline
{
    [TestClass]
    [TestCategory("Offline")]
    public class PathSafetyTests
    {
        [DataTestMethod]
        [DataRow("images")]
        [DataRow("My Folder")]
        [DataRow("file.name.ext")]
        public void ValidSegments(string value) => Assert.IsTrue(PathSafety.IsValidSegment(value));

        [DataTestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("   ")]
        [DataRow(".")]
        [DataRow("..")]
        [DataRow(" .. ")]
        [DataRow("a/b")]
        [DataRow("a\\b")]
        [DataRow("c:")]
        [DataRow("a:b")]
        public void InvalidSegments(string value) => Assert.IsFalse(PathSafety.IsValidSegment(value));

        [TestMethod]
        public void EnsureWithinRoot_AcceptsChildren_RejectsEscapes()
        {
            var root = Path.Combine(Path.GetTempPath(), "trinity-root");

            Assert.IsNotNull(PathSafety.EnsureWithinRoot(root, Path.Combine(root, "a", "b.txt")));
            Assert.ThrowsException<ArgumentException>(() => PathSafety.EnsureWithinRoot(root, Path.Combine(root, "..", "evil.txt")));
            // same prefix, different directory
            Assert.ThrowsException<ArgumentException>(() => PathSafety.EnsureWithinRoot(root, root + "-other" + Path.DirectorySeparatorChar + "x.txt"));
        }
    }
}
