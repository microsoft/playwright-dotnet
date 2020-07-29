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

        public Task ClickAsync(double x, double y, int delay = 0, MouseButton button = MouseButton.Left, int clickCount = 1)
            => _channel.MouseClickAsync(x, y, delay, button, clickCount);

        public Task DoubleClickAsync(double x, double y, ClickOptions options = null)
        {
            throw new System.NotImplementedException();
        }

        public Task DownAsync(MouseButton button = MouseButton.Left, int clickCount = 1) => _channel.MouseDownAsync(button, clickCount);

        public Task MoveAsync(double x, double y, int? steps = null) => _channel.MouseMoveAsync(x, y, steps);

        public Task UpAsync(MouseButton button = MouseButton.Left, int clickCount = 1) => _channel.MouseUpAsync(button, clickCount);

        public Task WheelAsync(double deltaX, double deltaY)
        {
            throw new System.NotImplementedException();
        }
    }
}
