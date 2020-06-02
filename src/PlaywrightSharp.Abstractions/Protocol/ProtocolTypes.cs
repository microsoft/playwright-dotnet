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
        private static readonly IDictionary<string, Type> ResponsesMapper = new Dictionary<string, Type>();
        private static readonly IDictionary<string, Type> EventsMapper = new Dictionary<string, Type>();

        static ProtocolTypes()
        {
            var protocolResponse = typeof(TProtocolResponse);
            var protocolRequest = typeof(TProtocolRequest);
            var protocolEvent = typeof(TProtocolEvent);

            var types = typeof(TProtocolRequest).Assembly.GetTypes();

            var responses = types.Where(type => type.IsClass && protocolResponse.IsAssignableFrom(type)).ToDictionary(type => type.Name);
            var requests = types.Where(type => type.IsClass && type.GetInterface(protocolRequest.Name) != null).ToDictionary(type => type.Name);

            foreach (string name in responses.Keys)
            {
                // .Replace doesn't work since the class name might contain the word Response
                string requestName = name.Substring(0, name.Length - "Response".Length) + "Request";
                var type = requests[requestName];

                var request = (TProtocolRequest)Activator.CreateInstance(type);

                ResponsesMapper.Add(request.Command, responses[name]);
            }

            foreach (var type in types.Where(type => !type.IsAbstract && type.IsClass && protocolEvent.IsAssignableFrom(type)))
            {
                var eventObj = (TProtocolEvent)Activator.CreateInstance(type);

                EventsMapper.Add(eventObj.InternalName, type);
            }
        }

        public static TProtocolResponse ParseResponse(string command, string json, JsonSerializerOptions options = null)
            => ResponsesMapper.TryGetValue(command, out var type)
                ? (TProtocolResponse)JsonSerializer.Deserialize(json, type, options ?? JsonHelper.DefaultJsonSerializerOptions)
                : default;

        public static TProtocolEvent ParseEvent(string method, string json, JsonSerializerOptions options = null)
            => EventsMapper.TryGetValue(method, out var type)
                ? (TProtocolEvent)JsonSerializer.Deserialize(json, type, options ?? JsonHelper.DefaultJsonSerializerOptions)
                : default;
    }
}
