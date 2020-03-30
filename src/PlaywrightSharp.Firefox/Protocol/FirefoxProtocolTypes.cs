using PlaywrightSharp.Firefox.Helper;
using PlaywrightSharp.Protocol;

namespace PlaywrightSharp.Firefox.Protocol
{
    internal static class FirefoxProtocolTypes
    {
        public static IFirefoxResponse ParseResponse(string command, string json)
            => ProtocolTypes<IFirefoxRequest<IFirefoxResponse>, IFirefoxResponse, IFirefoxEvent>.ParseResponse(command, json ?? "{}", JsonHelper.DefaultJsonSerializerOptions);

        public static IFirefoxEvent ParseEvent(string method, string json)
            => ProtocolTypes<IFirefoxRequest<IFirefoxResponse>, IFirefoxResponse, IFirefoxEvent>.ParseEvent(method, json, JsonHelper.DefaultJsonSerializerOptions);
    }
}
