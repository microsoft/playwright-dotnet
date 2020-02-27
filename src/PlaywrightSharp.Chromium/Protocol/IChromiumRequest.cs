using System.Text.Json.Serialization;

namespace PlaywrightSharp.Chromium.Protocol
{
    /// <summary>
    /// Basic structure for sending requests to chromium using the devtools protocol.
    /// </summary>
    /// <typeparam name="TChromiumResponse">The response type.</typeparam>
    internal interface IChromiumRequest<TChromiumResponse>
        where TChromiumResponse : IChromiumResponse
    {
        /// <summary>
        /// Gets the command name that will be sent to chromium.
        /// </summary>
        [JsonIgnore]
        string Command { get; }
    }
}
