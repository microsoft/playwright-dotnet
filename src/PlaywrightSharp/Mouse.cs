using System.Threading.Tasks;
using PlaywrightSharp.Input;
using PlaywrightSharp.Transport.Channels;

namespace PlaywrightSharp
{
    internal class Mouse : IMouse
    {
        private readonly PageChannel _channel;

        public Mouse(PageChannel channel)
        {
            _channel = channel;
        }

        public Task ClickAsync(decimal x, decimal y, int delay = 0, MouseButton button = MouseButton.Left, int clickCount = 1)
            => _channel.MouseClickAsync(x, y, delay, button, clickCount);

        public Task DblClickAsync(decimal x, decimal y, int delay = 0, MouseButton button = MouseButton.Left)
            => _channel.MouseClickAsync(x, y, delay, button, 2);

        public Task DownAsync(MouseButton button = MouseButton.Left, int clickCount = 1) => _channel.MouseDownAsync(button, clickCount);

        public Task MoveAsync(decimal x, decimal y, int? steps = 1) => _channel.MouseMoveAsync(x, y, steps);

        public Task UpAsync(MouseButton button = MouseButton.Left, int clickCount = 1) => _channel.MouseUpAsync(button, clickCount);
    }
}
