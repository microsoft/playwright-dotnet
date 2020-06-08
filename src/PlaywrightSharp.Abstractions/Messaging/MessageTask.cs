using System.Threading.Tasks;
using PlaywrightSharp.Protocol;

namespace PlaywrightSharp.Messaging
{
    internal class MessageTask<TProtocolResponse> : IMessageTask
        where TProtocolResponse : IProtocolResponse
    {
        public TaskCompletionSource<TProtocolResponse> TaskWrapper { get; set; }

        public string Method { get; set; }
    }
}
