using PlaywrightSharp.Protocol;

namespace PlaywrightSharp.Firefox.Protocol
{
    /// <summary>
    /// Basic class for sending requests to firefox using the devtools protocol.
    /// </summary>
    /// <typeparam name="TFirefoxResponse">The response type.</typeparam>
    internal interface IFirefoxRequest<out TFirefoxResponse> : IProtocolRequest<TFirefoxResponse>
        where TFirefoxResponse : IFirefoxResponse
    {
    }
}
