using System.Threading.Tasks;
using PlaywrightSharp.Firefox.Protocol;

namespace PlaywrightSharp.Firefox
{
    internal class MessageTask
    {
        internal TaskCompletionSource<IFirefoxResponse> TaskWrapper { get; set; }

        internal string Method { get; set; }
    }
}
