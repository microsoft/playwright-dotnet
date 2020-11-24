using System.Drawing;
using System.Threading.Tasks;
using PlaywrightSharp.Transport.Channels;

namespace PlaywrightSharp
{
    internal class Touchscreen : ITouchscreen
    {
        private readonly PageChannel _channel;

        public Touchscreen(PageChannel channel)
        {
            _channel = channel;
        }

        public Task TapAsync(Point point) => _channel.TouchscreenTapAsync(point);
    }
}
