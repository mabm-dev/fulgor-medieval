using System;

namespace Fulgor.Core.Randomness
{
    /// <summary>
    /// Implementación de Mulberry32 compatible bit a bit con el prototipo TypeScript.
    /// </summary>
    public sealed class DeterministicRandom
    {
        private const uint Increment = 0x6D2B79F5u;
        private const double UInt32Range = 4294967296d;
        private const long JavaScriptMaxSafeInteger = 9007199254740991L;

        private uint state;

        public DeterministicRandom(long seed)
        {
            if (seed < -JavaScriptMaxSafeInteger || seed > JavaScriptMaxSafeInteger)
            {
                throw new ArgumentOutOfRangeException(nameof(seed), "La semilla debe ser un entero seguro de JavaScript.");
            }

            state = unchecked((uint)seed);
        }

        public uint State => state;

        public double Next()
        {
            unchecked
            {
                state += Increment;
                var value = state;
                value = (value ^ (value >> 15)) * (value | 1u);
                value ^= value + (value ^ (value >> 7)) * (value | 61u);
                return (value ^ (value >> 14)) / UInt32Range;
            }
        }

        public int NextInteger(int minimum, int maximum)
        {
            if (minimum > maximum)
            {
                throw new ArgumentOutOfRangeException(nameof(minimum), "El límite mínimo no puede superar al máximo.");
            }

            var range = (long)maximum - minimum + 1L;
            return (int)(minimum + (long)Math.Floor(Next() * range));
        }
    }
}
