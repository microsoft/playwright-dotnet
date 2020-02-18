using System.Text.Json;
using System.Threading.Tasks;

namespace PlaywrightSharp.Chromium
{
    internal class MessageTask
    {
        internal TaskCompletionSource<JsonElement?> TaskWrapper { get; set; }

        internal string Method { get; set; }
    }
}