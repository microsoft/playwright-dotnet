using System;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using PlaywrightSharp.Chromium.Helpers;
using PlaywrightSharp.Chromium.Protocol;
using PlaywrightSharp.Chromium.Protocol.Runtime;
using PlaywrightSharp.Helpers;

namespace PlaywrightSharp.Chromium
{
    internal class ChromiumExecutionContext : IExecutionContextDelegate
    {
        private const string EvaluationScriptUrl = "__playwright_evaluation_script__";
        private static readonly Regex _sourceUrlRegex = new Regex("/^[\040\t] *\\/\\/[@#] sourceURL=\\s*(\\S*?)\\s*$", RegexOptions.Compiled);
        private readonly ChromiumSession _client;

        public ChromiumExecutionContext(ChromiumSession client, ExecutionContextDescription contextPayload)
        {
            _client = client;
            ContextId = contextPayload.Id.Value;
        }

        internal int ContextId { get; }

        public async Task EvaluateAsync(FrameExecutionContext context, bool returnByValue, string script, object[] args)
            => await EvaluateAsync<object>(context, returnByValue, script, args).ConfigureAwait(false);

        public async Task<T> EvaluateAsync<T>(FrameExecutionContext context, bool returnByValue, string script, object[] args)
        {
            string suffix = $"//# sourceURL={EvaluationScriptUrl}";
            RemoteObject remoteObject = null;

            if (script.IsJavascriptFunction())
            {
                RuntimeCallFunctionOnResponse result = null;

                try
                {
                    result = await _client.SendAsync(new RuntimeCallFunctionOnRequest
                    {
                        FunctionDeclaration = $"{script}\n{suffix}\n",
                        ExecutionContextId = ContextId,
                        Arguments = args.Select(a => FormatArgument(a, context)).ToArray(),
                        ReturnByValue = returnByValue,
                        AwaitPromise = true,
                        UserGesture = true,
                    }).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    result = RewriteError(ex);
                }

                if (result.ExceptionDetails != null)
                {
                    throw new PlaywrightSharpException($"Evaluation failed: {result.ExceptionDetails.ToExceptionMessage()}");
                }

                remoteObject = result.Result;
            }
            else
            {
                string expressionWithSourceUrl = _sourceUrlRegex.IsMatch(script) ? script : script + '\n' + suffix;
                var result = await _client.SendAsync(new RuntimeEvaluateRequest
                {
                    Expression = expressionWithSourceUrl,
                    ContextId = ContextId,
                    ReturnByValue = returnByValue,
                    AwaitPromise = true,
                    UserGesture = true,
                }).ConfigureAwait(false);

                if (result.ExceptionDetails != null)
                {
                    throw new PlaywrightSharpException($"Evaluation failed: {result.ExceptionDetails.ToExceptionMessage()}");
                }

                remoteObject = result.Result;
            }

            return (T)(returnByValue ? GetValueFromRemoteObject<T>(remoteObject) : context.CreateHandle(remoteObject));
        }

        public Task ReleaseHandleAsync(JSHandle handle) => ReleaseObjectAsync(_client, handle.RemoteObject);

        public string HandleToString(IJSHandle handle, bool includeType)
        {
            var remote = ((JSHandle)handle).RemoteObject;
            if (!string.IsNullOrEmpty(remote.ObjectId))
            {
                string type = string.IsNullOrEmpty(remote.Subtype) ? remote.Type : remote.Subtype;
                return "JSHandle@" + type;
            }

            return (includeType ? "JSHandle:" : string.Empty) + GetValueFromRemoteObject<string>(remote);
        }

        public async Task<T> HandleJSONValueAsync<T>(IJSHandle handle)
        {
            var remoteObject = ((JSHandle)handle).RemoteObject;

            if (!string.IsNullOrEmpty(remoteObject.ObjectId))
            {
                var response = await _client.SendAsync(new RuntimeCallFunctionOnRequest
                {
                    FunctionDeclaration = "function() { return this; }",
                    ObjectId = remoteObject.ObjectId,
                    ReturnByValue = true,
                    AwaitPromise = true,
                }).ConfigureAwait(false);

                return (T)GetValueFromRemoteObject<T>(response.Result);
            }

            return (T)GetValueFromRemoteObject<T>(remoteObject);
        }

