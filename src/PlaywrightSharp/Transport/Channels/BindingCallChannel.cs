using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PlaywrightSharp.Helpers;

namespace PlaywrightSharp.Transport.Channels
{
    internal class BindingCallChannel : Channel<BindingCall>
    {
        public BindingCallChannel(string guid, Connection connection, BindingCall owner) : base(guid, connection, owner)
        {
        }

        internal Task ResolveAsync(object result)
            => Connection.SendMessageToServerAsync<ResponseChannel>(
                Guid,
                "resolve",
                new Dictionary<string, object>
                {
                    ["result"] = ScriptsHelper.SerializedArgument(result),
                });

        internal Task RejectAsync(Exception ex)
            => Connection.SendMessageToServerAsync<ResponseChannel>(
                Guid,
                "reject",
                new Dictionary<string, object>
                {
                    ["error"] = new
                    {
                        error = new
                        {
                            message = ex.Message,
                            stack = ex.StackTrace,
                            name = ex.GetType().Name,
                        },
                    },
                });
    }
}
