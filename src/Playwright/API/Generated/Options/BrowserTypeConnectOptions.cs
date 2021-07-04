using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Microsoft.Playwright.API.Generated.Options
{
    public class BrowserTypeConnectOptions
    {
        public BrowserTypeConnectOptions() { }

        public BrowserTypeConnectOptions(BrowserTypeConnectOptions clone)
        {
            if (clone == null) return;
            Headers = clone.Headers;
            Timeout = clone.Timeout;
            SlowMo = clone.SlowMo;
        }

        /// <summary>
        /// <para>
        /// Additional HTTP headers to be sent with web socket connect request.
        /// </para>
        /// </summary>
        [JsonPropertyName("headers")]
        public IEnumerable<KeyValuePair<string, string>> Headers { get; set; }

        /// <summary>
        /// <para>
        /// Maximum time in milliseconds to wait for the browser instance to start. Defaults
        /// to <c>30000</c> (30 seconds). Pass <c>0</c> to disable timeout.
        /// </para>
        /// </summary>
        [JsonPropertyName("timeout")]
        public float? Timeout { get; set; }

        /// <summary>
        /// <para>
        /// Slows down Playwright operations by the specified amount of milliseconds. Useful
        /// so that you can see what is going on.
        /// </para>
        /// </summary>
        [JsonPropertyName("slowMo")]
        public float? SlowMo { get; set; }
    }
}
