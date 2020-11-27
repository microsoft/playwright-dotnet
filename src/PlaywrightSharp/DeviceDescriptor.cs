using System;

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
        /// Viewport.
        /// </summary>
        public ViewportSize Viewport { get; set; }

        /// <summary>
        /// Has touch.
        /// </summary>
        public bool HasTouch { get; set; }

        /// <summary>
        /// Is mobile.
        /// </summary>
        public bool IsMobile { get; set; }

        /// <summary>
        /// Device scale factor.
        /// </summary>
        public decimal DeviceScaleFactor { get; set; }

        /// <summary>
        /// Converts the <see cref="DeviceDescriptor"/> to <see cref="BrowserContextOptions"/>.
        /// </summary>
        /// <param name="descriptor">Descriptor to convert.</param>
        public static implicit operator BrowserContextOptions(DeviceDescriptor descriptor)
        {
            if (descriptor == null)
            {
                return null;
            }

            var options = new BrowserContextOptions();
            descriptor.HydrateBrowserContextOptions(options);
            return options;
        }

        /// <summary>
        /// Converts the <see cref="BrowserContextOptions"/> to <see cref="BrowserContextOptions"/>.
        /// </summary>
        /// <returns>A <see cref="BrowserContextOptions"/> with the same information as the <see cref="DeviceDescriptor"/>.</returns>
        public BrowserContextOptions ToBrowserContextOptions() => this;

        internal void HydrateBrowserContextOptions(BrowserContextOptions options)
        {
            options.UserAgent = UserAgent;
            options.Viewport = Viewport;
            options.HasTouch = HasTouch;
            options.IsMobile = IsMobile;
            options.DeviceScaleFactor = DeviceScaleFactor;
        }
    }
}
