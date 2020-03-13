using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PlaywrightSharp.Input
{
    internal class Mouse : IMouse
    {
        private readonly IRawMouse _raw;
        private readonly IKeyboard _keyboard;
        private readonly List<MouseButton> _buttons = new List<MouseButton>();
        private double _x = 0;
        private double _y = 0;
        private MouseButton _lastButton;

        public Mouse(IRawMouse rawMouse, IKeyboard keyboard)
        {
            _raw = rawMouse;
            _keyboard = keyboard;
        }

        public async Task ClickAsync(double x, double y, ClickOptions options = null)
        {
            if ((options?.Delay ?? 0) != 0)
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

        public Task DownAsync(ClickOptions options = null)
        {
            var button = options?.Button ?? MouseButton.Left;
            int clickCount = options?.ClickCount ?? 1;

            _lastButton = button;
            _buttons.Add(button);

            return _raw.DownAsync(_x, _y, _lastButton, _buttons, _keyboard.Modifiers, clickCount);
        }

        public async Task MoveAsync(double x, double y, MoveOptions options = null)
        {
            int steps = options?.Steps ?? 1;
            double fromX = _x;
            double fromY = _y;
            _x = x;
            _y = y;

            for (int i = 1; i <= steps; i++)
            {
                double middleX = fromX + ((x - fromX) * (i / steps));
                double middleY = fromY + ((y - fromY) * (i / steps));
                await _raw.MoveAsync(middleX, middleY, _lastButton, _buttons, _keyboard.Modifiers).ConfigureAwait(false);
            }
        }

        public Task UpAsync(ClickOptions options = null)
        {
            var button = options?.Button ?? MouseButton.Left;
            int clickCount = options?.ClickCount ?? 1;

            _lastButton = MouseButton.None;
            _buttons.Remove(button);
            return _raw.UpAsync(_x, _y, button, _buttons, _keyboard.Modifiers, clickCount);
        }

        public Task WheelAsync(double deltaX, double deltaY)
        {
            throw new NotImplementedException();
        }
    }
}
