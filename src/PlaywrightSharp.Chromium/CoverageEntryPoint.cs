using System;
using PlaywrightSharp.Chromium.Protocol.Profiler;

namespace PlaywrightSharp.Chromium
{
    internal class CoverageEntryPoint : IComparable<CoverageEntryPoint>
    {
        public int Offset { get; set; }

        public int Type { get; set; }

        public CoverageRange Range { get; set; }

        public int CompareTo(CoverageEntryPoint other)
        {
            // Sort with increasing offsets.
            if (Offset != other.Offset)
            {
                return Offset - other.Offset;
            }

            // All "end" points should go before "start" points.
            if (Type != other.Type)
            {
                return Type - other.Type;
            }

            int aLength = Range.EndOffset.Value - Range.StartOffset.Value;
            int bLength = other.Range.EndOffset.Value - other.Range.StartOffset.Value;

            // For two "start" points, the one with longer range goes first.
            if (Type == 0)
            {
                return bLength - aLength;
            }

            // For two "end" points, the one with shorter range goes first.
            return aLength - bLength;
        }
    }
}