        private RuntimeCallFunctionOnResponse RewriteError(Exception ex)
        {
            if (ex.Message.Contains("Object reference chain is too long"))
            {
                return new RuntimeCallFunctionOnResponse { Result = new RemoteObject { Type = "undefined" } };
            }

            if (ex.Message.Contains("Object couldn't be returned by value"))
            {
                return new RuntimeCallFunctionOnResponse { Result = new RemoteObject { Type = "undefined" } };
            }

            if (
                ex.Message.EndsWith("Cannot find context with specified id") ||
                ex.Message.EndsWith("Inspected target navigated or closed") ||
                ex.Message.EndsWith("Execution context was destroyed."))
            {
                throw new PlaywrightSharpException("Execution context was destroyed, most likely because of a navigation.");
            }

            throw ex;
        }

        private object ValueFromUnserializableValue(IRemoteObject remoteObject, string unserializableValue)
        {
            if (
                remoteObject.Type == RemoteObjectType.Bigint &&
                decimal.TryParse(remoteObject.UnserializableValue.Replace("n", string.Empty), out decimal decimalValue))
            {
                return new BigInteger(decimalValue);
            }

            switch (unserializableValue)
            {
                case "-0":
                    return -0;
                case "NaN":
                    return double.NaN;
                case "Infinity":
                    return double.PositiveInfinity;
                case "-Infinity":
                    return double.NegativeInfinity;
                default:
                    throw new PlaywrightSharpException("Unsupported unserializable value: " + unserializableValue);
            }
        }

        private CallArgument FormatArgument(object arg, FrameExecutionContext context)
        {
            switch (arg)
            {
                case BigInteger big:
                    return new CallArgument { UnserializableValue = $"{big}n" };

                case int integer when integer == -0:
                    return new CallArgument { UnserializableValue = "-0" };

                case double d:
                    if (double.IsPositiveInfinity(d))
                    {
                        return new CallArgument { UnserializableValue = "Infinity" };
                    }

                    if (double.IsNegativeInfinity(d))
                    {
                        return new CallArgument { UnserializableValue = "-Infinity" };
                    }

                    if (double.IsNaN(d))
                    {
                        return new CallArgument { UnserializableValue = "NaN" };
                    }

                    break;

                case JSHandle objectHandle:
                    if (objectHandle.Context != context)
                    {
                        throw new PlaywrightSharpException("JSHandles can be evaluated only in the context they were created!");
                    }

                    if (objectHandle.Disposed)
                    {
                        throw new PlaywrightSharpException("JSHandle is disposed!");
                    }

                    var remoteObject = objectHandle.RemoteObject;
                    if (!string.IsNullOrEmpty(remoteObject.UnserializableValue))
                    {
                        return new CallArgument { UnserializableValue = remoteObject.UnserializableValue };
                    }

                    if (string.IsNullOrEmpty(remoteObject.ObjectId))
                    {
                        return new CallArgument { Value = remoteObject.Value };
                    }

                    return new CallArgument { ObjectId = remoteObject.ObjectId };
            }

            return new CallArgument
            {
                Value = arg,
            };
        }

        private object GetValueFromRemoteObject<T>(IRemoteObject remoteObject)
        {
            string unserializableValue = remoteObject.UnserializableValue;

            if (unserializableValue != null)
            {
                return ValueFromUnserializableValue(remoteObject, unserializableValue);
            }

            object value = remoteObject.Value;

            if (value == null)
            {
                return default(T);
            }

            return remoteObject != null ? ((JsonElement)remoteObject.Value).ToObject<T>() : default;
        }

        private async Task ReleaseObjectAsync(ChromiumSession client, IRemoteObject remoteObject)
        {
            if (string.IsNullOrEmpty(remoteObject.ObjectId))
            {
                return;
            }

            try
            {
                await client.SendAsync(new RuntimeReleaseObjectRequest
                {
                    ObjectId = remoteObject.ObjectId,
                }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"{ex}\n{ex.StackTrace}");
            }
        }
    }
}
