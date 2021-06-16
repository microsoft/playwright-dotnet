using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Playwright.Transport.Channels;

namespace Microsoft.Playwright.Core
{
    internal partial class Accessibility : IAccessibility
    {
        private readonly PageChannel _channel;

        public Accessibility(PageChannel channel)
        {
            _channel = channel;
        }

        public Task<JsonElement?> SnapshotAsync(AccessibilitySnapshotOptions options = default)
        {
            options ??= new AccessibilitySnapshotOptions();
            return _channel.AccessibilitySnapshotAsync(options.InterestingOnly, (options.Root as ElementHandle)?.ElementChannel);
        }
    }
}
