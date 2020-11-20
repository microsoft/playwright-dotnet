using System;
using System.Collections.Generic;
using System.Text;
using PlaywrightSharp.Input;

namespace PlaywrightSharp
{
    /// <summary>
    /// Describes various options for <see cref="IPage.TapAsync(string, FrameTapOptions)"/>.
    /// </summary>
    public class FrameTapOptions
    {
        /// <summary>
        /// Gets or sets whether to bypass the actionability checks. Defaults to false.
        /// </summary>
        public bool Force { get; set; } = false;

        /// <summary>
        /// Actions that initiate navigations are waiting for these navigations to happen and
        /// for pages to start loading. You can opt out of waiting via setting this flag.
        /// You would only need this option in the exceptional cases such as navigating to inaccessible pages.
        /// Defaults to false.
        /// </summary>
        public bool NoWaitAfter { get; set; } = false;

        /// <summary>
        /// Gets or sets Modifier keys to press. Ensures that only these modifiers are pressed during the tap,
        /// and then restores current modifiers back. If not specified, currently pressed modifiers are used.
        /// </summary>
        public Modifier[] Modifiers { get; set; }

        /// <summary>
        /// A point to tap relative to the top-left corner of element padding box.
        /// If not specified, taps some visible point of the element.
        /// </summary>
        public (int x, int y)? Position { get; set; }

        /// <summary>
        /// Maximum time in milliseconds, defaults to 30 seconds, pass 0 to disable timeout. The default value
        /// can be changed by using the <see cref="IBrowserContext.DefaultTimeout"/> or or <see cref="IPage.DefaultTimeout"/>.
        /// </summary>
        public int Timeout { get; set; }
    }
}
