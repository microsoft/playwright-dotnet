namespace PlaywrightSharp
{
    /// <summary>
    /// Script range. See <see cref="JSCoverageFunction"/>.
    /// </summary>
    public class JSCoverageEntryRange
    {
        /// <summary>
        /// A start offset in text, inclusive.
        /// </summary>
        public int StartOffset { get; set; }

        /// <summary>
        /// An end offset in text, exclusive.
        /// </summary>
        public int EndOffset { get; set; }

        /// <summary>
        /// Count.
        /// </summary>
        public int Count { get; set; }

        /// <inheritdoc cref="object"/>
        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            var range = obj as JSCoverageEntryRange;

            return range.StartOffset == StartOffset &&
                   range.EndOffset == EndOffset &&
                   range.Count == Count;
        }

        /// <inheritdoc cref="object"/>
        public override int GetHashCode()
            => StartOffset.GetHashCode() * 397 ^ EndOffset.GetHashCode() * 397 ^ Count.GetHashCode() * 397;
    }
}
