/*
 * MIT License
 *
 * Copyright (c) Microsoft Corporation.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Playwright.Transport;
using Microsoft.Playwright.Transport.Channels;
using Microsoft.Playwright.Transport.Protocol;

namespace Microsoft.Playwright.Core;

internal class BindingCall : ChannelOwnerBase, IChannelOwner<BindingCall>
{
    private static readonly Type VoidTaskResultType = Type.GetType("System.Threading.Tasks.VoidTaskResult");

    private readonly BindingCallChannel _channel;
    private readonly BindingCallInitializer _initializer;

    public BindingCall(IChannelOwner parent, string guid, BindingCallInitializer initializer) : base(parent, guid)
    {
        _channel = new(guid, parent.Connection, this);
        _initializer = initializer;
    }

    public string Name => _initializer.Name;

    ChannelBase IChannelOwner.Channel => _channel;

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

            if (methodParams.Length == 1 && methodParams[0] == typeof(IJSHandle))
            {
                args.Add(_initializer.Handle);
            }
            else
            {
                for (int i = 0; i < methodParams.Length; i++)
                {
                    args.Add(ScriptsHelper.ParseEvaluateResult(_initializer.Args[i], methodParams[i]));
                }
            }

            object result = binding.DynamicInvoke(args.ToArray());

            if (result is Task taskResult)
            {
                result = null;

                await taskResult.ConfigureAwait(false);

                var taskResultType = taskResult.GetType();
                if (taskResultType.IsGenericType && taskResultType.GenericTypeArguments[0] != VoidTaskResultType)
                {
                    // the task is already awaited and therefore the call to property Result will not deadlock
                    result = taskResult.GetType().GetProperty(taskResultPropertyName).GetValue(taskResult);
                }
            }

            await _channel.ResolveAsync(ScriptsHelper.SerializedArgument(result)).ConfigureAwait(false);
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
