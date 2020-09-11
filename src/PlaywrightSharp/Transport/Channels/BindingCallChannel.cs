using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PlaywrightSharp.Helpers;

namespace PlaywrightSharp.Transport.Channels
{
    internal class BindingCallChannel : Channel<BindingCall>
    {
        public BindingCallChannel(string guid, ConnectionScope scope, BindingCall owner) : base(guid, scope, owner)
        {
        }

        internal Task ResolveAsync(object result)
            => Scope.SendMessageToServer<ResponseChannel>(
                Guid,
                "resolve",
                new Dictionary<string, object>
                {
                    ["result"] = ScriptsHelper.SerializedArgument(result),
                });

        internal Task RejectAsync(Exception ex)
            => Scope.SendMessageToServer<ResponseChannel>(
                Guid,
                "reject",
                new Dictionary<string, object>
                {
                    ["error"] = new
                    {
                        message = ex.Message,
                        stack = ex.StackTrace,
                        name = ex.GetType().Name,
                    },
                });
    }
}
