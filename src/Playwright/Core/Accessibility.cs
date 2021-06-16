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
            return SnapshotAsync(interestingOnly: options.InterestingOnly, root: options.Root);
        }

        internal Task<JsonElement?> SnapshotAsync(bool? interestingOnly = null, IElementHandle root = null)
            => _channel.AccessibilitySnapshotAsync(interestingOnly, (root as ElementHandle)?.ElementChannel);
    }
}
