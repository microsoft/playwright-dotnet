using System.Text.Json.Serialization;

namespace PlaywrightSharp.Firefox.Protocol
{
    /// <summary>
    /// Basic structure for sending requests to chromium using the devtools protocol.
    /// </summary>
    /// <typeparam name="TFirefoxResponse">The response type.</typeparam>
    internal interface IFirefoxRequest<out TFirefoxResponse>
        where TFirefoxResponse : IFirefoxResponse
    {
        /// <summary>
        /// Gets the command name that will be sent to chromium.
        /// </summary>
        [JsonIgnore]
        string Command { get; }
    }
}
