namespace PlaywrightSharp
{
    /// <summary>
    /// Device descriptor.
    /// </summary>
    public class DeviceDescriptor
    {
        /// <summary>
        /// Device name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }

        /// <summary>
        /// User Agent.
        /// </summary>
        /// <value>The user agent.</value>
        public string UserAgent { get; set; }

        /// <summary>
        /// ViewPort.
        /// </summary>
        /// <value>The viewport.</value>
        public Viewport ViewPort { get; set; }
    }
}
