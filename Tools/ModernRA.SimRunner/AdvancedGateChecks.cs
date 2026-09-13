using ModernRA.Rules;

internal static class AdvancedGateChecks
{
    public static void Run()
    {
        VerifyIntelPrecisionAndHiddenState();
        VerifyAIAuthorityAndPlayerOverride();
        VerifyRoboticsDegradationMatrix();
        VerifyLocalAuthoritativeNetworkLoop();
        SupplyGateChecks.Run();
    }

    private static void VerifyIntelPrecisionAndHiddenState()
    {
        Check(!IntelReplicationRules.TryBuildVisibleState(1, 1234, 876, 7, 830, RuleIntelLevel.Unknown, out _), "unknown enemy leaked into snapshot");
        Check(IntelReplicationRules.TryBuildVisibleState(1, 1234, 876, 7, 830, RuleIntelLevel.Detected, out VisibleEntityState detected), "detected contact missing");
        Check(IntelReplicationRules.TryBuildVisibleState(1, 1234, 876, 7, 830, RuleIntelLevel.Classified, out VisibleEntityState classified), "classified contact missing");
        Check(IntelReplicationRules.TryBuildVisibleState(1, 1234, 876, 7, 830, RuleIntelLevel.Confirmed, out VisibleEntityState confirmed), "confirmed contact missing");
        Check(IntelReplicationRules.TryBuildVisibleState(1, 1234, 876, 7, 830, RuleIntelLevel.Tracked, out VisibleEntityState tracked), "tracked contact missing");
        Check(detected.Detail == RuleReplicationDetail.Contact && detected.UnitClass == -1 && detected.Health == -1, "detected precision leaked class or health");
        Check(classified.Detail == RuleReplicationDetail.Class && classified.UnitClass == 7 && classified.Health == -1, "classified precision invalid");
        Check(confirmed.Detail == RuleReplicationDetail.Confirmed && confirmed.Health >= 0 && confirmed.Health != 830, "confirmed health must be bucketed");
        Check(tracked.Detail == RuleReplicationDetail.Full && tracked.X == 1234 && tracked.Y == 876 && tracked.Health == 830, "tracked precision must be exact");
        Check(detected.X != classified.X || detected.Y != classified.Y, "detected and classified precision collapsed to same view");
        Check(classified.X != confirmed.X || classified.Y != confirmed.Y, "classified and confirmed precision collapsed to same view");
    }

    private static void VerifyAIAuthorityAndPlayerOverride()
    {
        var authority = new RuleAIAuthority(
            RuleAIAuthorityLevel.BattleGroup,
            1,
            42,
            RuleAIForbiddenAction.StrategicWeapon | RuleAIForbiddenAction.ChangeMainTech,
            5);

        Check(AIAuthorityRules.IsAllowed(authority, new RuleAIOrder(1, 42, RuleAIAction.Move, RuleAIAuthorityLevel.Tactical, 5)), "legal AI move rejected");
        Check(!AIAuthorityRules.IsAllowed(authority, new RuleAIOrder(1, 42, RuleAIAction.StrategicWeapon, RuleAIAuthorityLevel.Tactical, 5)), "forbidden strategic weapon allowed");
        Check(!AIAuthorityRules.IsAllowed(authority, new RuleAIOrder(1, 99, RuleAIAction.Move, RuleAIAuthorityLevel.Tactical, 5)), "AI escaped authorized region");
        Check(!AIAuthorityRules.IsAllowed(authority, new RuleAIOrder(1, 42, RuleAIAction.Move, RuleAIAuthorityLevel.Tactical, 4)), "stale AI order survived player override");
        Check(!AIAuthorityRules.IsAllowed(authority, new RuleAIOrder(2, 42, RuleAIAction.Move, RuleAIAuthorityLevel.Tactical, 5)), "AI crossed player ownership boundary");
    }

