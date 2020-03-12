namespace PlaywrightSharp.Firefox.Protocol
{
    /// <summary>
    /// Basic class for chromium events.
    /// </summary>
    internal interface IFirefoxEvent
    {
        /// <summary>
        /// Gets the name of the event recieved from chromium devtools protocol.
        /// </summary>
        string InternalName { get; }
    }
}
