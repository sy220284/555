using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ModernRA.Rules;

internal sealed class PlayerCommandReplayDocument
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

internal static class PlayerCommandReplayFile
{
    public const int SchemaVersion = 1;
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
        ulong finalStateHash)
    {
        if (string.IsNullOrWhiteSpace(scenarioId))
            throw new ArgumentException("scenario id is required", nameof(scenarioId));
        if (canonicalCommands == null)
            throw new ArgumentNullException(nameof(canonicalCommands));

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
            SchemaVersion = SchemaVersion,
            ScenarioId = scenarioId,
            MapSourceSha256 = GrayRangeGeneratedData.SourceMapSha256,
            RulesetId = RulesetId,
            CommandHash = commandHash.ToString("X16"),
            WinnerTeamId = winnerTeamId,
            ResolvedTick = resolvedTick,
            FinalStateHash = finalStateHash.ToString("X16"),
            Commands = records
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
        PlayerCommandReplayDocument? document = JsonSerializer.Deserialize<PlayerCommandReplayDocument>(bytes, JsonOptions);
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
        if (document.SchemaVersion != SchemaVersion)
            throw new InvalidDataException($"unsupported command replay schema {document.SchemaVersion}");
        if (!string.Equals(document.MapSourceSha256, GrayRangeGeneratedData.SourceMapSha256, StringComparison.Ordinal))
            throw new InvalidDataException("command replay map source hash mismatch");
        if (!string.Equals(document.RulesetId, RulesetId, StringComparison.Ordinal))
            throw new InvalidDataException("command replay ruleset mismatch");
        if (string.IsNullOrWhiteSpace(document.ScenarioId))
            throw new InvalidDataException("command replay scenario id missing");
        if (!string.Equals(document.ScenarioId, GrayRangeGeneratedData.Create().MapId, StringComparison.Ordinal))
            throw new InvalidDataException("command replay scenario id mismatch");
        if (document.Commands == null || document.Commands.Length == 0)
            throw new InvalidDataException("command replay contains no commands");
        if (document.WinnerTeamId != 1 && document.WinnerTeamId != 2)
            throw new InvalidDataException("command replay winner team is invalid");
        if (document.ResolvedTick <= 0)
            throw new InvalidDataException("command replay resolved tick is invalid");
        if (!IsHex64(document.CommandHash) || !IsHex64(document.FinalStateHash))
            throw new InvalidDataException("command replay hash fields are malformed");
    }

    private static bool IsHex64(string value)
    {
        return value.Length == 16 && ulong.TryParse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _);
    }
}
