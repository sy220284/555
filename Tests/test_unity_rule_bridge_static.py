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

    def test_hot_mirror_state_is_applied_by_burst_batch_job(self):
        text = (SIM / "AnnihilationRuleBridgeSystem.cs").read_text(encoding="utf-8")
        self.assertIn("[BurstCompile]", text)
        self.assertIn("ApplyRuleMirrorSnapshotJob : IJobEntity", text)
        self.assertIn("NativeParallelHashMap<int, RuleMirrorSnapshot>", text)
        self.assertIn("ScheduleParallel(Dependency)", text)
        self.assertIn("Allocator.Persistent", text)
        self.assertIn("_mirrorSnapshots.Clear()", text)
        self.assertIn("_mirrorSnapshots.Capacity = requiredCapacity", text)
        self.assertIn("_mirrorSnapshots.Dispose()", text)
        self.assertNotIn("Allocator.TempJob", text)
        self.assertNotIn("SetComponentData(entity, new SimPosition", text)
        self.assertNotIn("SetComponentData(entity, new HealthState", text)

    def test_rule_entities_are_disabled_and_reused_instead_of_destroyed(self):
        bridge = (SIM / "AnnihilationRuleBridgeSystem.cs").read_text(encoding="utf-8")
        components = (SIM / "GrayRangeRuntimeComponents.cs").read_text(encoding="utf-8")
        presentation = (ROOT / "Packages/com.modernra.presentation/Runtime/GrayboxPlayableController.cs").read_text(encoding="utf-8")
        self.assertIn("AnnihilationRuleActive : IComponentData, IEnableableComponent", components)
        self.assertIn("Stack<Entity> _entityPool", bridge)
        self.assertIn("SetComponentEnabled<AnnihilationRuleActive>(entity, false)", bridge)
        self.assertIn("SetComponentEnabled<AnnihilationRuleActive>(entity, true)", bridge)
        self.assertNotIn("EntityManager.DestroyEntity(entity)", bridge)
        self.assertIn("ComponentType.ReadOnly<AnnihilationRuleActive>()", presentation)

    def test_graybox_controls_support_box_selection_and_authoritative_groups(self):
        text = (ROOT / "Packages/com.modernra.presentation/Runtime/GrayboxPlayableController.cs").read_text(encoding="utf-8")
        self.assertIn("HashSet<int> _selectedUnitIds", text)
        self.assertIn("ScreenRect(_selectionStart, end)", text)
        self.assertIn("selection.Contains", text)
        self.assertIn("PrototypeControlGroupPayload.EncodeUnitGroup", text)
        self.assertIn("PrototypeControlGroupPayload.EncodeGroupWaypoint", text)
        self.assertIn("PrototypePlayerCommandKind.HoldControlGroup", text)
        self.assertIn("_orderedSelection.Sort()", text)
        self.assertNotIn("EntityQuery queueQuery = entityManager.CreateEntityQuery", text)

    def test_graybox_markers_are_recycled_by_entity_kind(self):
        text = (ROOT / "Packages/com.modernra.presentation/Runtime/GrayboxPlayableController.cs").read_text(encoding="utf-8")
        self.assertIn("Stack<GameObject> _buildingMarkerPool", text)
        self.assertIn("Stack<GameObject> _unitMarkerPool", text)
        self.assertIn("RecycleMarker(marker, kind)", text)
        self.assertIn("pool.Count > 0", text)
        self.assertIn("marker.SetActive(false)", text)
        self.assertNotIn("Destroy(marker);\n                _markers.Remove(id)", text)

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

    def test_runtime_state_blocks_rule_bridge_until_map_exists(self):
        text = (SIM / "AnnihilationRuleBridgeSystem.cs").read_text(encoding="utf-8")
        self.assertIn("RequireForUpdate<GrayRangeRuntimeState>()", text)
        self.assertIn("UpdateAfter(typeof(SimulationTickSystem))", text)


if __name__ == "__main__":
    unittest.main()
