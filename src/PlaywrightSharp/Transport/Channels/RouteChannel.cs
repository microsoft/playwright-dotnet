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

        public Task AbortAsync(string errorCode)
            => Connection.SendMessageToServerAsync(
                Guid,
                "abort",
                new Dictionary<string, object>
                {
                    ["errorCode"] = string.IsNullOrEmpty(errorCode) ? RequestAbortErrorCode.Failed : errorCode,
                });

        public Task FulfillAsync(NormalizedFulfillResponse response)
            => Connection.SendMessageToServerAsync(
                Guid,
                "fulfill",
                response);

        public Task ContinueAsync(string url, string method, byte[] postData, IEnumerable<KeyValuePair<string, string>> headers)
        {
            var args = new Dictionary<string, object>();

            if (url != null)
            {
                args["url"] = url;
            }

            if (method != null)
            {
                args["method"] = method;
            }

            if (postData != null)
            {
                args["postData"] = Convert.ToBase64String(postData);
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
