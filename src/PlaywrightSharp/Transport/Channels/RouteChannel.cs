using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PlaywrightSharp.Transport.Protocol;

namespace PlaywrightSharp.Transport.Channels
{
    internal class RouteChannel : Channel<Route>
    {
        public RouteChannel(string guid, ConnectionScope scope, Route owner) : base(guid, scope, owner)
        {
        }

        public Task AbortAsync(RequestAbortErrorCode errorCode)
            => Scope.SendMessageToServer(
                Guid,
                "abort",
                new Dictionary<string, object>
                {
                    ["errorCode"] = errorCode,
                });

        public Task FulfillAsync(NormalizedFulfillResponse response)
            => Scope.SendMessageToServer(
                Guid,
                "fulfill",
                response);

        public Task ContinueAsync(RouteContinueOverrides overrides = null)
        {
            var args = new Dictionary<string, object>();

            if (overrides != null)
            {
                if (overrides.Method != null)
                {
                    args["method"] = overrides.Method;
                }

                if (!string.IsNullOrEmpty(overrides.PostData))
                {
                    args["postData"] = Convert.ToBase64String(Encoding.UTF8.GetBytes(overrides.PostData));
                }

                if (overrides.Headers != null)
                {
                    args["headers"] = overrides.Headers.Select(kv => new HeaderEntry { Name = kv.Key, Value = kv.Value }).ToArray();
                }
            }

            return Scope.SendMessageToServer(
                Guid,
                "continue",
                args,
                true);
        }
    }
}
