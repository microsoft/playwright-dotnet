using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Playwright
{
    /// <summary>
    /// Contains all the constants relevant to permissions.
    /// <seealso cref="IBrowserContext.GrantPermissionsAsync(IEnumerable{string}, string)"/>
    /// </summary>
    public static class ContextPermissions
    {
        public const string Geolocation = "geolocation";

        public const string MIDI = "midi";

        public const string MIDISysex = "midi-sysex";

        public const string Notifications = "notifications";

        public const string Push = "push";

        public const string Camera = "camera";

        public const string Microphone = "microphone";

        public const string BackgroundSync = "background-sync";

        public const string AmbientLightSensor = "ambient-light-sensor";

        public const string Accelerometer = "accelerometer";

        public const string Gyroscope = "gyroscope";

        public const string Magnetometer = "magnetometer";

        public const string AccessibilityEvents = "accessibility-events";

        public const string ClipboardRead = "clipboard-read";

        public const string ClipboardWrite = "clipboard-write";

        public const string PaymentHandler = "payment-handler";
    }
}
