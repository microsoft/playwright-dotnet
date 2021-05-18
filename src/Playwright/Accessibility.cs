using System.Threading.Tasks;
using Microsoft.Playwright.Transport.Channels;

namespace Microsoft.Playwright
{
    internal partial class Accessibility : IAccessibility
    {
        private readonly PageChannel _channel;

        public Accessibility(PageChannel channel)
        {
            _channel = channel;
        }

        public Task<AccessibilitySnapshotResult> SnapshotAsync(bool? interestingOnly = null, IElementHandle root = null)
            => _channel.AccessibilitySnapshotAsync(interestingOnly, (root as ElementHandle)?.ElementChannel);
    }
}
