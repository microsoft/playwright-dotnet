using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PlaywrightSharp.Input
{
    /// <inheritdoc cref="IKeyboard"/>
    public class Keyboard : IKeyboard
    {
        private static readonly Modifier[] Modifiers = { Modifier.Alt, Modifier.Control, Modifier.Meta, Modifier.Shift };

        private readonly IRawKeyboard _raw;
        private readonly List<Modifier> _pressedModifiers = new List<Modifier>();
        private readonly ISet<string> _pressedKeys = new HashSet<string>();

        internal Keyboard(IRawKeyboard rawKeyboard)
        {
            _raw = rawKeyboard;
        }

        /// <inheritdoc cref="IKeyboard.Modifiers"/>
        Modifier[] IKeyboard.Modifiers => _pressedModifiers.ToArray();

        /// <inheritdoc cref="IKeyboard.DownAsync(string, DownOptions)"/>
        public Task DownAsync(string key, DownOptions options = null)
        {
            var description = GetKeyDescriptionForString(key);
            bool autoRepeat = _pressedKeys.Contains(description.Code);
            _pressedKeys.Add(key);
            if (Enum.TryParse<Modifier>(key, out var modifier))
            {
                _pressedModifiers.Add(modifier);
            }

            string text = options?.Text ?? description.Text;
            return _raw.KeyDownAsync(_pressedModifiers.ToArray(), description.Code, description.KeyCode, description.KeyCodeWithoutLocation, description.Key, description.Location, autoRepeat, text);
        }

        /// <inheritdoc cref="IKeyboard.EnsureModifiersAsync(Modifier[])"/>
        async Task<Modifier[]> IKeyboard.EnsureModifiersAsync(Modifier[] modifiers)
        {
            var restore = _pressedModifiers.ToArray();
            var tasks = new List<Task>();
            foreach (var key in Modifiers)
            {
                bool needDown = modifiers.Contains(key);
                bool isDown = _pressedModifiers.Contains(key);
                if (needDown && !isDown)
                {
                    tasks.Add(DownAsync(key.ToString()));
                }
                else if (!needDown && isDown)
                {
                    tasks.Add(UpAsync(key.ToString()));
                }
            }

            await Task.WhenAll(tasks.ToArray()).ConfigureAwait(false);
            return restore;
        }

        /// <inheritdoc cref="IKeyboard.PressAsync(string, PressOptions)"/>
        public async Task PressAsync(string key, PressOptions options = null)
        {
            int delay = options?.Delay ?? 0;
            await DownAsync(key, options).ConfigureAwait(false);
            if (delay != 0)
            {
                await Task.Delay(delay).ConfigureAwait(false);
            }

            await UpAsync(key).ConfigureAwait(false);
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
            var description = GetKeyDescriptionForString(key);
            if (Enum.TryParse<Modifier>(key, out var modifier))
            {
                _pressedModifiers.Remove(modifier);
            }

            _pressedKeys.Remove(key);
            return _raw.KeyUpAsync(_pressedModifiers.ToArray(), description.Code, description.KeyCode, description.KeyCodeWithoutLocation, description.Key, description.Location);
        }

        private KeyDescription GetKeyDescriptionForString(string keyString)
        {
            bool shift = _pressedModifiers.Contains(Modifier.Shift);
            var description = new KeyDescription
            {
                Key = string.Empty,
                KeyCode = 0,
                KeyCodeWithoutLocation = 0,
                Code = string.Empty,
                Text = string.Empty,
                Location = 0,
            };

            var definition = KeyDefinitions.Get(keyString);
            if (!string.IsNullOrEmpty(definition.Key))
            {
                description.Key = definition.Key;
            }

            if (shift && !string.IsNullOrEmpty(definition.ShiftKey))
            {
                description.Key = definition.ShiftKey;
            }

            if (definition.KeyCode > 0)
            {
                description.KeyCode = definition.KeyCode.Value;
            }

            if (shift && definition.ShiftKeyCode != null)
            {
                description.KeyCode = (int)definition.ShiftKeyCode;
            }

            if (!string.IsNullOrEmpty(definition.Code))
            {
                description.Code = definition.Code;
            }

            if (definition.Location != 0)
            {
                description.Location = definition.Location.Value;
            }

            if (description.Key.Length == 1)
            {
                description.Text = description.Key;
            }

            if (!string.IsNullOrEmpty(definition.Text))
            {
                description.Text = definition.Text;
            }

            if (shift && !string.IsNullOrEmpty(definition.ShiftText))
            {
                description.Text = definition.ShiftText;
            }

            // if any modifiers besides shift are pressed, no text should be sent
            if (_pressedModifiers.Count > 1 || (!shift && _pressedModifiers.Count == 1))
            {
                description.Text = string.Empty;
            }

            if (definition.KeyCodeWithoutLocation != null)
            {
                description.KeyCodeWithoutLocation = definition.KeyCodeWithoutLocation.Value;
            }
            else
            {
                description.KeyCodeWithoutLocation = definition.KeyCode.GetValueOrDefault();
            }

            return description;
        }
    }
}
