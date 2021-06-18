using System.Text.Json;
using System.Threading.Tasks;

namespace Microsoft.Playwright.Transport
{
    internal class ConnectionCallback
    {
        public TaskCompletionSource<JsonElement?> TaskCompletionSource { get; set; }
    }
}
