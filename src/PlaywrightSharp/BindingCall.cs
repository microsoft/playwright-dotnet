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
    internal class BindingCall : ChannelOwnerBase, IChannelOwner<BindingCall>
    {
        private readonly BindingCallChannel _channel;
        private readonly BindingCallInitializer _initializer;

        public BindingCall(IChannelOwner parent, string guid, BindingCallInitializer initializer) : base(parent, guid)
        {
            _channel = new BindingCallChannel(guid, parent.Connection, this);
            _initializer = initializer;
        }

        public string Name => _initializer.Name;

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
                        Context = _initializer?.Frame?.Page?.Context,
                        Page = _initializer?.Frame?.Page,
                        Frame = _initializer?.Frame,
                    },
                };

                for (int i = 0; i < methodParams.Length; i++)
                {
                    args.Add(ScriptsHelper.ParseEvaluateResult(_initializer.Args[i], methodParams[i]));
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

                await _channel.ResolveAsync(result).ConfigureAwait(false);
            }
            catch (TargetInvocationException ex)
            {
                await _channel.RejectAsync(ex.InnerException).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await _channel.RejectAsync(ex).ConfigureAwait(false);
            }
        }
    }
}
