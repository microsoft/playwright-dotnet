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
        public string WebSocketEndpoint { get; set; }
    }
}
