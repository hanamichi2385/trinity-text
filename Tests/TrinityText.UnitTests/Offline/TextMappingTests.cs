using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Globalization;
using System.Threading.Tasks;
using TrinityText.Business;
using TrinityText.Business.Services.Impl;
using TrinityText.Domain;

namespace TrinityText.UnitTests.Offline
{
    [TestClass]
    [TestCategory("Offline")]
    public class TextMappingTests
    {

        private static TextDTO NewDto() => new()
        {
            Name = "info",
            Language = "it",
            TextTypeId = 3,
            TextType = new TextTypeDTO { Id = 3, Name = "type" },
            TextRevision = new TextRevisionDTO { Content = "content" },
        };

        [TestMethod]
        public void DtoToEntity_LinksTheTypeThroughTheForeignKeyOnly()
        {
            var entity = BusinessMapper.ToEntity(NewDto());

            Assert.AreEqual(3, entity.FK_TEXTTYPE);
            // mapping the nested DTO would make EF try to insert a new TextType
            Assert.IsNull(entity.TEXTTYPE);
        }

        [TestMethod]
        public void Names_AreUpperCased_WithInvariantCulture()
        {
            var previous = CultureInfo.CurrentCulture;
            try
            {
                // "i".ToUpper() is "İ" under tr-TR: the same key would differ between import and save
                CultureInfo.CurrentCulture = new CultureInfo("tr-TR");

                var entity = BusinessMapper.ToEntity(NewDto());

                Assert.AreEqual("INFO", entity.NAME);
            }
            finally
            {
                CultureInfo.CurrentCulture = previous;
            }
        }

        [TestMethod]
        public void LatestRevision_IsTheOneWithTheHighestNumber()
        {
            var entity = new Text
            {
                NAME = "K",
                REVISIONS =
                [
                    new TextRevision { REVISION_NUMBER = 2, CONTENT = "new", CREATION_DATE = new System.DateTime(2026, 1, 1) },
                    new TextRevision { REVISION_NUMBER = 1, CONTENT = "old", CREATION_DATE = new System.DateTime(2026, 6, 1) },
                ],
            };

            var dto = BusinessMapper.ToDto(entity);

            Assert.AreEqual("new", dto.TextRevision.Content);
        }

        [DataTestMethod]
        [DataRow(0)]
        [DataRow(-1)]
        public async Task CleanRevisions_NonPositiveLimit_IsRejected_BeforeTouchingTheDatabase(int keep)
        {
            // would delete EVERY revision of every text
            var service = new TextService(null, null, null, NullLogger<TextService>.Instance);

            var result = await service.CleanRevisions(keep);

            Assert.IsFalse(result.Success);
        }
    }
}
