using System.Threading.Tasks;
using PlaywrightSharp.Transport.Channels;

namespace PlaywrightSharp
{
    internal class Accesibility : IAccessibility
    {
        private readonly PageChannel _channel;

        public Accesibility(PageChannel channel)
        {
            _channel = channel;
        }

        public Task<SerializedAXNode> SnapshotAsync(AccessibilitySnapshotOptions options = null)
        {
            var root = (options?.Root as ElementHandle)?.ElementChannel;
            return _channel.AccessibilitySnapshotAsync(options?.InterestingOnly, root);
        }
    }
}