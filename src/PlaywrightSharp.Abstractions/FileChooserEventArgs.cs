using System;

namespace PlaywrightSharp
{
    /// <summary>
    /// see <see cref="IPage.FileChooser"/> arguments.
    /// </summary>
    public class FileChooserEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FileChooserEventArgs"/> class.
        /// </summary>
        /// <param name="element">The input element.</param>
        /// <param name="multiple">The multiple option.</param>
        public FileChooserEventArgs(IElementHandle element, bool multiple)
        {
            Element = element;
            Multiple = multiple;
        }

        /// <summary>
        /// Handle to the input element that was clicked.
        /// </summary>
        public IElementHandle Element { get; set; }

        /// <summary>
        /// Whether file chooser allow for multiple file selection.
        /// </summary>
        /// <see href="https://developer.mozilla.org/en-US/docs/Web/HTML/Element/input/file#attr-multiple"/>
        public bool Multiple { get; set; }
    }
}
