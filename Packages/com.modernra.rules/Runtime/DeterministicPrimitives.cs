using System;

namespace ModernRA.Rules
{
    public readonly struct Int2
    {
        public readonly int X;
        public readonly int Y;

        public Int2(int x, int y)
        {
            X = x;
            Y = y;
        }
    }

    public struct DeterministicRng
    {
        public ulong State;

        public DeterministicRng(ulong seed)
        {
            State = seed == 0 ? 0x9E3779B97F4A7C15UL : seed;
        }

        public uint NextUInt()
        {
            unchecked
            {
                State += 0x9E3779B97F4A7C15UL;
                ulong z = State;
                z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
                z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
                z ^= z >> 31;
                return (uint)(z >> 32);
            }
        }

        public int NextPermille()
        {
            return (int)(NextUInt() % 1000U);
        }
    }

    public static class StateHash64
    {
        private const ulong Offset = 14695981039346656037UL;
        private const ulong Prime = 1099511628211UL;

        public static ulong Begin() => Offset;

        public static ulong Add(ulong hash, int value)
        {
            unchecked
            {
                uint v = (uint)value;
                for (int i = 0; i < 4; i++)
                {
                    hash ^= (byte)(v & 0xFF);
                    hash *= Prime;
                    v >>= 8;
                }
                return hash;
            }
        }

        public static ulong Add(ulong hash, long value)
        {
            unchecked
            {
                ulong v = (ulong)value;
                for (int i = 0; i < 8; i++)
                {
                    hash ^= (byte)(v & 0xFF);
                    hash *= Prime;
                    v >>= 8;
                }
                return hash;
            }
        }

        public static ulong Add(ulong hash, ulong value)
        {
            unchecked
            {
                ulong v = value;
                for (int i = 0; i < 8; i++)
                {
                    hash ^= (byte)(v & 0xFF);
                    hash *= Prime;
                    v >>= 8;
                }
                return hash;
            }
        }

        public static ulong Add(ulong hash, string value)
        {
            unchecked
            {
                if (value == null)
                    return Add(hash, -1);
                for (int i = 0; i < value.Length; i++)
                {
                    char c = value[i];
                    hash ^= (byte)(c & 0xFF);
                    hash *= Prime;
                    hash ^= (byte)(c >> 8);
                    hash *= Prime;
                }
                return hash;
            }
        }
    }
}
