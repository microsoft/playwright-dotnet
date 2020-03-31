using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PlaywrightSharp.Input
{
    /// <inheritdoc cref="IKeyboard"/>
    public class Keyboard : IKeyboard
    {
        private readonly IRawKeyboard _raw;
        private readonly List<Modifier> _pressedModifiers = new List<Modifier>();

        internal Keyboard(IRawKeyboard rawKeyboard)
        {
            _raw = rawKeyboard;
        }

        /// <inheritdoc cref="IKeyboard.Modifiers"/>
        Modifier[] IKeyboard.Modifiers => _pressedModifiers.ToArray();

        /// <inheritdoc cref="IKeyboard.DownAsync(string, DownOptions)"/>
        public Task DownAsync(string key, DownOptions options = null)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IKeyboard.EnsureModifiersAsync(Modifier[])"/>
        Task<Modifier[]> IKeyboard.EnsureModifiersAsync(Modifier[] modifiers)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IKeyboard.PressAsync(string, PressOptions)"/>
        public Task PressAsync(string key, PressOptions options = null)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IKeyboard.SendCharactersAsync(string)"/>
        public Task SendCharactersAsync(string text) => _raw.SendTextAsync(text);

        /// <inheritdoc cref="IKeyboard.TypeAsync(string, TypeOptions)"/>
        public Task TypeAsync(string text, TypeOptions options = null)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc cref="IKeyboard.UpAsync(string)"/>
        public Task UpAsync(string key)
        {
            throw new NotImplementedException();
        }
    }
}
