using System.Drawing;
using PlaywrightSharp.Input;

namespace PlaywrightSharp
{
    /// <summary>
    /// Options to use when clicking.
    /// </summary>
    public class ClickOptions : WaitForSelectorOptions, IPointerActionOptions
    {
        /// <summary>
        /// Time to wait between <c>mousedown</c> and <c>mouseup</c> in milliseconds. Defaults to 0.
        /// </summary>
        public int Delay { get; set; } = 0;

        /// <summary>
        /// Defaults to 1. See https://developer.mozilla.org/en-US/docs/Web/API/UIEvent/detail.
        /// </summary>
        public int ClickCount { get; set; } = 1;

        /// <summary>
        /// The button to use for the click. Defaults to <see cref="MouseButton.Left"/>.
        /// </summary>
        public MouseButton Button { get; set; } = MouseButton.Left;

        /// <summary>
        /// A point to click relative to the top-left corner of element padding box. If not specified, clicks to some visible point of the element.
        /// </summary>
        public Point? Position { get; set; }

        /// <summary>
        /// Modifier keys to press. Ensures that only these modifiers are pressed during the click, and then restores current modifiers back. If not specified, currently pressed modifiers are used.
        /// </summary>
        public Modifier[] Modifiers { get; set; }

        /// <summary>
        /// Whether to pass the accionability checks.
        /// </summary>
        public bool Force { get; set; }

        /// <summary>
        /// Actions that initiate navigations are waiting for these navigations to happen and for pages to start loading.
        /// You can opt out of waiting via setting this flag. You would only need this option in the exceptional cases such as navigating to inaccessible pages. Defaults to false.
        /// </summary>
        public bool NoWaitAfter { get; set; }

        internal ClickOptions WithClickCount(int clickCount) => new ClickOptions
        {
            Delay = Delay,
            ClickCount = clickCount,
            Button = Button,
            Position = Position,
            Modifiers = Modifiers,
        };
    }
}
