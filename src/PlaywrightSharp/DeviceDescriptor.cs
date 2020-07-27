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
        public string Name { get; set; }

        /// <summary>
        /// User Agent.
        /// </summary>
        public string UserAgent { get; set; }

        /// <summary>
        /// ViewPort.
        /// </summary>
        public ViewportSize ViewPort { get; set; }

        /// <summary>
        /// Has touch.
        /// </summary>
        public bool HasTouch { get; set; }
    }
}
