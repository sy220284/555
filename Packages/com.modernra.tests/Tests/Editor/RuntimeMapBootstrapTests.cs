using ModernRA.Rules;
using NUnit.Framework;

namespace ModernRA.Tests
{
    public sealed class RuntimeMapBootstrapTests
    {
        [Test]
        public void GrayRangeGeneratedDataContainsAuthoritativeGameplayAnchors()
        {
            RuntimeMapBootstrapData map = GrayRangeGeneratedData.Create();
            Assert.That(map.MapId, Is.EqualTo("MAP_GRAY_RANGE"));
            Assert.That(map.SizeMeters.X, Is.EqualTo(8000));
            Assert.That(map.SizeMeters.Y, Is.EqualTo(8000));
            Assert.That(map.Spawns.Length, Is.EqualTo(2));
            Assert.That(map.Resources.Length, Is.EqualTo(8));
            Assert.That(map.StrategicSites.Length, Is.EqualTo(3));
            Assert.That(map.Roads.Length, Is.EqualTo(3));
            Assert.That(map.ControlRegions.Length, Is.EqualTo(5));
            Assert.That(map.BuildableAreas.Length, Is.EqualTo(2));
        }

        [Test]
        public void StandardAnnihilationConfigUsesGeneratedMapData()
        {
            RuntimeMapBootstrapData map = GrayRangeGeneratedData.Create();
            AnnihilationPrototypeConfig config = map.CreateStandardAnnihilationConfig();
            Assert.That(config.SpawnA.X, Is.EqualTo(1200));
            Assert.That(config.SpawnA.Y, Is.EqualTo(1200));
            Assert.That(config.SpawnB.X, Is.EqualTo(6800));
            Assert.That(config.SpawnB.Y, Is.EqualTo(6800));
            Assert.That(config.SharedCorridor.Length, Is.EqualTo(5));
            Assert.That(config.StartingIndustrialMilli, Is.EqualTo(12000L * 1000L));
        }

        [Test]
        public void GeneratedSourceFingerprintIsPresent()
        {
            Assert.That(GrayRangeGeneratedData.SourceMapSha256, Has.Length.EqualTo(64));
            Assert.That(GrayRangeGeneratedData.GeneratorVersion, Is.EqualTo(1));
        }
    }
}