    private static void VerifyRoboticsDegradationMatrix()
    {
        RobotFallbackMode[] lostExpected =
        {
            RobotFallbackMode.SafeStopOrReturn,
            RobotFallbackMode.LocalNavigate,
            RobotFallbackMode.ContinueMission,
            RobotFallbackMode.LocalCoordinate
        };

        for (int level = 1; level <= 4; level++)
        {
            var configured = (RuleAutonomyLevel)level;
            RobotRuntimeDecision lost = RoboticsDegradationRules.Evaluate(configured, RuleLinkState.Lost, 100, 100, RuleEWLevel.Normal);
            Check(lost.Fallback == lostExpected[level - 1], $"A{level} lost-link fallback invalid");
            RobotRuntimeDecision blackout = RoboticsDegradationRules.Evaluate(configured, RuleLinkState.Connected, 100, 100, RuleEWLevel.Blackout);
            Check(blackout.EffectiveLink == RuleLinkState.Lost, $"A{level} blackout did not sever effective link");
            RobotRuntimeDecision starved = RoboticsDegradationRules.Evaluate(configured, RuleLinkState.Connected, 20, 100, RuleEWLevel.Normal);
            Check((int)starved.EffectiveAutonomy >= 1 && (int)starved.EffectiveAutonomy <= level, $"A{level} compute degradation out of bounds");
        }
    }

