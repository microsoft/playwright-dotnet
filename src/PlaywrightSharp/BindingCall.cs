using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using PlaywrightSharp.Helpers;
using PlaywrightSharp.Transport;
using PlaywrightSharp.Transport.Channels;
using PlaywrightSharp.Transport.Protocol;

namespace PlaywrightSharp
{
    internal class BindingCall : IChannelOwner<BindingCall>
    {
        private readonly ConnectionScope _scope;
        private readonly BindingCallChannel _channel;
        private readonly BindingCallInitializer _initializer;

        public BindingCall(ConnectionScope scope, string guid, BindingCallInitializer initializer)
        {
            _scope = scope;
            _channel = new BindingCallChannel(guid, scope, this);
            _initializer = initializer;
        }

        public string Name => _initializer.Name;

        /// <inheritdoc/>
        ConnectionScope IChannelOwner.Scope => _scope;

        /// <inheritdoc/>
        ChannelBase IChannelOwner.Channel => _channel;

        /// <inheritdoc/>
        IChannel<BindingCall> IChannelOwner<BindingCall>.Channel => _channel;

        internal async Task CallAsync(Delegate binding)
        {
            try
            {
                const string taskResultPropertyName = "Result";
                var methodParams = binding.Method.GetParameters().Select(parameter => parameter.ParameterType).Skip(1).ToArray();
                var args = new List<object>
                {
                    new BindingSource
                    {
                        Context = _initializer.Frame.Page.Context,
                        Page = _initializer.Frame.Page,
                        Frame = _initializer.Frame,
                    },
                };

                for (int i = 0; i < methodParams.Length; i++)
                {
                    args.Add(_initializer.Args[i].ToObject(methodParams[i], _scope.Connection.GetDefaultJsonSerializerOptions()));
                }

                object result = binding.DynamicInvoke(args.ToArray());

                if (result is Task taskResult)
                {
                    await taskResult.ConfigureAwait(false);

                    if (taskResult.GetType().IsGenericType)
                    {
                        // the task is already awaited and therefore the call to property Result will not deadlock
                        result = taskResult.GetType().GetProperty(taskResultPropertyName).GetValue(taskResult);
                    }
                }

                _channel.ResolveAsync(result);
            }
            catch (Exception ex)
            {
                _channel.RejectAsync(ex);
            }
        }
    }
}
