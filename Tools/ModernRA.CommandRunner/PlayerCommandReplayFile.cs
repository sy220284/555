using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ModernRA.Rules;

internal sealed class PlayerCommandReplayDocument
{
    public int FormatVersion { get; set; }
    public string GameVersion { get; set; } = string.Empty;
    public string ContentHash { get; set; } = string.Empty;
    public string MapId { get; set; } = string.Empty;
    public DateTimeOffset TimestampUtc { get; set; }
    public ulong DeterminismSeed { get; set; }
    public int ProtocolVersion { get; set; }
    public string ScenarioId { get; set; } = string.Empty;
    public string MapSourceSha256 { get; set; } = string.Empty;
    public string RulesetId { get; set; } = string.Empty;
    public string CommandHash { get; set; } = string.Empty;
    public int WinnerTeamId { get; set; }
    public int ResolvedTick { get; set; }
    public string FinalStateHash { get; set; } = string.Empty;
    public PlayerReplayInitialSnapshot InitialSnapshot { get; set; } = new PlayerReplayInitialSnapshot();
    public PlayerReplayEventRecord[] Events { get; set; } = Array.Empty<PlayerReplayEventRecord>();
}

internal sealed class PlayerCommandReplayV2Document
{
    public int FormatVersion { get; set; }
    public string GameVersion { get; set; } = string.Empty;
    public string ContentHash { get; set; } = string.Empty;
    public string MapId { get; set; } = string.Empty;
    public DateTimeOffset TimestampUtc { get; set; }
    public ulong DeterminismSeed { get; set; }
    public int ProtocolVersion { get; set; }
    public string ScenarioId { get; set; } = string.Empty;
    public string MapSourceSha256 { get; set; } = string.Empty;
    public string RulesetId { get; set; } = string.Empty;
    public string CommandHash { get; set; } = string.Empty;
    public int WinnerTeamId { get; set; }
    public int ResolvedTick { get; set; }
    public string FinalStateHash { get; set; } = string.Empty;
    public PlayerCommandRecord[] Commands { get; set; } = Array.Empty<PlayerCommandRecord>();
    public PlayerCommandCheckpointRecord[] Checkpoints { get; set; } = Array.Empty<PlayerCommandCheckpointRecord>();
}

internal sealed class PlayerCommandReplayV1Document
{
    public int SchemaVersion { get; set; }
    public string ScenarioId { get; set; } = string.Empty;
    public string MapSourceSha256 { get; set; } = string.Empty;
    public string RulesetId { get; set; } = string.Empty;
    public string CommandHash { get; set; } = string.Empty;
    public int WinnerTeamId { get; set; }
    public int ResolvedTick { get; set; }
    public string FinalStateHash { get; set; } = string.Empty;
    public PlayerCommandRecord[] Commands { get; set; } = Array.Empty<PlayerCommandRecord>();
}

internal sealed class PlayerReplayInitialSnapshot
{
    public int Tick { get; set; }
    public string StateHash { get; set; } = string.Empty;
    public int SpawnAX { get; set; }
    public int SpawnAY { get; set; }
    public int SpawnBX { get; set; }
    public int SpawnBY { get; set; }
    public long StartingIndustrialMilli { get; set; }
    public int MaxLiveTanksPerTeam { get; set; }
    public PlayerReplayPoint[] SharedCorridor { get; set; } = Array.Empty<PlayerReplayPoint>();
}

internal sealed class PlayerReplayPoint
{
    public int X { get; set; }
    public int Y { get; set; }
}

internal sealed class PlayerReplayEventRecord
{
    public string Kind { get; set; } = string.Empty;
    public int Tick { get; set; }
    public int Sequence { get; set; }
    public int PlayerId { get; set; }
    public int CommandKind { get; set; }
    public int IntValue { get; set; }
    public string StateHash { get; set; } = string.Empty;
}

internal sealed class PlayerCommandRecord
{
    public int Tick { get; set; }
    public int Sequence { get; set; }
    public int PlayerId { get; set; }
    public int Kind { get; set; }
    public int IntValue { get; set; }
}

internal sealed class PlayerCommandCheckpointRecord
{
    public int Tick { get; set; }
    public string StateHash { get; set; } = string.Empty;
}