    private static void VerifyLocalAuthoritativeNetworkLoop()
    {
        const ulong contentHash = 0xABCDEF1234567890UL;
        var server = new LocalAuthoritativeServer(contentHash, new[]
        {
            new ServerEntityState { EntityId = 10, TeamId = 1, X = 0, Y = 0, UnitClass = 1, Health = 1000 },
            new ServerEntityState { EntityId = 20, TeamId = 2, X = 1234, Y = 876, UnitClass = 2, Health = 900 }
        });

        Check(!server.TryConnect(contentHash + 1), "content hash mismatch was accepted");
        Check(server.TryConnect(contentHash), "matching content hash was rejected");
        server.AdvanceOneTick();
        AuthoritativeSnapshot initial = server.BuildSnapshot(1, 0);
        var client = new LocalClientReplica();
        client.Apply(initial);
        int ownContact = 10 * 31 + 1;
        int enemyContact = 20 * 31 + 1;
        Check(client.ContainsContact(ownContact), "own entity missing from full snapshot");
        Check(!client.ContainsContact(enemyContact), "hidden enemy leaked into full snapshot");
        Check(!server.SubmitIntent(new ClientCommandIntent(1, 20, 1, 0, 0)), "client commanded enemy authoritative entity");
        Check(server.SubmitIntent(new ClientCommandIntent(1, 10, 1, 100, 0)), "legal movement intent rejected");
        Check(!server.SubmitIntent(new ClientCommandIntent(1, 10, 1, 200, 0)), "replayed command sequence accepted");

        var jammedSensor = new RuleSensorCoverageSource(100, 1, 0, 0, 2000, 1000, 0,
            new[] { new RuleEWInterferenceSource(200, 1000, 1000) });
        Check(RuleSensorDetectionRules.EffectiveRange(jammedSensor) == 300,
            "EW quality did not reduce authoritative sensor range to the 0.15 floor");
        var clearSensor = new RuleSensorCoverageSource(100, 1, 0, 0, 2000, 1000, 0,
            Array.Empty<RuleEWInterferenceSource>());
        Check(server.RefreshSensorIntelIfDue(1, new[] { clearSensor }),
            "authoritative sensor scan missed its deterministic 15Hz lane");
        server.AdvanceOneTick();
        AuthoritativeSnapshot delta = server.BuildSnapshot(1, initial.Tick);
        Check(delta.IsDelta && delta.BaselineTick == initial.Tick, "incremental snapshot baseline invalid");
        client.Apply(delta);
        Check(client.ContainsContact(enemyContact), "detected enemy missing from filtered delta");
        Check(client.TryGet(enemyContact, out VisibleEntityState detectedEnemy) && detectedEnemy.Detail == RuleReplicationDetail.Contact, "detected enemy received excess precision");
        Check(client.TryInterpolate(ownContact, 500, out Int2 interpolated) && interpolated.X == 5, "client interpolation did not blend authoritative snapshots");

        server.SetIntel(1, 20, RuleIntelLevel.Tracked);
        server.AdvanceOneTick();
        AuthoritativeSnapshot trackedDelta = server.BuildSnapshot(1, delta.Tick);
        client.Apply(trackedDelta);
        Check(client.TryGet(enemyContact, out VisibleEntityState trackedEnemy) && trackedEnemy.Detail == RuleReplicationDetail.Full && trackedEnemy.X == 1234 && trackedEnemy.Y == 876, "tracked enemy did not receive exact authorized state");
        Check(trackedDelta.ContentHash == contentHash, "snapshot content hash changed");

        server.AdvanceOneTick();
        AuthoritativeSnapshot skippedDelta = server.BuildSnapshot(1, trackedDelta.Tick);
        server.AdvanceOneTick();
        AuthoritativeSnapshot outOfSequenceDelta = server.BuildSnapshot(1, skippedDelta.Tick);
        bool rejectedGap = false;
        try
        {
            client.Apply(outOfSequenceDelta);
        }
        catch (InvalidOperationException)
        {
            rejectedGap = true;
        }
        Check(rejectedGap && client.NeedsReconnect && client.LastTick == trackedDelta.Tick,
            "client accepted an incremental snapshot after a missing baseline");
        AuthoritativeSnapshot reconnect = server.BuildReconnectSnapshot(1);
        client.Apply(reconnect);
        Check(!client.NeedsReconnect && client.LastTick == server.Tick && client.ContainsContact(enemyContact),
            "full authorized snapshot did not recover the disconnected client");
        var wrongContentClient = new LocalClientReplica();
        wrongContentClient.Apply(initial);
        bool rejectedContentChange = false;
        try
        {
            wrongContentClient.Apply(new AuthoritativeSnapshot
            {
                Tick = initial.Tick + 1,
                BaselineTick = initial.Tick,
                IsDelta = true,
                ContentHash = contentHash + 1
            });
        }
        catch (InvalidOperationException)
        {
            rejectedContentChange = true;
        }
        Check(rejectedContentChange, "client accepted a snapshot from different content");

        var intelAdapter = new PrototypeBattleGroupIntelAdapter(
            new[] { new Int2(0, 0), new Int2(1000, 1000), new Int2(1500, 1000) },
            new[] { new PrototypeBattleGroupTargetProfile(2, 700, 800, 500, 600, 900) });
        intelAdapter.Apply(initial);
        intelAdapter.Apply(delta);
        intelAdapter.Apply(trackedDelta);
        PrototypeBattleGroupTarget[] aiTargets = intelAdapter.BuildTargets();
        Check(aiTargets.Length == 1 && aiTargets[0].TargetId == enemyContact && aiTargets[0].Intel == RuleIntelLevel.Tracked,
            "battle-group AI adapter did not consume the filtered enemy snapshot");

        server.SetIntel(1, 20, RuleIntelLevel.Unknown);
        Check(server.RefreshSensorIntelIfDue(1, Array.Empty<RuleSensorCoverageSource>()),
            "sensor coverage removal missed its deterministic 15Hz lane");
        server.AdvanceOneTick();
        AuthoritativeSnapshot hiddenDelta = server.BuildSnapshot(1, reconnect.Tick);
        Check(hiddenDelta.RemovedContactIds.Length == 1 && hiddenDelta.RemovedContactIds[0] == enemyContact,
            "lost intelligence did not emit a contact tombstone");
        client.Apply(hiddenDelta);
        intelAdapter.Apply(hiddenDelta);
        Check(!client.ContainsContact(enemyContact), "client retained a hidden enemy after the tombstone");
        Check(intelAdapter.BuildTargets().Length == 0, "battle-group AI retained a hidden target after the tombstone");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
