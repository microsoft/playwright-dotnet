using System;
using System.Collections.Generic;

namespace PlaywrightSharp
{
    /// <summary>
    /// Margin options used in <see cref="PdfOptions"/>.
    /// </summary>
    public class MarginOptions : IEquatable<MarginOptions>
    {
        /// <summary>
        /// Top margin, accepts values labeled with units.
        /// </summary>
        public string Top { get; set; }

        /// <summary>
        /// Left margin, accepts values labeled with units.
        /// </summary>
        public string Left { get; set; }

        /// <summary>
        /// Bottom margin, accepts values labeled with units.
        /// </summary>
        public string Bottom { get; set; }

        /// <summary>
        /// Right margin, accepts values labeled with units.
        /// </summary>
        public string Right { get; set; }

        /// <inheritdoc cref="IEquatable{T}"/>
        public static bool operator ==(MarginOptions left, MarginOptions right)
            => EqualityComparer<MarginOptions>.Default.Equals(left, right);

        /// <inheritdoc cref="IEquatable{T}"/>
        public static bool operator !=(MarginOptions left, MarginOptions right) => !(left == right);

        /// <summary>
        /// Checks for object equality.
        /// </summary>
        /// <param name="obj">Options to check.</param>
        /// <returns>Whether the objects are equal or not.</returns>
        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            return Equals((MarginOptions)obj);
        }

        /// <summary>
        /// Checks for object equality.
        /// </summary>
        /// <param name="options">Options to check.</param>
        /// <returns>Whether the objects are equal or not.</returns>
        public bool Equals(MarginOptions options)
            => options != null &&
                   Top == options.Top &&
                   Left == options.Left &&
                   Bottom == options.Bottom &&
                   Right == options.Right;

        /// <inheritdoc/>
        public override int GetHashCode()
            => -481391125
                ^ EqualityComparer<string>.Default.GetHashCode(Top)
                ^ EqualityComparer<string>.Default.GetHashCode(Left)
                ^ EqualityComparer<string>.Default.GetHashCode(Bottom)
                ^ EqualityComparer<string>.Default.GetHashCode(Right);
    }
}
