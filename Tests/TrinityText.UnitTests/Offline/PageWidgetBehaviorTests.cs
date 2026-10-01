using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TrinityText.Business;
using TrinityText.Business.Services.Impl;
using TrinityText.Domain;

namespace TrinityText.UnitTests.Offline
{
    /// <summary>Pages and widgets on EF Core and NHibernate (SQLite in memory).</summary>
    [TestClass]
    [TestCategory("Offline")]
    public class PageWidgetBehaviorTests
    {
        public static IEnumerable<object[]> Providers => new[] { new object[] { "EF" }, new object[] { "NH" } };


        private static PageService Pages(ProviderScope scope)
            => new(scope.Repo<Page>(), scope.Repo<PageType>(), NullLogger<PageService>.Instance);

        private static async Task<int> CreatePageType(ProviderFixture db, string name)
        {
            using var scope = db.NewScope();
            return (await scope.Repo<PageType>().Create(new PageType
            {
                NAME = name,
                SCHEMA = "<root id=\"r\"><content id=\"c\"/></root>",
                OUTPUT_FILENAME = name.ToLowerInvariant(),
            })).ID;
        }

        private static PageDTO NewPage(int typeId, string title, string language = "it", string website = "W", string site = "S1")
            => new()
            {
                Title = title,
                Language = language,
                Website = website,
                Site = site,
                PageTypeId = typeId,
                Content = "<page><title><![CDATA[" + title + "]]></title></page>",
                CreationUser = "me",
            };

        [DataTestMethod]
        [DynamicData(nameof(Providers))]
        public async Task Page_Save_Update_Search_Remove(string provider)
        {
            using var db = ProviderFixture.Create(provider);
            var typeId = await CreatePageType(db, "News");

            int id;
            using (var scope = db.NewScope())
            {
                var rs = await Pages(scope).Save(NewPage(typeId, "First"));
                Assert.IsTrue(rs.Success, string.Join(",", rs.Errors?.Select(e => e.Description) ?? []));
                id = rs.Value.Id.Value;
                Assert.AreEqual("News", rs.Value.PageType.Name);
            }

            using (var scope = db.NewScope())
            {
                var dto = (await Pages(scope).Get(id)).Value;
                dto.Title = "Renamed";
                dto.Content = "<page>changed</page>";
                dto.LastUpdateUser = "you";
                var rs = await Pages(scope).Save(dto);
                Assert.IsTrue(rs.Success, string.Join(",", rs.Errors?.Select(e => e.Description) ?? []));
                Assert.AreEqual("Renamed", rs.Value.Title);
                Assert.AreEqual("News", rs.Value.PageType.Name, "the page type is not touched by the update");
            }

            using (var scope = db.NewScope())
            {
                var stored = await scope.Repo<Page>().Read(id);
                Assert.AreEqual("<page>changed</page>", stored.CONTENT);
                Assert.AreEqual(typeId, stored.FK_PAGETYPE);
                Assert.AreEqual("W", stored.FK_WEBSITE);
                Assert.AreEqual("you", stored.LASTUPDATE_USER);
                // the page type row (schema included) was not rewritten by the page update
                Assert.AreEqual("<root id=\"r\"><content id=\"c\"/></root>", (await scope.Repo<PageType>().Read(typeId)).SCHEMA);
            }

            using (var scope = db.NewScope())
            {
                var search = new SearchPageDTO { UserWebsites = ["W"], WebsiteLanguages = ["it"], ExcludeContent = true };
                var rs = await Pages(scope).Search(search, 0, 10);

                Assert.IsTrue(rs.Success, string.Join(",", rs.Errors?.Select(e => e.Description) ?? []));
                var item = rs.Value.Result.Single();
                Assert.AreEqual(1, rs.Value.TotalCount);
                Assert.AreEqual(string.Empty, item.Content, "ExcludeContent");
                Assert.AreEqual("News", item.PageType.Name);

                search.ExcludeContent = false;
                Assert.AreEqual("<page>changed</page>", (await Pages(scope).Search(search, 0, 10)).Value.Result.Single().Content);
            }

            using (var scope = db.NewScope())
            {
                Assert.IsTrue((await Pages(scope).Remove(id)).Success);
                Assert.IsFalse((await Pages(scope).Remove(id)).Success);
            }
        }

