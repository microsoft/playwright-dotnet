using System.Text.Json.Serialization;

namespace PlaywrightSharp
{
    /// <summary>
    /// Options for <see cref="IBrowserType.ConnectAsync(ConnectOptions)"/>.
    /// </summary>
    public class ConnectOptions
    {
        /// <summary>
        /// Slows down PlaywrightSharp operations by the specified amount of milliseconds. Useful so that you can see what is going on.
        /// </summary>
        public int? SlowMo { get; set; }

        /// <summary>
        /// Maximum navigation time in milliseconds.
        /// </summary>
        public int? Timeout { get; set; }

        /// <summary>
        /// A browser websocket endpoint to connect to.
        /// </summary>
        [JsonPropertyName("wsEndpoint")]
        public string WSEndpoint { get; set; }
    }
}
