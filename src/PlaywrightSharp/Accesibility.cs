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

        public Task<SerializedAXNode> SnapshotAsync(bool interestingOnly = true, IElementHandle root = null)
            => _channel.AccessibilitySnapshotAsync(interestingOnly, (root as ElementHandle)?.ElementChannel);
    }
}