        [DataTestMethod]
        [DynamicData(nameof(Providers))]
        public async Task PublishablePages_CarryTheirPageType_AndSeparateSites(string provider)
        {
            using var db = ProviderFixture.Create(provider);
            var typeId = await CreatePageType(db, "News");

            using (var scope = db.NewScope())
            {
                Assert.IsTrue((await Pages(scope).Save(NewPage(typeId, "site one", site: "S1"))).Success);
                Assert.IsTrue((await Pages(scope).Save(NewPage(typeId, "site two", site: "S2"))).Success);
                Assert.IsTrue((await Pages(scope).Save(NewPage(typeId, "english", language: "en", site: "S1"))).Success);
            }

            using (var scope = db.NewScope())
            {
                var rs = await Pages(scope).GetPublishablePagesByWebsite("W", new Dictionary<string, string[]> { ["S1"] = ["it"], ["S2"] = ["it", "en"] });

                Assert.IsTrue(rs.Success, string.Join(",", rs.Errors?.Select(e => e.Description) ?? []));
                CollectionAssert.AreEqual(new[] { "site one" }, rs.Value["S1"].Select(p => p.Title).ToArray());
                CollectionAssert.AreEqual(new[] { "site two" }, rs.Value["S2"].Select(p => p.Title).ToArray());
                Assert.IsTrue(rs.Value["S1"].All(p => p.PageType.Schema.Contains("<root")), "the schema comes with the page type");
            }

            using (var scope = db.NewScope())
            {
                var rs = await Pages(scope).GetPublishablePages("W", "S1", ["it", "en"]);

                Assert.IsTrue(rs.Success);
                Assert.AreEqual(1, rs.Value["it"].Count);
                Assert.AreEqual(1, rs.Value["en"].Count);
                Assert.AreEqual("News", rs.Value["it"][0].PageType.Name);
            }
        }

        [DataTestMethod]
        [DynamicData(nameof(Providers))]
        public async Task Widget_Save_Search_GetByKeys_Remove(string provider)
        {
            using var db = ProviderFixture.Create(provider);

            WidgetService Service(ProviderScope scope) => new(scope.Repo<Widget>(), NullLogger<WidgetService>.Instance);

            int global;
            using (var scope = db.NewScope())
            {
                global = (await Service(scope).Save(new WidgetDTO { Key = "footer", Language = "it", Content = "global", CreationUser = "me" })).Value.Id.Value;
                Assert.IsTrue((await Service(scope).Save(new WidgetDTO { Key = "footer", Language = "it", Website = "W", Content = "website", CreationUser = "me" })).Success);
                Assert.IsTrue((await Service(scope).Save(new WidgetDTO { Key = "footer", Language = "it", Website = "W", Site = "S1", Content = "site", CreationUser = "me" })).Success);
            }

            using (var scope = db.NewScope())
            {
                // same key / scope again
                Assert.IsFalse((await Service(scope).Save(new WidgetDTO { Key = "footer", Language = "it", Website = "W", Content = "dup", CreationUser = "me" })).Success);

                Assert.AreEqual("site", (await Service(scope).GetByKeys("footer", "W", "S1", "it")).Value.Content);
                Assert.AreEqual("website", (await Service(scope).GetByKeys("footer", "W", "S2", "it")).Value.Content);
                Assert.AreEqual("global", (await Service(scope).GetByKeys("footer", "OTHER", "S1", "it")).Value.Content);
                Assert.IsFalse((await Service(scope).GetByKeys("footer", "W", "S1", "en")).Success);
            }

            using (var scope = db.NewScope())
            {
                var dto = (await Service(scope).Get(global)).Value;
                dto.Content = "global v2";
                Assert.IsTrue((await Service(scope).Save(dto)).Success);
                Assert.AreEqual("global v2", (await Service(scope).Get(global)).Value.Content);

                Assert.IsTrue((await Service(scope).Remove(global)).Success);
                Assert.IsFalse((await Service(scope).Remove(global)).Success);
            }
        }
    }
}
