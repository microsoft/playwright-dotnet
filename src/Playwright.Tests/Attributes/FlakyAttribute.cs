using System;

namespace Microsoft.Playwright.Tests
{
    public class FlakyAttribute : Attribute
    {
        public FlakyAttribute()
        {
        }

        /// <summary>
        /// Determines the maximum amount of retries a flaky test should be granted.
        /// </summary>
        public int MaximumRetries { get; set; } = 3;

        /// <summary>
        /// If we know the cause of flakiness, this property should reflect it.
        /// </summary>
        public string Justification { get; set; }
    }
}
