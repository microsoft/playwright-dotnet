using System;

namespace PlaywrightSharp
{
    /// <summary>
    /// <see cref="IPage.Dialog"/> arguments.
    /// </summary>
    public class DialogEventArgs : EventArgs
    {
        /// <summary>
        /// Dialog data.
        /// </summary>
        /// <value>Dialog data.</value>
        public IDialog Dialog { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DialogEventArgs"/> class.
        /// </summary>
        /// <param name="dialog">Dialog.</param>
        public DialogEventArgs(IDialog dialog) => Dialog = dialog;
    }
}