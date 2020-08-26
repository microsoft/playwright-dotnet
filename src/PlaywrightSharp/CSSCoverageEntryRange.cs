namespace PlaywrightSharp
{
    /// <summary>
    /// Script range.
    /// </summary>
    public class CSSCoverageEntryRange
    {
        /// <summary>
        /// A start offset in text, inclusive.
        /// </summary>
        public int Start { get; set; }

        /// <summary>
        /// An end offset in text, exclusive.
        /// </summary>
        public int End { get; set; }

        /// <inheritdoc cref="object"/>
        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            var range = obj as CSSCoverageEntryRange;

            return range.Start == Start &&
                   range.End == End;
        }

        /// <inheritdoc cref="object"/>
        public override int GetHashCode() => Start.GetHashCode() * 397 ^ End.GetHashCode() * 397;
    }
}
