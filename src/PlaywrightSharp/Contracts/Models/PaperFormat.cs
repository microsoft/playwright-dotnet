using System;
using System.Collections.Generic;

namespace PlaywrightSharp
{
    /// <summary>
    /// Paper format.
    /// </summary>
    /// <seealso cref="IPage.PdfAsync(string, float?, bool?, string, string, bool?, bool?, string, string, string, string, Margin, bool?)"/>
    public static class PaperFormat
    {
        /// <summary>
        /// Letter: 8.5 inches x 11 inches.
        /// </summary>
        public const string Letter = "Letter";

        /// <summary>
        /// Legal: 8.5 inches by 14 inches.
        /// </summary>
        public const string Legal = "Legal";

        /// <summary>
        /// Tabloid: 11 inches by 17 inches.
        /// </summary>
        public const string Tabloid = "Tabloid";

        /// <summary>
        /// Ledger: 17 inches by 11 inches.
        /// </summary>
        public const string Ledger = "Ledger";

        /// <summary>
        /// A0: 33.1 inches by 46.8 inches.
        /// </summary>
        public const string A0 = "A0";

        /// <summary>
        /// A1: 23.4 inches by 33.1 inches.
        /// </summary>
        public const string A1 = "A1";

        /// <summary>
        /// A2: 16.5 inches by 23.4 inches.
        /// </summary>
        public const string A2 = "A2";

        /// <summary>
        /// A3: 11.7 inches by 16.5 inches.
        /// </summary>
        public const string A3 = "A3";

        /// <summary>
        /// A4: 8.27 inches by 11.7 inches.
        /// </summary>
        public const string A4 = "A4";

        /// <summary>
        /// A5: 5.83 inches by 8.27 inches.
        /// </summary>
        public const string A5 = "A5";

        /// <summary>
        /// A6: 4.13 inches by 5.83 inches.
        /// </summary>
        public const string A6 = "A6";
    }
}