internal static class PlayerCommandReplayFile
{
    public const int FormatVersion = 3;
    public const string GameVersion = "0.1.0-implementation-bootstrap";
    public const int ProtocolVersion = 1;
    public const string RulesetId = "RULESET_ANNIHILATION_STANDARD";
    private const string PlayerCommandEvent = "player_command";
    private const string StateKeyframeEvent = "state_keyframe";
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = false
    };

    public static PlayerCommandReplayDocument Create(
        string scenarioId,
        AnnihilationPrototypeConfig config,
        PrototypePlayerCommand[] canonicalCommands,
        ulong commandHash,
        int winnerTeamId,
        int resolvedTick,
        ulong finalStateHash,
        PlayerCommandCheckpointRecord[] checkpoints)
    {
        if (string.IsNullOrWhiteSpace(scenarioId))
            throw new ArgumentException("scenario id is required", nameof(scenarioId));
        if (config == null)
            throw new ArgumentNullException(nameof(config));
        if (canonicalCommands == null)
            throw new ArgumentNullException(nameof(canonicalCommands));
        if (checkpoints == null)
            throw new ArgumentNullException(nameof(checkpoints));

        var document = new PlayerCommandReplayDocument
        {
            FormatVersion = FormatVersion,
            GameVersion = GameVersion,
            ContentHash = GrayRangeGeneratedData.SourceMapSha256,
            MapId = scenarioId,
            TimestampUtc = DateTimeOffset.UtcNow,
            DeterminismSeed = 0,
            ProtocolVersion = ProtocolVersion,
            ScenarioId = scenarioId,
            MapSourceSha256 = GrayRangeGeneratedData.SourceMapSha256,
            RulesetId = RulesetId,
            CommandHash = commandHash.ToString("X16"),
            WinnerTeamId = winnerTeamId,
            ResolvedTick = resolvedTick,
            FinalStateHash = finalStateHash.ToString("X16"),
            InitialSnapshot = CreateInitialSnapshot(config),
            Events = CreateEvents(canonicalCommands, checkpoints)
        };
        Validate(document);
        return document;
    }

    public static byte[] Serialize(PlayerCommandReplayDocument document)
    {
        Validate(document);
        return Encoding.UTF8.GetBytes(JsonSerializer.Serialize(document, JsonOptions) + "\n");
    }

    public static void Write(string path, PlayerCommandReplayDocument document)
    {
        File.WriteAllBytes(path, Serialize(document));
    }

    public static PlayerCommandReplayDocument Read(string path)
    {
        byte[] bytes = File.ReadAllBytes(path);
        PlayerCommandReplayDocument? document;
        using (JsonDocument json = JsonDocument.Parse(bytes))
        {
            JsonElement root = json.RootElement;
            if (root.TryGetProperty("schema_version", out JsonElement legacyVersion))
            {
                if (legacyVersion.GetInt32() != 1)
                    throw new InvalidDataException($"unsupported command replay schema {legacyVersion.GetInt32()}");
                PlayerCommandReplayV1Document? legacy = JsonSerializer.Deserialize<PlayerCommandReplayV1Document>(bytes, JsonOptions);
                document = legacy == null ? null : MigrateV1(legacy);
            }
            else
            {
                int version = root.GetProperty("format_version").GetInt32();
                if (version == 2)
                {
                    PlayerCommandReplayV2Document? legacy = JsonSerializer.Deserialize<PlayerCommandReplayV2Document>(bytes, JsonOptions);
                    document = legacy == null ? null : MigrateV2(legacy);
                }
                else if (version == FormatVersion)
                {
                    document = JsonSerializer.Deserialize<PlayerCommandReplayDocument>(bytes, JsonOptions);
                }
                else
                {
                    throw new InvalidDataException($"unsupported command replay format {version}");
                }
            }
        }
        if (document == null)
            throw new InvalidDataException("command replay deserialized to null");
        Validate(document);
        return document;
    }

    public static AnnihilationPrototypeConfig ToInitialConfig(PlayerCommandReplayDocument document)
    {
        Validate(document);
        return CreateConfig(document.InitialSnapshot);
    }

    public static PrototypePlayerCommand[] ToCommands(PlayerCommandReplayDocument document)
    {
        Validate(document);
        var records = new List<PrototypePlayerCommand>();
        for (int i = 0; i < document.Events.Length; i++)
        {
            PlayerReplayEventRecord item = document.Events[i];
            if (!string.Equals(item.Kind, PlayerCommandEvent, StringComparison.Ordinal))
                continue;
            records.Add(new PrototypePlayerCommand(
                item.Tick,
                item.Sequence,
                item.PlayerId,
                (PrototypePlayerCommandKind)item.CommandKind,
                item.IntValue));
        }
        var timeline = new DeterministicCommandTimeline(records);
        ulong actualHash = timeline.ComputeCanonicalHash();
        if (!string.Equals(actualHash.ToString("X16"), document.CommandHash, StringComparison.Ordinal))
            throw new InvalidDataException("command replay canonical hash mismatch");
        return timeline.ToCanonicalArray();
    }

    public static PlayerCommandCheckpointRecord[] ToCheckpoints(PlayerCommandReplayDocument document)
    {
        Validate(document);
        var records = new List<PlayerCommandCheckpointRecord>();
        for (int i = 0; i < document.Events.Length; i++)
        {
            PlayerReplayEventRecord item = document.Events[i];
            if (string.Equals(item.Kind, StateKeyframeEvent, StringComparison.Ordinal))
                records.Add(new PlayerCommandCheckpointRecord { Tick = item.Tick, StateHash = item.StateHash });
        }
        return records.ToArray();
    }

    public static void ValidateScenario(PlayerCommandReplayDocument document, string expectedScenarioId)
    {
        Validate(document);
        if (string.IsNullOrWhiteSpace(expectedScenarioId))
            throw new ArgumentException("expected scenario id is required", nameof(expectedScenarioId));
        if (!string.Equals(document.ScenarioId, expectedScenarioId, StringComparison.Ordinal))
            throw new InvalidDataException("command replay scenario id mismatch");
    }

    public static void ValidateOutcome(PlayerCommandReplayDocument document, int winnerTeamId, int resolvedTick, ulong finalStateHash)
    {
        Validate(document);
        if (document.WinnerTeamId != winnerTeamId || document.ResolvedTick != resolvedTick ||
            !string.Equals(document.FinalStateHash, finalStateHash.ToString("X16"), StringComparison.Ordinal))
        {
            throw new InvalidDataException("command replay recorded outcome mismatch");
        }
    }

    public static string Sha256Hex(byte[] bytes)
    {
        return Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    }

    private static PlayerReplayInitialSnapshot CreateInitialSnapshot(AnnihilationPrototypeConfig config)
    {
        var corridor = new PlayerReplayPoint[config.SharedCorridor.Length];
        for (int i = 0; i < corridor.Length; i++)
            corridor[i] = new PlayerReplayPoint { X = config.SharedCorridor[i].X, Y = config.SharedCorridor[i].Y };
        AnnihilationPrototypeWorld initial = AnnihilationPrototype.Create(config);
        return new PlayerReplayInitialSnapshot
        {
            Tick = initial.Tick,
            StateHash = AnnihilationPrototype.ComputeStateHash(initial).ToString("X16"),
            SpawnAX = config.SpawnA.X,
            SpawnAY = config.SpawnA.Y,
            SpawnBX = config.SpawnB.X,
            SpawnBY = config.SpawnB.Y,
            StartingIndustrialMilli = config.StartingIndustrialMilli,
            MaxLiveTanksPerTeam = config.MaxLiveTanksPerTeam,
            SharedCorridor = corridor
        };
    }

    private static PlayerReplayEventRecord[] CreateEvents(
        PrototypePlayerCommand[] commands, PlayerCommandCheckpointRecord[] checkpoints)
    {
        var events = new List<PlayerReplayEventRecord>(commands.Length + checkpoints.Length);
        for (int i = 0; i < commands.Length; i++)
        {
            PrototypePlayerCommand command = commands[i];
            events.Add(new PlayerReplayEventRecord
            {
                Kind = PlayerCommandEvent,
                Tick = command.Tick,
                Sequence = command.Sequence,
                PlayerId = command.PlayerId,
                CommandKind = (int)command.Kind,
                IntValue = command.IntValue
            });
        }
        for (int i = 0; i < checkpoints.Length; i++)
        {
            events.Add(new PlayerReplayEventRecord
            {
                Kind = StateKeyframeEvent,
                Tick = checkpoints[i].Tick,
                StateHash = checkpoints[i].StateHash
            });
        }
        events.Sort(CompareEvents);
        return events.ToArray();
    }

    private static int CompareEvents(PlayerReplayEventRecord left, PlayerReplayEventRecord right)
    {
        int value = left.Tick.CompareTo(right.Tick);
        if (value != 0) return value;
        value = EventOrder(left.Kind).CompareTo(EventOrder(right.Kind));
        if (value != 0) return value;
        value = left.PlayerId.CompareTo(right.PlayerId);
        if (value != 0) return value;
        return left.Sequence.CompareTo(right.Sequence);
    }

    private static int EventOrder(string kind)
    {
        if (string.Equals(kind, PlayerCommandEvent, StringComparison.Ordinal)) return 0;
        if (string.Equals(kind, StateKeyframeEvent, StringComparison.Ordinal)) return 1;
        return int.MaxValue;
    }

    private static void Validate(PlayerCommandReplayDocument document)
    {
        if (document.FormatVersion != FormatVersion)
            throw new InvalidDataException($"unsupported command replay format {document.FormatVersion}");
        if (!string.Equals(document.GameVersion, GameVersion, StringComparison.Ordinal))
            throw new InvalidDataException("command replay game version mismatch");
        if (!string.Equals(document.ContentHash, GrayRangeGeneratedData.SourceMapSha256, StringComparison.Ordinal))
            throw new InvalidDataException("command replay content hash mismatch");
        if (string.IsNullOrWhiteSpace(document.MapId) || string.IsNullOrWhiteSpace(document.ScenarioId))
            throw new InvalidDataException("command replay scenario identity missing");
        if (document.TimestampUtc.Offset != TimeSpan.Zero)
            throw new InvalidDataException("command replay timestamp is not UTC");
        if (document.ProtocolVersion != ProtocolVersion)
            throw new InvalidDataException("command replay protocol version mismatch");
        if (!string.Equals(document.MapSourceSha256, GrayRangeGeneratedData.SourceMapSha256, StringComparison.Ordinal))
            throw new InvalidDataException("command replay map source hash mismatch");
        if (!string.Equals(document.RulesetId, RulesetId, StringComparison.Ordinal))
            throw new InvalidDataException("command replay ruleset mismatch");
        if (document.WinnerTeamId != 1 && document.WinnerTeamId != 2)
            throw new InvalidDataException("command replay winner team is invalid");
        if (document.ResolvedTick <= 0)
            throw new InvalidDataException("command replay resolved tick is invalid");
        if (!IsHex64(document.CommandHash) || !IsHex64(document.FinalStateHash))
            throw new InvalidDataException("command replay hash fields are malformed");
        ValidateInitialSnapshot(document.InitialSnapshot);
        ValidateEvents(document);
    }

    private static void ValidateInitialSnapshot(PlayerReplayInitialSnapshot snapshot)
    {
        if (snapshot == null || snapshot.Tick != 0 || !IsHex64(snapshot.StateHash))
            throw new InvalidDataException("command replay initial snapshot is invalid");
        AnnihilationPrototypeConfig config = CreateConfig(snapshot);
        string actual = AnnihilationPrototype.ComputeStateHash(AnnihilationPrototype.Create(config)).ToString("X16");
        if (!string.Equals(actual, snapshot.StateHash, StringComparison.Ordinal))
            throw new InvalidDataException("command replay initial snapshot hash mismatch");
    }

    private static AnnihilationPrototypeConfig CreateConfig(PlayerReplayInitialSnapshot snapshot)
    {
        if (snapshot.SharedCorridor == null || snapshot.SharedCorridor.Length < 2)
            throw new InvalidDataException("command replay initial corridor is invalid");
        if (snapshot.StartingIndustrialMilli < 0 || snapshot.MaxLiveTanksPerTeam < 1)
            throw new InvalidDataException("command replay initial economy is invalid");
        var corridor = new Int2[snapshot.SharedCorridor.Length];
        for (int i = 0; i < corridor.Length; i++)
            corridor[i] = new Int2(snapshot.SharedCorridor[i].X, snapshot.SharedCorridor[i].Y);
        return new AnnihilationPrototypeConfig
        {
            SpawnA = new Int2(snapshot.SpawnAX, snapshot.SpawnAY),
            SpawnB = new Int2(snapshot.SpawnBX, snapshot.SpawnBY),
            SharedCorridor = corridor,
            StartingIndustrialMilli = snapshot.StartingIndustrialMilli,
            MaxLiveTanksPerTeam = snapshot.MaxLiveTanksPerTeam
        };
    }

    private static void ValidateEvents(PlayerCommandReplayDocument document)
    {
        if (document.Events == null || document.Events.Length == 0)
            throw new InvalidDataException("command replay contains no events");
        int commandCount = 0;
        int keyframeCount = 0;
        int finalKeyframeTick = 0;
        string finalKeyframeHash = string.Empty;
        var commands = new List<PrototypePlayerCommand>();
        for (int i = 0; i < document.Events.Length; i++)
        {
            PlayerReplayEventRecord item = document.Events[i];
            if (item.Tick <= 0 || item.Tick > document.ResolvedTick)
                throw new InvalidDataException("command replay event tick is invalid");
            if (i > 0 && CompareEvents(document.Events[i - 1], item) >= 0)
                throw new InvalidDataException("command replay events are not strictly ordered");
            if (string.Equals(item.Kind, PlayerCommandEvent, StringComparison.Ordinal))
            {
                if (!string.IsNullOrEmpty(item.StateHash))
                    throw new InvalidDataException("command replay player event contains state data");
                commandCount++;
                commands.Add(new PrototypePlayerCommand(item.Tick, item.Sequence, item.PlayerId,
                    (PrototypePlayerCommandKind)item.CommandKind, item.IntValue));
            }
            else if (string.Equals(item.Kind, StateKeyframeEvent, StringComparison.Ordinal))
            {
                if (item.Sequence != 0 || item.PlayerId != 0 || item.CommandKind != 0 || item.IntValue != 0 || !IsHex64(item.StateHash))
                    throw new InvalidDataException("command replay keyframe payload is invalid");
                keyframeCount++;
                finalKeyframeTick = item.Tick;
                finalKeyframeHash = item.StateHash;
            }
            else
            {
                throw new InvalidDataException($"unsupported command replay event {item.Kind}");
            }
        }
        if (commandCount == 0 || keyframeCount == 0)
            throw new InvalidDataException("command replay event stream is incomplete");
        var timeline = new DeterministicCommandTimeline(commands);
        if (!string.Equals(timeline.ComputeCanonicalHash().ToString("X16"), document.CommandHash, StringComparison.Ordinal))
            throw new InvalidDataException("command replay event command hash mismatch");
        if (finalKeyframeTick != document.ResolvedTick ||
            !string.Equals(finalKeyframeHash, document.FinalStateHash, StringComparison.Ordinal))
            throw new InvalidDataException("command replay final keyframe does not match outcome");
    }

    private static PlayerCommandReplayDocument MigrateV1(PlayerCommandReplayV1Document legacy)
    {
        var v2 = new PlayerCommandReplayV2Document
        {
            FormatVersion = 2,
            GameVersion = GameVersion,
            ContentHash = legacy.MapSourceSha256,
            MapId = legacy.ScenarioId,
            TimestampUtc = DateTimeOffset.UnixEpoch,
            ProtocolVersion = ProtocolVersion,
            ScenarioId = legacy.ScenarioId,
            MapSourceSha256 = legacy.MapSourceSha256,
            RulesetId = legacy.RulesetId,
            CommandHash = legacy.CommandHash,
            WinnerTeamId = legacy.WinnerTeamId,
            ResolvedTick = legacy.ResolvedTick,
            FinalStateHash = legacy.FinalStateHash,
            Commands = legacy.Commands ?? Array.Empty<PlayerCommandRecord>(),
            Checkpoints = new[] { new PlayerCommandCheckpointRecord { Tick = legacy.ResolvedTick, StateHash = legacy.FinalStateHash } }
        };
        return MigrateV2(v2);
    }

    private static PlayerCommandReplayDocument MigrateV2(PlayerCommandReplayV2Document legacy)
    {
        AnnihilationPrototypeConfig config = GrayRangeGeneratedData.Create().CreateStandardAnnihilationConfig();
        var sourceCommands = legacy.Commands ?? Array.Empty<PlayerCommandRecord>();
        var commands = new PrototypePlayerCommand[sourceCommands.Length];
        for (int i = 0; i < commands.Length; i++)
        {
            PlayerCommandRecord item = sourceCommands[i];
            commands[i] = new PrototypePlayerCommand(item.Tick, item.Sequence, item.PlayerId,
                (PrototypePlayerCommandKind)item.Kind, item.IntValue);
        }
        return new PlayerCommandReplayDocument
        {
            FormatVersion = FormatVersion,
            GameVersion = legacy.GameVersion,
            ContentHash = legacy.ContentHash,
            MapId = legacy.MapId,
            TimestampUtc = legacy.TimestampUtc,
            DeterminismSeed = legacy.DeterminismSeed,
            ProtocolVersion = legacy.ProtocolVersion,
            ScenarioId = legacy.ScenarioId,
            MapSourceSha256 = legacy.MapSourceSha256,
            RulesetId = legacy.RulesetId,
            CommandHash = legacy.CommandHash,
            WinnerTeamId = legacy.WinnerTeamId,
            ResolvedTick = legacy.ResolvedTick,
            FinalStateHash = legacy.FinalStateHash,
            InitialSnapshot = CreateInitialSnapshot(config),
            Events = CreateEvents(commands, legacy.Checkpoints ?? Array.Empty<PlayerCommandCheckpointRecord>())
        };
    }

    private static bool IsHex64(string value)
    {
        return value != null && value.Length == 16 &&
            ulong.TryParse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _);
    }
}
