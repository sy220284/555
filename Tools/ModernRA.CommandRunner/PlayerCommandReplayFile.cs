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
    public const int FormatVersion = 2;
    public const string GameVersion = "0.1.0-implementation-bootstrap";
    public const int ProtocolVersion = 1;
    public const string RulesetId = "RULESET_ANNIHILATION_STANDARD";
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = false
    };

    public static PlayerCommandReplayDocument Create(
        string scenarioId,
        PrototypePlayerCommand[] canonicalCommands,
        ulong commandHash,
        int winnerTeamId,
        int resolvedTick,
        ulong finalStateHash,
        PlayerCommandCheckpointRecord[] checkpoints)
    {
        if (string.IsNullOrWhiteSpace(scenarioId))
            throw new ArgumentException("scenario id is required", nameof(scenarioId));
        if (canonicalCommands == null)
            throw new ArgumentNullException(nameof(canonicalCommands));
        if (checkpoints == null)
            throw new ArgumentNullException(nameof(checkpoints));

        var records = new PlayerCommandRecord[canonicalCommands.Length];
        for (int i = 0; i < canonicalCommands.Length; i++)
        {
            PrototypePlayerCommand command = canonicalCommands[i];
            records[i] = new PlayerCommandRecord
            {
                Tick = command.Tick,
                Sequence = command.Sequence,
                PlayerId = command.PlayerId,
                Kind = (int)command.Kind,
                IntValue = command.IntValue
            };
        }

        return new PlayerCommandReplayDocument
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
            Commands = records,
            Checkpoints = checkpoints
        };
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
                document = JsonSerializer.Deserialize<PlayerCommandReplayDocument>(bytes, JsonOptions);
            }
        }
        if (document == null)
            throw new InvalidDataException("command replay deserialized to null");
        Validate(document);
        return document;
    }

    public static PrototypePlayerCommand[] ToCommands(PlayerCommandReplayDocument document)
    {
        Validate(document);
        var commands = new PrototypePlayerCommand[document.Commands.Length];
        for (int i = 0; i < document.Commands.Length; i++)
        {
            PlayerCommandRecord record = document.Commands[i];
            commands[i] = new PrototypePlayerCommand(
                record.Tick,
                record.Sequence,
                record.PlayerId,
                (PrototypePlayerCommandKind)record.Kind,
                record.IntValue);
        }
        var timeline = new DeterministicCommandTimeline(commands);
        ulong actualHash = timeline.ComputeCanonicalHash();
        if (!string.Equals(actualHash.ToString("X16"), document.CommandHash, StringComparison.Ordinal))
            throw new InvalidDataException("command replay canonical hash mismatch");
        return timeline.ToCanonicalArray();
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

    private static void Validate(PlayerCommandReplayDocument document)
    {
        if (document.FormatVersion != FormatVersion)
            throw new InvalidDataException($"unsupported command replay format {document.FormatVersion}");
        if (!string.Equals(document.GameVersion, GameVersion, StringComparison.Ordinal))
            throw new InvalidDataException("command replay game version mismatch");
        if (!string.Equals(document.ContentHash, GrayRangeGeneratedData.SourceMapSha256, StringComparison.Ordinal))
            throw new InvalidDataException("command replay content hash mismatch");
        if (string.IsNullOrWhiteSpace(document.MapId))
            throw new InvalidDataException("command replay map id missing");
        if (document.TimestampUtc.Offset != TimeSpan.Zero)
            throw new InvalidDataException("command replay timestamp is not UTC");
        if (document.ProtocolVersion != ProtocolVersion)
            throw new InvalidDataException("command replay protocol version mismatch");
        if (!string.Equals(document.MapSourceSha256, GrayRangeGeneratedData.SourceMapSha256, StringComparison.Ordinal))
            throw new InvalidDataException("command replay map source hash mismatch");
        if (!string.Equals(document.RulesetId, RulesetId, StringComparison.Ordinal))
            throw new InvalidDataException("command replay ruleset mismatch");
        if (string.IsNullOrWhiteSpace(document.ScenarioId))
            throw new InvalidDataException("command replay scenario id missing");
        if (document.Commands == null || document.Commands.Length == 0)
            throw new InvalidDataException("command replay contains no commands");
        if (document.WinnerTeamId != 1 && document.WinnerTeamId != 2)
            throw new InvalidDataException("command replay winner team is invalid");
        if (document.ResolvedTick <= 0)
            throw new InvalidDataException("command replay resolved tick is invalid");
        if (!IsHex64(document.CommandHash) || !IsHex64(document.FinalStateHash))
            throw new InvalidDataException("command replay hash fields are malformed");
        if (document.Checkpoints == null || document.Checkpoints.Length == 0)
            throw new InvalidDataException("command replay contains no state checkpoints");
        int previousTick = 0;
        for (int i = 0; i < document.Checkpoints.Length; i++)
        {
            PlayerCommandCheckpointRecord checkpoint = document.Checkpoints[i];
            if (checkpoint.Tick <= previousTick || checkpoint.Tick > document.ResolvedTick)
                throw new InvalidDataException("command replay checkpoints are not strictly increasing");
            if (!IsHex64(checkpoint.StateHash))
                throw new InvalidDataException("command replay checkpoint hash is malformed");
            previousTick = checkpoint.Tick;
        }
        PlayerCommandCheckpointRecord final = document.Checkpoints[document.Checkpoints.Length - 1];
        if (final.Tick != document.ResolvedTick ||
            !string.Equals(final.StateHash, document.FinalStateHash, StringComparison.Ordinal))
            throw new InvalidDataException("command replay final checkpoint does not match outcome");
    }

    private static PlayerCommandReplayDocument MigrateV1(PlayerCommandReplayV1Document legacy)
    {
        return new PlayerCommandReplayDocument
        {
            FormatVersion = FormatVersion,
            GameVersion = GameVersion,
            ContentHash = legacy.MapSourceSha256,
            MapId = legacy.ScenarioId,
            TimestampUtc = DateTimeOffset.UnixEpoch,
            DeterminismSeed = 0,
            ProtocolVersion = ProtocolVersion,
            ScenarioId = legacy.ScenarioId,
            MapSourceSha256 = legacy.MapSourceSha256,
            RulesetId = legacy.RulesetId,
            CommandHash = legacy.CommandHash,
            WinnerTeamId = legacy.WinnerTeamId,
            ResolvedTick = legacy.ResolvedTick,
            FinalStateHash = legacy.FinalStateHash,
            Commands = legacy.Commands ?? Array.Empty<PlayerCommandRecord>(),
            Checkpoints = new[]
            {
                new PlayerCommandCheckpointRecord
                {
                    Tick = legacy.ResolvedTick,
                    StateHash = legacy.FinalStateHash
                }
            }
        };
    }

    private static bool IsHex64(string value)
    {
        return value.Length == 16 && ulong.TryParse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _);
    }
}
