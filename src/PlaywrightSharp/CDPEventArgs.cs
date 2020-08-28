using System.Text.Json;

namespace PlaywrightSharp
{
    /// <summary>
    /// See <see cref="ICDPSession.MessageReceived"/>.
    /// </summary>
    public class CDPEventArgs
    {
        /// <summary>
        /// Method name.
        /// </summary>
        public string Method { get; set; }

        /// <summary>
        /// Message arguments.
        /// </summary>
        public JsonElement? Params { get; set; }
    }
}
