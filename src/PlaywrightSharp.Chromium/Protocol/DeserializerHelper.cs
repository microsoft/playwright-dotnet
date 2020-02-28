using System;
using System.Collections.Generic;
using System.Linq;

namespace PlaywrightSharp.Chromium.Protocol
{
    internal static class DeserializerHelper
    {
        private static readonly Dictionary<string, Type> ChromiumResponseMapper = new Dictionary<string, Type>();

        static DeserializerHelper()
        {
            var chromiumResponse = typeof(IChromiumResponse);
            var chromiumRequest = typeof(IChromiumRequest<>);

            var types = typeof(DeserializerHelper).Assembly.GetTypes();

            var responses = types.Where(type => type.IsClass && type.IsAssignableFrom(chromiumResponse)).ToDictionary(type => type.Name);
            var requests = types.Where(type => type.IsClass && type.IsAssignableFrom(chromiumRequest)).ToDictionary(type => type.Name);

            foreach (string name in responses.Keys)
            {
                string requestName = name.Replace("Response", "Request");
                var type = requests[requestName];
                var request = (IChromiumRequest<IChromiumResponse>)Activator.CreateInstance(type.MakeGenericType(responses[name]));

                ChromiumResponseMapper.Add(request.Command, responses[name]);
            }
        }

        public static Type GetRespnseType(string command) => ChromiumResponseMapper[command];
    }
}
