using System;

namespace PlaywrightSharp
{
    /// <summary>
    /// Arguments used by <see cref="IPage.Popup"/>.
    /// </summary>
    public class PopupEventArgs : EventArgs
    {
        internal PopupEventArgs(IPage page) => Page = page;

        /// <summary>
        /// Popup <see cref="IPage"/>.
        /// </summary>
        public IPage Page { get; }
    }
}
