namespace PlaywrightSharp
{
    /// <summary>
    /// Script range.
    /// </summary>
    public class CoverageEntryRange
    {
        /// <summary>
        /// A start offset in text, inclusive.
        /// </summary>
        public int Start { get; internal set; }

        /// <summary>
        /// An end offset in text, exclusive.
        /// </summary>
        public int End { get; internal set; }

        /// <inheritdoc cref="object"/>
        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            var range = obj as CoverageEntryRange;

            return range.Start == Start &&
                   range.End == End;
        }

        /// <inheritdoc cref="object"/>
        public override int GetHashCode() => Start.GetHashCode() * 397 ^ End.GetHashCode() * 397;
    }
}
