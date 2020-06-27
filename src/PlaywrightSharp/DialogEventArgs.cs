using System;

namespace PlaywrightSharp
{
    /// <summary>
    /// <see cref="IPage.Dialog"/> arguments.
    /// </summary>
    public class DialogEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DialogEventArgs"/> class.
        /// </summary>
        /// <param name="dialog">Dialog.</param>
        public DialogEventArgs(IDialog dialog) => Dialog = dialog;

        /// <summary>
        /// Dialog data.
        /// </summary>
        public IDialog Dialog { get; }
    }
}
