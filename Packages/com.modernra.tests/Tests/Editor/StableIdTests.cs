using ModernRA.Core;
using NUnit.Framework;

namespace ModernRA.Tests
{
    public sealed class StableIdTests
    {
        [TestCase("UNIT_COMMON_MBT")]
        [TestCase("MODE_CONQUEST")]
        [TestCase("MIN_RARE_EARTHS")]
        [TestCase("MAT_RARE_EARTH_ACCESS")]
        public void ValidIds_AreAccepted(string value) => Assert.That(StableIdRules.IsValid(value), Is.True);

        [TestCase("FACTION_ALLIED")]
        [TestCase("unit_common_mbt")]
        [TestCase("INVALID")]
        public void LegacyOrInvalidIds_AreRejected(string value) => Assert.That(StableIdRules.IsValid(value), Is.False);
    }
}
