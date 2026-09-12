using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace ModernRA.Core
{
    public readonly struct StableId : IEquatable<StableId>
    {
        public readonly string Value;

        public StableId(string value)
        {
            if (!StableIdRules.IsValid(value))
                throw new ArgumentException($"Invalid stable id: {value}", nameof(value));
            Value = value;
        }

        public bool Equals(StableId other) => string.Equals(Value, other.Value, StringComparison.Ordinal);
        public override bool Equals(object obj) => obj is StableId other && Equals(other);
        public override int GetHashCode() => Value == null ? 0 : StringComparer.Ordinal.GetHashCode(Value);
        public override string ToString() => Value ?? string.Empty;
        public static implicit operator string(StableId id) => id.Value;
    }

    public static class StableIdRules
    {
        private static readonly HashSet<string> Prefixes = new(StringComparer.Ordinal)
        {
            "FAC", "CTY", "UNIT", "FORM", "BLD", "WPN", "TECH", "STRAT", "ABL", "STATUS",
            "VET", "CHAR", "MODE", "RULESET", "MAP", "MIS", "SFX", "VOICE", "MUSIC", "UIEV",
            "LOC", "MIN", "MAT"
        };

        private static readonly Regex Pattern = new("^[A-Z][A-Z0-9]*(?:_[A-Z0-9]+)+$", RegexOptions.Compiled);

        public static bool IsValid(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || !Pattern.IsMatch(value)) return false;
            var underscore = value.IndexOf('_');
            return underscore > 0 && Prefixes.Contains(value.Substring(0, underscore));
        }
    }
}
