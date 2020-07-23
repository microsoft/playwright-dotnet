using Xunit;
using Xunit.Sdk;

namespace PlaywrightSharp.Tests.Helpers
{
    /// <summary>
    /// Works just like [Fact] except that failures are retried (by default, 3 times).
    /// </summary>
    [XunitTestCaseDiscoverer("PlaywrightSharp.Tests.Helpers.RetryFactDiscoverer", "PlaywrightSharp.Tests")]
    public class RetryAttribute : FactAttribute
    {
        /// <inheritdoc />
        public RetryAttribute(int maxRetries = 3)
        {
            MaxRetries = maxRetries;
        }

        /// <summary>
        /// Number of retries allowed for a failed test. If unset (or set less than 1), will
        /// default to 3 attempts.
        /// </summary>
        public int MaxRetries { get; set; }
    }
}
