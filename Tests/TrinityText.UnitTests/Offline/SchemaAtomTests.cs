using Microsoft.VisualStudio.TestTools.UnitTesting;
using TrinityText.Business.Schema;

namespace TrinityText.UnitTests.Offline
{
    [TestClass]
    [TestCategory("Offline")]
    public class SchemaAtomTests
    {
        [TestMethod]
        public void OptionalNumber_CanBeEmpty()
            => Assert.IsTrue(new NumberAtom { IsRequired = false, Value = "" }.Validate("Age", "atom").Success);

        [TestMethod]
        public void RequiredNumber_CannotBeEmpty()
            => Assert.IsFalse(new NumberAtom { IsRequired = true, Value = "" }.Validate("Age", "atom").Success);

        [TestMethod]
        public void Number_InvalidFormatAndRange()
        {
            Assert.IsFalse(new NumberAtom { Value = "abc" }.Validate("Age", "atom").Success);
            Assert.IsFalse(new NumberAtom { Value = "3", MinValue = 5 }.Validate("Age", "atom").Success);
            Assert.IsFalse(new NumberAtom { Value = "30", MaxValue = 5 }.Validate("Age", "atom").Success);
            Assert.IsTrue(new NumberAtom { Value = "7", MinValue = 5, MaxValue = 10 }.Validate("Age", "atom").Success);
        }

        [TestMethod]
        public void OptionalDate_CanBeEmpty()
            => Assert.IsTrue(new DateTimeAtom { IsRequired = false, Value = "" }.Validate("When", "atom").Success);

        [TestMethod]
        public void Date_MustMatchFormat()
        {
            Assert.IsFalse(new DateTimeAtom { IsRequired = true, Value = "" }.Validate("When", "atom").Success);
            Assert.IsFalse(new DateTimeAtom { Value = "2026-01-31" }.Validate("When", "atom").Success);
            Assert.IsTrue(new DateTimeAtom { Value = "31/01/2026 10:30:00" }.Validate("When", "atom").Success);
        }
    }
}
