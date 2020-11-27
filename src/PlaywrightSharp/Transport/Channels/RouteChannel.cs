using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using PlaywrightSharp.Transport.Protocol;

namespace PlaywrightSharp.Transport.Channels
{
    internal class RouteChannel : Channel<Route>
    {
        public RouteChannel(string guid, Connection connection, Route owner) : base(guid, connection, owner)
        {
        }

        public Task AbortAsync(RequestAbortErrorCode errorCode)
            => Connection.SendMessageToServerAsync(
                Guid,
                "abort",
                new Dictionary<string, object>
                {
                    ["errorCode"] = errorCode,
                });

        public Task FulfillAsync(NormalizedFulfillResponse response)
            => Connection.SendMessageToServerAsync(
                Guid,
                "fulfill",
                response);

        public Task ContinueAsync(HttpMethod method, string postData, Dictionary<string, string> headers)
        {
            var args = new Dictionary<string, object>();

            if (method != null)
            {
                args["method"] = method;
            }

            if (!string.IsNullOrEmpty(postData))
            {
                args["postData"] = Convert.ToBase64String(Encoding.UTF8.GetBytes(postData));
            }

            if (headers != null)
            {
                args["headers"] = headers.Select(kv => new HeaderEntry { Name = kv.Key, Value = kv.Value }).ToArray();
            }

            return Connection.SendMessageToServerAsync(
                Guid,
                "continue",
                args,
                true);
        }
    }
}
