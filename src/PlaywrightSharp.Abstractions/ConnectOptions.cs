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
        public int SlowMo { get; set; }

        /// <summary>
        /// We don´t need doc. This is going to be removed.
        /// </summary>
        public string BrowserURL { get; set; }

        /// <summary>
        /// A browser websocket endpoint to connect to.
        /// </summary>
        public string BrowserWSEndpoint { get; set; }

        /// <summary>
        /// Optional factory for <see cref="IConnectionTransport"/> implementations.
        /// </summary>
        public TransportFactory TransportFactory { get; set; }

        /// <summary>
        /// Whether the transport should enqueue messages or not.
        /// </summary>
        public bool EnqueueTransportMessages { get; set; }

        /// <summary>
        /// Clones the <see cref="ConnectOptions"/>.
        /// </summary>
        /// <returns>A copy of the current <see cref="ConnectOptions"/>.</returns>
        public ConnectOptions Clone() => (ConnectOptions)MemberwiseClone();
    }
}
