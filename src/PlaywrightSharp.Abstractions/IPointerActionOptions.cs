using System.Drawing;
using PlaywrightSharp.Input;

namespace PlaywrightSharp
{
    /// <summary>
    /// Pointer options.
    /// </summary>
    /// <seealso cref="IElementHandle.HoverAsync"/>
    public interface IPointerActionOptions
    {
        /// <summary>
        /// A point to click relative to the top-left corner of element padding box. If not specified, clicks to some visible point of the element.
        /// </summary>
        Point? RelativePoint { get; set; }

        /// <summary>
        /// Modifier keys to press. Ensures that only these modifiers are pressed during the click, and then restores current modifiers back. If not specified, currently pressed modifiers are used.
        /// </summary>
        Modifier[] Modifiers { get; set; }
    }
}
