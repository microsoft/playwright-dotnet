using PlaywrightSharp.Protocol;

namespace PlaywrightSharp.Webkit.Protocol
{
    /// <summary>
    /// Base type for webkit protocol request type.
    /// </summary>
    /// <typeparam name="TWebkitResponse">The protocol response type for a specific request.</typeparam>
    internal interface IWebkitRequest<TWebkitResponse> : IProtocolRequest<TWebkitResponse>
        where TWebkitResponse : IWebkitResponse
    {
    }
}
