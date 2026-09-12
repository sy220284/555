using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ModernRA.Rules;

internal sealed class AnnihilationReplayFileData
{
    public string ScenarioId = string.Empty;
    public string SourceMapSha256 = string.Empty;
    public int CheckpointIntervalTicks;
    public AnnihilationReplayTape Tape = new AnnihilationReplayTape();
}

internal static class AnnihilationReplayFile
{
    public const int SchemaVersion = 1;

    public static string Write(AnnihilationReplayTape tape, string scenarioId, string sourceMapSha256, int checkpointIntervalTicks)
    {
        if (tape == null)
            throw new ArgumentNullException(nameof(tape));
        if (string.IsNullOrWhiteSpace(scenarioId))
            throw new ArgumentException("scenario id is required", nameof(scenarioId));
        ValidateSha256(sourceMapSha256, nameof(sourceMapSha256));
        if (checkpointIntervalTicks <= 0)
            throw new ArgumentOutOfRangeException(nameof(checkpointIntervalTicks));
        if (tape.Checkpoints.Count == 0)
            throw new InvalidOperationException("replay tape has no checkpoints");

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = false }))
        {
            writer.WriteStartObject();
            writer.WriteNumber("schema_version", SchemaVersion);
            writer.WriteString("scenario_id", scenarioId);
            writer.WriteString("source_map_sha256", sourceMapSha256.ToLowerInvariant());
            writer.WriteString("ruleset_id", "RULESET_ANNIHILATION_STANDARD");
            writer.WriteNumber("checkpoint_interval_ticks", checkpointIntervalTicks);
            writer.WriteNumber("winner_team_id", tape.WinnerTeamId);
            writer.WriteNumber("resolved_tick", tape.ResolvedTick);
            writer.WriteString("final_state_hash", tape.FinalStateHash.ToString("X16"));
            writer.WriteStartArray("checkpoints");
            for (int i = 0; i < tape.Checkpoints.Count; i++)
            {
                AnnihilationReplayCheckpoint checkpoint = tape.Checkpoints[i];
                writer.WriteStartObject();
                writer.WriteNumber("tick", checkpoint.Tick);
                writer.WriteString("state_hash", checkpoint.StateHash.ToString("X16"));
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    public static AnnihilationReplayFileData Read(string json, string expectedScenarioId, string expectedSourceMapSha256)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new ArgumentException("replay json is required", nameof(json));
        ValidateSha256(expectedSourceMapSha256, nameof(expectedSourceMapSha256));

        using JsonDocument document = JsonDocument.Parse(json);
        JsonElement root = document.RootElement;
        int schemaVersion = root.GetProperty("schema_version").GetInt32();
        if (schemaVersion != SchemaVersion)
            throw new InvalidOperationException($"unsupported replay schema {schemaVersion}");

        string scenarioId = root.GetProperty("scenario_id").GetString() ?? throw new InvalidOperationException("replay scenario id missing");
        string sourceMapSha256 = root.GetProperty("source_map_sha256").GetString() ?? throw new InvalidOperationException("replay source map hash missing");
        string rulesetId = root.GetProperty("ruleset_id").GetString() ?? throw new InvalidOperationException("replay ruleset id missing");
        if (!string.Equals(scenarioId, expectedScenarioId, StringComparison.Ordinal))
            throw new InvalidOperationException("replay scenario does not match current scenario");
        if (!string.Equals(sourceMapSha256, expectedSourceMapSha256, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("replay source map hash does not match current content");
        if (!string.Equals(rulesetId, "RULESET_ANNIHILATION_STANDARD", StringComparison.Ordinal))
            throw new InvalidOperationException("replay ruleset is not standard annihilation");

        int interval = root.GetProperty("checkpoint_interval_ticks").GetInt32();
        if (interval <= 0)
            throw new InvalidOperationException("replay checkpoint interval is invalid");

        var tape = new AnnihilationReplayTape
        {
            WinnerTeamId = root.GetProperty("winner_team_id").GetInt32(),
            ResolvedTick = root.GetProperty("resolved_tick").GetInt32(),
            FinalStateHash = ParseStateHash(root.GetProperty("final_state_hash").GetString(), "final_state_hash")
        };

        int previousTick = 0;
        foreach (JsonElement item in root.GetProperty("checkpoints").EnumerateArray())
        {
            int tick = item.GetProperty("tick").GetInt32();
            if (tick <= previousTick)
                throw new InvalidOperationException("replay checkpoints are not strictly increasing");
            ulong hash = ParseStateHash(item.GetProperty("state_hash").GetString(), "state_hash");
            tape.Checkpoints.Add(new AnnihilationReplayCheckpoint(tick, hash));
            previousTick = tick;
        }

        if (tape.Checkpoints.Count == 0)
            throw new InvalidOperationException("replay file contains no checkpoints");
        AnnihilationReplayCheckpoint final = tape.Checkpoints[tape.Checkpoints.Count - 1];
        if (final.Tick != tape.ResolvedTick || final.StateHash != tape.FinalStateHash)
            throw new InvalidOperationException("replay final checkpoint does not match replay result");

        return new AnnihilationReplayFileData
        {
            ScenarioId = scenarioId,
            SourceMapSha256 = sourceMapSha256,
            CheckpointIntervalTicks = interval,
            Tape = tape
        };
    }

    public static string ComputeSha256(string json)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(json);
        return Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    }

    private static ulong ParseStateHash(string? value, string fieldName)
    {
        if (value == null || value.Length != 16 || !ulong.TryParse(value, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out ulong hash))
            throw new InvalidOperationException($"replay {fieldName} is invalid");
        return hash;
    }

    private static void ValidateSha256(string value, string parameterName)
    {
        if (value == null || value.Length != 64)
            throw new ArgumentException("SHA256 must contain 64 hexadecimal characters", parameterName);
        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            bool hex = (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
            if (!hex)
                throw new ArgumentException("SHA256 contains non-hexadecimal characters", parameterName);
        }
    }
}
