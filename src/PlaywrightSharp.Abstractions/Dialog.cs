using System;
using System.Threading.Tasks;

namespace PlaywrightSharp
{
    /// <inheritdoc cref="IDialog"/>
    public class Dialog : IDialog
    {
        private readonly Func<bool, string, Task> _onHandle;
        private bool _handled;

        internal Dialog(DialogType dialogType, string message, Func<bool, string, Task> onHandle, string defaultValue)
        {
            _onHandle = onHandle;
            DialogType = dialogType;
            Message = message;
            DefaultValue = defaultValue;
        }

        /// <inheritdoc cref="IDialog.DialogType"/>
        public DialogType DialogType { get; set; }

        /// <inheritdoc cref="IDialog.DefaultValue"/>
        public string DefaultValue { get; set; }

        /// <inheritdoc cref="IDialog.Message"/>
        public string Message { get; set; }

        /// <inheritdoc cref="IDialog.AcceptAsync(string)"/>
        public Task AcceptAsync(string promptText = "")
        {
            if (_handled)
            {
                throw new PlaywrightSharpException("Cannot accept dialog which is already handled!");
            }

            _handled = true;
            return _onHandle(true, promptText);
        }

        /// <inheritdoc cref="IDialog.DismissAsync"/>
        public Task DismissAsync()
        {
            if (_handled)
            {
                throw new PlaywrightSharpException("Cannot accept dialog which is already handled!");
            }

            _handled = true;
            return _onHandle(false, null);
        }
    }
}
