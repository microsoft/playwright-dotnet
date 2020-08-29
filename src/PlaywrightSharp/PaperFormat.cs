using System;
using System.Collections.Generic;

namespace PlaywrightSharp
{
    /// <summary>
    /// Paper format.
    /// </summary>
    /// <seealso cref="IPage.GetPdfAsync(string, decimal, bool, string, string, bool, bool, string, PaperFormat, string, string, MarginOptions, bool)"/>
    public class PaperFormat : IEquatable<PaperFormat>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PaperFormat"/> class.
        /// </summary>
        /// <param name="width">Page width in inches.</param>
        /// <param name="height">Page height in inches.</param>
        public PaperFormat(double width, double height)
        {
            Width = width;
            Height = height;
        }

        /// <summary>
        /// Letter: 8.5 inches x 11 inches.
        /// </summary>
        public static PaperFormat Letter => new PaperFormat(8.5, 11);

        /// <summary>
        /// Legal: 8.5 inches by 14 inches.
        /// </summary>
        public static PaperFormat Legal => new PaperFormat(8.5, 14);

        /// <summary>
        /// Tabloid: 11 inches by 17 inches.
        /// </summary>
        public static PaperFormat Tabloid => new PaperFormat(11, 17);

        /// <summary>
        /// Ledger: 17 inches by 11 inches.
        /// </summary>
        public static PaperFormat Ledger => new PaperFormat(17, 11);

        /// <summary>
        /// A0: 33.1 inches by 46.8 inches.
        /// </summary>
        public static PaperFormat A0 => new PaperFormat(33.1, 46.8);

        /// <summary>
        /// A1: 23.4 inches by 33.1 inches.
        /// </summary>
        public static PaperFormat A1 => new PaperFormat(23.4, 33.1);

        /// <summary>
        /// A2: 16.5 inches by 23.4 inches.
        /// </summary>
        public static PaperFormat A2 => new PaperFormat(16.54, 23.4);

        /// <summary>
        /// A3: 11.7 inches by 16.5 inches.
        /// </summary>
        public static PaperFormat A3 => new PaperFormat(11.7, 16.54);

        /// <summary>
        /// A4: 8.27 inches by 11.7 inches.
        /// </summary>
        public static PaperFormat A4 => new PaperFormat(8.27, 11.7);

        /// <summary>
        /// A5: 5.83 inches by 8.27 inches.
        /// </summary>
        public static PaperFormat A5 => new PaperFormat(5.83, 8.27);

        /// <summary>
        /// A6: 4.13 inches by 5.83 inches.
        /// </summary>
        public static PaperFormat A6 => new PaperFormat(4.13, 5.83);

        /// <summary>
        /// Page width in inches.
        /// </summary>
        public double Width { get; set; }

        /// <summary>
        /// Page height in inches.
        /// </summary>
        public double Height { get; set; }

        /// <inheritdoc cref="IEquatable{T}"/>
        public static bool operator ==(PaperFormat left, PaperFormat right)
            => EqualityComparer<PaperFormat>.Default.Equals(left, right);

        /// <inheritdoc cref="IEquatable{T}"/>
        public static bool operator !=(PaperFormat left, PaperFormat right) => !(left == right);

        /// <summary>
        /// Checks for object equality.
        /// </summary>
        /// <param name="obj">Format to check.</param>
        /// <returns>Whether the objects are equal or not.</returns>
        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            return Equals((PaperFormat)obj);
        }

        /// <summary>
        /// Checks for object equality.
        /// </summary>
        /// <param name="format">Format to check.</param>
        /// <returns>Whether the objects are equal or not.</returns>
        public bool Equals(PaperFormat format)
            => format != null &&
               Width == format.Width &&
               Height == format.Height;

        /// <inheritdoc cref="object.GetHashCode"/>
        public override int GetHashCode()
            => 859600377
               ^ Width.GetHashCode()
               ^ Height.GetHashCode();
    }
}
