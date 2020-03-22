using PlaywrightSharp.Protocol;

namespace PlaywrightSharp.Chromium.Protocol
{
    internal static class ChromiumProtocolTypes
    {
        public static IChromiumResponse ParseResponse(string command, string json)
            => ProtocolTypes<IChromiumRequest<IChromiumResponse>, IChromiumResponse, IChromiumEvent>.ParseResponse(command, json);

        public static IChromiumEvent ParseEvent(string method, string json)
            => ProtocolTypes<IChromiumRequest<IChromiumResponse>, IChromiumResponse, IChromiumEvent>.ParseEvent(method, json);
    }
}
