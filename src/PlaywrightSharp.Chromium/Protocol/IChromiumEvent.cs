namespace PlaywrightSharp.Chromium.Protocol
{
    /// <summary>
    /// Basic class for chromium events.
    /// </summary>
    public abstract class ChromiumEvent
    {
        /// <summary>
        /// Gets the name of the event recieved from chromium devtools protocol.
        /// </summary>
        public abstract string InternalName { get; }
    }
}
