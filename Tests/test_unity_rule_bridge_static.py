import pathlib
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[1]
SIM = ROOT / "Packages/com.modernra.simulation/Runtime"


class UnityRuleBridgeStaticTests(unittest.TestCase):
    def test_bridge_calls_authoritative_live_session_step(self):
        text = (SIM / "AnnihilationRuleBridgeSystem.cs").read_text(encoding="utf-8")
        self.assertIn("LiveCommandedAnnihilationSession", text)
        self.assertIn("_session.Step();", text)
        self.assertIn("GrayRangeGeneratedData.Create()", text)
        self.assertNotIn("TankCost", text)
        self.assertNotIn("RawDamage", text)
        self.assertNotIn("MiningPerTick", text)

    def test_bridge_admits_player_commands_before_authoritative_step(self):
        text = (SIM / "AnnihilationRuleBridgeSystem.cs").read_text(encoding="utf-8")
        self.assertIn("PlayerCommandRequest", text)
        self.assertIn("_session.Submit(command)", text)
        self.assertIn("executeTick", text)
        self.assertIn("pending.Clear()", text)

    def test_bridge_mirrors_buildings_units_and_match_state(self):
        text = (SIM / "AnnihilationRuleBridgeSystem.cs").read_text(encoding="utf-8")
        self.assertIn("PrototypeBuildingState", text)
        self.assertIn("PrototypeCombatUnitState", text)
        self.assertIn("AnnihilationMatchState", text)
        self.assertIn("ComputeStateHash", text)

    def test_map_bootstrap_materializes_all_major_graybox_categories(self):
        text = (SIM / "GrayRangeBootstrapSystem.cs").read_text(encoding="utf-8")
        for name in (
            "CreateSpawnAnchors",
            "CreateResourceAnchors",
            "CreateStrategicSiteAnchors",
            "CreateRoadPoints",
            "CreateControlRegionVertices",
            "CreateBuildableVertices",
        ):
            self.assertIn(name, text)

    def test_bridge_mirrors_authoritative_supply_state(self):
        text = (SIM / "AnnihilationRuleBridgeSystem.cs").read_text(encoding="utf-8")
        self.assertIn("AddComponent<SupplyState>", text)
        self.assertIn("new SupplyState { Level = (byte)unit.SupplyLevel }", text)

    def test_runtime_state_blocks_rule_bridge_until_map_exists(self):
        text = (SIM / "AnnihilationRuleBridgeSystem.cs").read_text(encoding="utf-8")
        self.assertIn("RequireForUpdate<GrayRangeRuntimeState>()", text)
        self.assertIn("UpdateAfter(typeof(SimulationTickSystem))", text)


if __name__ == "__main__":
    unittest.main()
