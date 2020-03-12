using PlaywrightSharp.Protocol;

namespace PlaywrightSharp.Chromium.Protocol
{
    /// <summary>
    /// Basic structure for sending requests to chromium using the devtools protocol.
    /// </summary>
    /// <typeparam name="TChromiumResponse">The response type.</typeparam>
    internal interface IChromiumRequest<out TChromiumResponse> : IProtocolRequest<IChromiumResponse>
        where TChromiumResponse : IChromiumResponse
    {
    }
}
