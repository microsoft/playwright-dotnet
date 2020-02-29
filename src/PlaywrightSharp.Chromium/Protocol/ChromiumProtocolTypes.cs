using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace PlaywrightSharp.Chromium.Protocol
{
    internal static class ChromiumProtocolTypes
    {
        private static readonly IDictionary<string, Type> ChromiumResponseMapper = new Dictionary<string, Type>();
        private static readonly IDictionary<string, Type> ChromiumEventsMapper = new Dictionary<string, Type>();

        static ChromiumProtocolTypes()
        {
            var chromiumResponse = typeof(IChromiumResponse);
            var chromiumRequest = typeof(IChromiumRequest<>);
            var chromiumEvent = typeof(IChromiumEvent);

            var types = typeof(ChromiumProtocolTypes).Assembly.GetTypes();

            var responses = types.Where(type => type.IsClass && chromiumResponse.IsAssignableFrom(type)).ToDictionary(type => type.Name);
            var requests = types.Where(type => type.IsClass && type.GetInterface(chromiumRequest.Name) != null).ToDictionary(type => type.Name);

            foreach (string name in responses.Keys)
            {
                // .Replace doesn't work since the class name might contain the work Response
                string requestName = name.Substring(0, name.Length - "Response".Length) + "Request";
                var type = requests[requestName];

                var request = (IChromiumRequest<IChromiumResponse>)Activator.CreateInstance(type);

                ChromiumResponseMapper.Add(request.Command, responses[name]);
            }

            foreach (var type in types.Where(type => !type.IsAbstract && type.IsClass && chromiumEvent.IsAssignableFrom(type)))
            {
                var eventObj = (IChromiumEvent)Activator.CreateInstance(type);

                ChromiumEventsMapper.Add(eventObj.InternalName, type);
            }
        }

        public static IChromiumResponse ParseResponse(string command, string json) => (IChromiumResponse)JsonSerializer.Deserialize(json, ChromiumResponseMapper[command]);

        public static IChromiumEvent ParseEvent(string method, string json) => (IChromiumEvent)JsonSerializer.Deserialize(json, ChromiumEventsMapper[method]);
    }
}
