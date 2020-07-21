using System;
using System.Collections.Generic;

namespace PlaywrightSharp.Transport.Channels
{
    internal class BindingCallChannel : Channel<BindingCall>
    {
        public BindingCallChannel(string guid, ConnectionScope scope, BindingCall owner) : base(guid, scope, owner)
        {
        }

        internal void ResolveAsync(object result)
            => Scope.SendMessageToServer<ResponseChannel>(
                Guid,
                "resolve",
                new Dictionary<string, object>
                {
                    ["result"] = result,
                });

        internal void RejectAsync(Exception ex)
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
