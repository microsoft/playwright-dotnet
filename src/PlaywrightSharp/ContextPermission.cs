namespace PlaywrightSharp
{
    /// <summary>
    /// Options for <see cref="IBrowserContext.GrantPermissionsAsync(ContextPermission[], string)"/>.
    /// </summary>
#pragma warning disable CA1711 // CA1711 doesn't want us to use the Permission suffix.
    public enum ContextPermission
#pragma warning restore CA1711
    {
        /// <summary>
        /// Geolocation.
        /// </summary>
        Geolocation,

        /// <summary>
        /// MIDI.
        /// </summary>
        Midi,

        /// <summary>
        /// Notifications.
        /// </summary>
        Notifications,

        /// <summary>
        /// Push.
        /// </summary>
        Push,

        /// <summary>
        /// Camera.
        /// </summary>
        Camera,

        /// <summary>
        /// Microphone.
        /// </summary>
        Microphone,

        /// <summary>
        /// Background sync.
        /// </summary>
        BackgroundSync,

        /// <summary>
        /// Ambient light sensor, Accelerometer, Gyroscope, Magnetometer.
        /// </summary>
        Sensors,

        /// <summary>
        /// Accessibility events.
        /// </summary>
        AccessibilityEvents,

        /// <summary>
        /// Clipboard read.
        /// </summary>
        ClipboardRead,

        /// <summary>
        /// Clipboard write.
        /// </summary>
        ClipboardWrite,

        /// <summary>
        /// Payment handler.
        /// </summary>
        PaymentHandler,

        /// <summary>
        /// MIDI sysex.
        /// </summary>
        MidiSysex,

        /// <summary>
        /// Ambient Light Sensor.
        /// </summary>
        AmbientLightSensor,

        /// <summary>
        /// Accelerometer.
        /// </summary>
        Accelerometer,

        /// <summary>
        /// Gyroscope.
        /// </summary>
        Gyroscope,

        /// <summary>
        /// Magnetometer.
        /// </summary>
        Magnetometer,
    }
}
