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
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Playwright.Helpers;
using Microsoft.Playwright.Transport;
using Microsoft.Playwright.Transport.Converters;
using Microsoft.Playwright.Transport.Protocol;

namespace Microsoft.Playwright.Core;

internal class BindingCall : ChannelOwner
{
    private readonly BindingCallInitializer _initializer;

    public BindingCall(ChannelOwner parent, string guid, BindingCallInitializer initializer) : base(parent, guid)
    {
        _initializer = initializer;
    }

    public string Name => _initializer.Name;

    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2070", Justification = "Task<T> result extraction on binding boundary.")]
    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2067", Justification = "Task<T> result extraction on binding boundary.")]
    private static object? ExtractTaskResult(Task task, Type taskType)
    {
        var resultProp = taskType.GetProperty("Result", BindingFlags.Public | BindingFlags.Instance);
        return resultProp?.GetValue(task);
    }

    internal async Task CallAsync(Delegate binding)
    {
        try
        {
            var methodParams = binding.Method.GetParameters().Skip(1).ToArray();
            var args = new List<object>
            {
                new BindingSource(_initializer.Frame.Page.Context, _initializer.Frame.Page, _initializer.Frame),
            };

            for (int i = 0; i < methodParams.Length; i++)
            {
                var argElement = _initializer.Args[i];
                args.Add(EvaluateArgumentValueConverter.Deserialize(argElement, methodParams[i].ParameterType)!);
            }

            object? result = binding.DynamicInvoke(args.ToArray());

            if (result is Task taskResult)
            {
                await taskResult.ConfigureAwait(false);
                var taskType = result.GetType();
                if (taskType.IsGenericType && taskType.GetGenericTypeDefinition() == typeof(Task<>))
                {
                    result = ExtractTaskResult(taskResult, taskType);
                }
                else
                {
                    result = null;
                }
            }

            await SendMessageToServerAsync("resolve", new Dictionary<string, object?>
            {
                ["result"] = ScriptsHelper.SerializedArgument(result),
            }).ConfigureAwait(false);
        }
        catch (TargetInvocationException ex)
        {
            await SendMessageToServerAsync(
                "reject",
                new Dictionary<string, object?>
                {
                    ["error"] = ex.InnerException!.ToObject(),
                }).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            await SendMessageToServerAsync(
                "reject",
                new Dictionary<string, object?>
                {
                    ["error"] = ex.ToObject(),
                }).ConfigureAwait(false);
        }
    }
}
