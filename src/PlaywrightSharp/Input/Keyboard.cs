using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PlaywrightSharp.Input
{
    internal class Keyboard : IKeyboard
    {
        private readonly IRawKeyboard _raw;
        private readonly List<Modifier> _pressedModifiers = new List<Modifier>();

        public Keyboard(IRawKeyboard rawKeyboard)
        {
            _raw = rawKeyboard;
        }

        public Modifier[] Modifiers => _pressedModifiers.ToArray();

        public Task DownAsync(string key, DownOptions options = null)
        {
            throw new NotImplementedException();
        }

        public Task<Modifier[]> EnsureModifiersAsync(Modifier[] modifiers)
        {
            throw new NotImplementedException();
        }

        public Task PressAsync(string key, PressOptions options = null)
        {
            throw new NotImplementedException();
        }

        public Task SendCharactersAsync(string charText)
        {
            throw new NotImplementedException();
        }

        public Task TypeAsync(string text, TypeOptions options = null)
        {
            throw new NotImplementedException();
        }

        public Task UpAsync(string key)
        {
            throw new NotImplementedException();
        }
    }
}
