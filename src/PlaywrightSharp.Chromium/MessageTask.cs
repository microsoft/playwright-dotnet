using System.Threading.Tasks;
using PlaywrightSharp.Chromium.Protocol;

namespace PlaywrightSharp.Chromium
{
    internal class MessageTask
    {
        internal TaskCompletionSource<IChromiumResponse> TaskWrapper { get; set; }

        internal string Method { get; set; }
    }
}
