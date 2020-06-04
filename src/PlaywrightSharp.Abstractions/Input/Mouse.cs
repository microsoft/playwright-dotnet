using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PlaywrightSharp.Input
{
    /// <inheritdoc cref="IMouse"/>
    public class Mouse : IMouse
    {
        private readonly IRawMouse _raw;
        private readonly IKeyboard _keyboard;
        private readonly List<MouseButton> _buttons = new List<MouseButton>();
        private double _x = 0;
        private double _y = 0;
        private MouseButton _lastButton;

        internal Mouse(IRawMouse rawMouse, IKeyboard keyboard)
        {
            _raw = rawMouse;
            _keyboard = keyboard;
        }

        /// <inheritdoc cref="IMouse.ClickAsync(double, double, ClickOptions)"/>
        public async Task ClickAsync(double x, double y, ClickOptions options = null)
        {
            options ??= new ClickOptions();
            if (options.Delay != 0)
            {
                await Task.WhenAll(
                  MoveAsync(x, y),
                  DownAsync(options)).ConfigureAwait(false);

                await Task.Delay(options.Delay).ConfigureAwait(false);

                await UpAsync(options).ConfigureAwait(false);
            }
            else
            {
                await Task.WhenAll(
                  MoveAsync(x, y),
                  DownAsync(options),
                  UpAsync(options)).ConfigureAwait(false);
            }
        }

        /// <inheritdoc cref="IMouse.DoubleClickAsync(double, double, ClickOptions)"/>
        public async Task DoubleClickAsync(double x, double y, ClickOptions options = null)
        {
            options ??= new ClickOptions();
            if (options.Delay != 0)
            {
                await MoveAsync(x, y).ConfigureAwait(false);
                await DownAsync(options?.WithClickCount(1)).ConfigureAwait(false);
                await Task.Delay(options.Delay).ConfigureAwait(false);
                await UpAsync(options?.WithClickCount(1)).ConfigureAwait(false);
                await Task.Delay(options.Delay).ConfigureAwait(false);
                await DownAsync(options?.WithClickCount(2)).ConfigureAwait(false);
                await Task.Delay(options.Delay).ConfigureAwait(false);
                await UpAsync(options?.WithClickCount(2)).ConfigureAwait(false);
            }
            else
            {
                await Task.WhenAll(
                    MoveAsync(x, y),
                    DownAsync(options?.WithClickCount(1)),
                    UpAsync(options?.WithClickCount(1)),
                    DownAsync(options?.WithClickCount(2)),
                    UpAsync(options?.WithClickCount(2))).ConfigureAwait(false);
            }
        }

        /// <inheritdoc cref="IMouse.TripleClickAsync(double, double, ClickOptions)"/>
        public async Task TripleClickAsync(double x, double y, ClickOptions options = null)
        {
            options ??= new ClickOptions();
            if (options.Delay != 0)
            {
                await MoveAsync(x, y).ConfigureAwait(false);
                await DownAsync(options?.WithClickCount(1)).ConfigureAwait(false);
                await Task.Delay(options.Delay).ConfigureAwait(false);
                await UpAsync(options?.WithClickCount(1)).ConfigureAwait(false);
                await Task.Delay(options.Delay).ConfigureAwait(false);
                await DownAsync(options?.WithClickCount(2)).ConfigureAwait(false);
                await Task.Delay(options.Delay).ConfigureAwait(false);
                await UpAsync(options?.WithClickCount(2)).ConfigureAwait(false);
                await Task.Delay(options.Delay).ConfigureAwait(false);
                await DownAsync(options?.WithClickCount(3)).ConfigureAwait(false);
                await Task.Delay(options.Delay).ConfigureAwait(false);
                await UpAsync(options?.WithClickCount(3)).ConfigureAwait(false);
            }
            else
            {
                await Task.WhenAll(
                    MoveAsync(x, y),
                    DownAsync(options?.WithClickCount(1)),
                    UpAsync(options?.WithClickCount(1)),
                    DownAsync(options?.WithClickCount(2)),
                    UpAsync(options?.WithClickCount(2)),
                    DownAsync(options?.WithClickCount(3)),
                    UpAsync(options?.WithClickCount(3))).ConfigureAwait(false);
            }
        }

        /// <inheritdoc cref="IMouse.DownAsync(ClickOptions)"/>
        public Task DownAsync(ClickOptions options = null)
        {
            var button = options?.Button ?? MouseButton.Left;
            int clickCount = options?.ClickCount ?? 1;

            _lastButton = button;
            _buttons.Add(button);

            return _raw.DownAsync(_x, _y, _lastButton, _buttons, _keyboard.Modifiers, clickCount);
        }

        /// <inheritdoc cref="IMouse.MoveAsync(double, double, MoveOptions)"/>
        public async Task MoveAsync(double x, double y, MoveOptions options = null)
        {
            int steps = options?.Steps ?? 1;
            double fromX = _x;
            double fromY = _y;
            _x = x;
            _y = y;

            for (int i = 1; i <= steps; i++)
            {
                double middleX = fromX + ((x - fromX) * ((double)i / steps));
                double middleY = fromY + ((y - fromY) * ((double)i / steps));
                await _raw.MoveAsync(middleX, middleY, _lastButton, _buttons, _keyboard.Modifiers).ConfigureAwait(false);
            }
        }

        /// <inheritdoc cref="IMouse.UpAsync(ClickOptions)"/>
        public Task UpAsync(ClickOptions options = null)
        {
            var button = options?.Button ?? MouseButton.Left;
            int clickCount = options?.ClickCount ?? 1;

            _lastButton = MouseButton.None;
            _buttons.Remove(button);
            return _raw.UpAsync(_x, _y, button, _buttons, _keyboard.Modifiers, clickCount);
        }

        /// <inheritdoc cref="IMouse.WheelAsync(double, double)"/>
        public Task WheelAsync(double deltaX, double deltaY)
        {
            throw new NotImplementedException();
        }
    }
}
