using Microsoft.VisualStudio.TestTools.UnitTesting;
using Resulz;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TrinityText.Business;

namespace TrinityText.UnitTests.Offline
{
    [TestClass]
    [TestCategory("Offline")]
    public class WidgetUtilitiesTests
    {
        private sealed class Harness
        {
            public Dictionary<string, string> Widgets { get; } = new();

            public int WidgetCalls { get; private set; }

            public bool FailLookups { get; set; }

            public WidgetUtilities Utilities { get; }

            public Harness()
            {
                var widgetService = Fake.Of<IWidgetService>((m, a) =>
                {
                    if (m.Name != nameof(IWidgetService.GetByKeys))
                    {
                        throw new NotImplementedException(m.Name);
                    }

                    WidgetCalls++;
                    if (FailLookups)
                    {
                        return Task.FromResult(OperationResult<WidgetDTO>.MakeFailure([ErrorMessage.Create("GET_BYKEYS", "GENERIC_ERROR")]));
                    }

                    return Task.FromResult(Widgets.TryGetValue((string)a[0], out var content)
                        ? OperationResult<WidgetDTO>.MakeSuccess(new WidgetDTO { Key = (string)a[0], Content = content })
                        : OperationResult<WidgetDTO>.MakeFailure([ErrorMessage.Create("GET_BYKEYS", "NOT_FOUND")]));
                });

                var fileManager = Fake.Of<IFileManagerService>((m, a) => throw new NotImplementedException(m.Name));

                Utilities = new WidgetUtilities(fileManager, widgetService);
            }
        }

        [TestMethod]
        public async Task Widget_IsExpanded_AndPlaceholdersReplaced()
        {
            var h = new Harness();
            h.Widgets["HELLO"] = "hello @[WEBSITE]/@[LANG]";

            var result = await h.Utilities.Replace("T", "W", "S", "it", "<a>@[WIDGET(HELLO)]</a>");

            Assert.AreEqual("<a>hello W/it</a>", result);
        }

        [TestMethod]
        public async Task UnknownWidget_KeepsItsKey()
        {
            var h = new Harness();

            var result = await h.Utilities.ReplaceWidget("x @[WIDGET(MISSING)] y", "S", "W", "T", "it");

            Assert.AreEqual("x MISSING y", result);
        }

        [TestMethod]
        public async Task LookupError_IsNotPublishedAsTheKey()
        {
            var h = new Harness { FailLookups = true };

            await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                () => h.Utilities.ReplaceWidget("@[WIDGET(ANY)]", "S", "W", "T", "it"));
        }

        [TestMethod]
        public async Task ExponentialExpansion_IsCapped()
        {
            var h = new Harness();
            h.Widgets["BOMB"] = string.Concat(System.Linq.Enumerable.Repeat("@[WIDGET(BOMB)]", 200));

            await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                () => h.Utilities.ReplaceWidget("@[WIDGET(BOMB)]", "S", "W", "T", "it"));
        }

        [TestMethod]
        public async Task CdataTerminator_InsideWidget_CannotCloseTheSection()
        {
            var h = new Harness();
            h.Widgets["EVIL"] = "a]]>b";

            var result = await h.Utilities.ReplaceWidget("<![CDATA[@[WIDGET(EVIL)]]]>", "S", "W", "T", "it");

            // still well-formed XML with a single text node
            var element = System.Xml.Linq.XElement.Parse("<r>" + result + "</r>");
            Assert.AreEqual("a]]>b", element.Value);
        }

        [TestMethod]
        public async Task SharedCache_ResolvesEachWidgetOnce()
        {
            var h = new Harness();
            h.Widgets["W1"] = "one";
            var cache = new WidgetResolutionCache();

            await h.Utilities.Replace("T", "W", "S", "it", "@[WIDGET(W1)]", cache);
            await h.Utilities.Replace("T", "W", "S", "it", "again @[WIDGET(W1)]", cache);
            await h.Utilities.Replace("T", "W", "S", "en", "@[WIDGET(W1)]", cache);

            // "it" once (2 pages), "en" once: the cache key includes the language
            Assert.AreEqual(2, h.WidgetCalls);
        }
    }
}
