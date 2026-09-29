using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using TrinityText.Business;
using TrinityText.Business.Services.Impl;

namespace TrinityText.UnitTests.Offline
{
    /// <summary>Which text wins when the same key exists at several levels (global / website / site / country).</summary>
    [TestClass]
    [TestCategory("Offline")]
    public class TextSelectionTests
    {
        private static TextDTO Text(string country, string website = null, string site = null)
            => new() { Name = "K", Country = country, Website = website, Site = site };

        [TestMethod]
        public void SingleText_IsPublished()
        {
            var output = new List<TextDTO>();
            var only = Text("IT");

            TextService.PickBest([only], "W", "S1", output);

            Assert.AreSame(only, output.Single());
        }

        [TestMethod]
        public void SameCountryTwice_DoesNotThrow_AndKeepsOneText()
        {
            // used to throw InvalidOperationException from Single() and abort the whole publication
            var output = new List<TextDTO>();

            TextService.PickBest([Text("IT"), Text("IT")], "W", "S1", output);

            Assert.AreEqual(1, output.Count);
        }

        [TestMethod]
        public void SameCountry_SiteSpecificTextWins()
        {
            var output = new List<TextDTO>();
            var global1 = Text("IT");
            var global2 = Text("IT");
            var forSite = Text("IT", "W", "S1");

            TextService.AppendByCountry([global1, global2, forSite], ["IT"], "S1", output);

            Assert.AreSame(forSite, output.Single());
        }

        [TestMethod]
        public void SameCountry_WebsiteLevelBeatsGlobal_WhenNoSiteText()
        {
            var output = new List<TextDTO>();
            var global = Text("IT");
            var website = Text("IT", "W");

            TextService.AppendByCountry([global, website], ["IT"], "S1", output);

            Assert.AreSame(website, output.Single());
        }

        [TestMethod]
        public void WebsiteLevelTextsOnly_AreNotDropped()
        {
            // two website-level texts (no site), different countries and no global text: they used to vanish
            var output = new List<TextDTO>();

            TextService.PickBest([Text("IT", "W"), Text("FR", "W")], "W", "S1", output);

            Assert.AreEqual(2, output.Count);
        }

        [TestMethod]
        public void ReduceTexts_PicksOnePerName()
        {
            var texts = new List<TextDTO>
            {
                new() { Name = "A", Website = null },
                new() { Name = "A", Website = "W", Site = "S1" },
                new() { Name = "B", Website = null },
            };

            var reduced = TextService.ReduceTexts(texts, "W", "S1", [null]);

            Assert.AreEqual(2, reduced.Count);
            Assert.AreEqual("S1", reduced.Single(t => t.Name == "A").Site);
        }
    }
}
