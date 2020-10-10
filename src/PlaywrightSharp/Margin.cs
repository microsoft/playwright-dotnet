using System;
using System.Collections.Generic;

namespace PlaywrightSharp
{
    /// <summary>
    /// Margin options used in <see cref="IPage.GetPdfAsync(string, decimal, bool, string, string, bool, bool, string, PaperFormat?, string, string, Margin, bool)"/>.
    /// </summary>
    public class Margin : IEquatable<Margin>
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
        public static bool operator ==(Margin left, Margin right)
            => EqualityComparer<Margin>.Default.Equals(left, right);

        /// <inheritdoc cref="IEquatable{T}"/>
        public static bool operator !=(Margin left, Margin right) => !(left == right);

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

            return Equals((Margin)obj);
        }

        /// <summary>
        /// Checks for object equality.
        /// </summary>
        /// <param name="other">Options to check.</param>
        /// <returns>Whether the objects are equal or not.</returns>
        public bool Equals(Margin other)
            => other != null &&
                   Top == other.Top &&
                   Left == other.Left &&
                   Bottom == other.Bottom &&
                   Right == other.Right;

        /// <inheritdoc/>
        public override int GetHashCode()
            => -481391125
                ^ EqualityComparer<string>.Default.GetHashCode(Top)
                ^ EqualityComparer<string>.Default.GetHashCode(Left)
                ^ EqualityComparer<string>.Default.GetHashCode(Bottom)
                ^ EqualityComparer<string>.Default.GetHashCode(Right);
    }
}
