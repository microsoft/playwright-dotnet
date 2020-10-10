using System;
using System.Collections.Generic;

namespace PlaywrightSharp
{
    /// <summary>
    /// Paper format.
    /// </summary>
    /// <seealso cref="IPage.GetPdfAsync(string, decimal, bool, string, string, bool, bool, string, PaperFormat?, string, string, Margin, bool)"/>
    public enum PaperFormat
    {
        /// <summary>
        /// Letter: 8.5 inches x 11 inches.
        /// </summary>
        Letter,

        /// <summary>
        /// Legal: 8.5 inches by 14 inches.
        /// </summary>
        Legal,

        /// <summary>
        /// Tabloid: 11 inches by 17 inches.
        /// </summary>
        Tabloid,

        /// <summary>
        /// Ledger: 17 inches by 11 inches.
        /// </summary>
        Ledger,

        /// <summary>
        /// A0: 33.1 inches by 46.8 inches.
        /// </summary>
        A0,

        /// <summary>
        /// A1: 23.4 inches by 33.1 inches.
        /// </summary>
        A1,

        /// <summary>
        /// A2: 16.5 inches by 23.4 inches.
        /// </summary>
        A2,

        /// <summary>
        /// A3: 11.7 inches by 16.5 inches.
        /// </summary>
        A3,

        /// <summary>
        /// A4: 8.27 inches by 11.7 inches.
        /// </summary>
        A4,

        /// <summary>
        /// A5: 5.83 inches by 8.27 inches.
        /// </summary>
        A5,

        /// <summary>
        /// A6: 4.13 inches by 5.83 inches.
        /// </summary>
        A6,
    }
}
