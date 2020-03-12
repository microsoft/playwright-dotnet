using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using PlaywrightSharp.Helpers;

namespace PlaywrightSharp.Protocol
{
    internal static class ProtocolTypes<TProtocolRequest, TProtocolResponse, TProtocolEvent>
        where TProtocolRequest : IProtocolRequest<TProtocolResponse>
        where TProtocolResponse : IProtocolResponse
        where TProtocolEvent : IProtocolEvent
    {
        private static readonly IDictionary<string, Type> ChromiumResponseMapper = new Dictionary<string, Type>();
        private static readonly IDictionary<string, Type> ChromiumEventsMapper = new Dictionary<string, Type>();

        static ProtocolTypes()
        {
            var chromiumResponse = typeof(TProtocolResponse);
            var chromiumRequest = typeof(TProtocolRequest);
            var chromiumEvent = typeof(TProtocolEvent);

            var types = typeof(TProtocolRequest).Assembly.GetTypes();

            var responses = types.Where(type => type.IsClass && chromiumResponse.IsAssignableFrom(type)).ToDictionary(type => type.Name);
            var requests = types.Where(type => type.IsClass && type.GetInterface(chromiumRequest.Name) != null).ToDictionary(type => type.Name);

            foreach (string name in responses.Keys)
            {
                // .Replace doesn't work since the class name might contain the word Response
                string requestName = name.Substring(0, name.Length - "Response".Length) + "Request";
                var type = requests[requestName];

                var request = (TProtocolRequest)Activator.CreateInstance(type);

                ChromiumResponseMapper.Add(request.Command, responses[name]);
            }

            foreach (var type in types.Where(type => !type.IsAbstract && type.IsClass && chromiumEvent.IsAssignableFrom(type)))
            {
                var eventObj = (TProtocolEvent)Activator.CreateInstance(type);

                ChromiumEventsMapper.Add(eventObj.InternalName, type);
            }
        }

        public static TProtocolResponse ParseResponse(string command, string json)
            => (TProtocolResponse)JsonSerializer.Deserialize(json, ChromiumResponseMapper[command], JsonHelper.DefaultJsonSerializerOptions);

        public static TProtocolEvent ParseEvent(string method, string json)
            => (TProtocolEvent)JsonSerializer.Deserialize(json, ChromiumEventsMapper[method], JsonHelper.DefaultJsonSerializerOptions);
    }
}
