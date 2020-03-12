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
        private readonly int _contextId;

        public ChromiumExecutionContext(ChromiumSession client, ExecutionContextDescription contextPayload)
        {
            _client = client;
            _contextId = contextPayload.Id.Value;
        }

        public async Task EvaluateAsync(FrameExecutionContext context, bool returnByValue, string script, object[] args)
            => await EvaluateAsync<object>(context, returnByValue, script, args).ConfigureAwait(false);

        public async Task<T> EvaluateAsync<T>(FrameExecutionContext context, bool returnByValue, string script, object[] args)
        {
            string suffix = $"//# sourceURL={EvaluationScriptUrl}";
            RemoteObject remoteObject = null;

            if (!script.IsJavascriptFunction())
            {
                string expressionWithSourceUrl = _sourceUrlRegex.IsMatch(script) ? script : script + '\n' + suffix;
                var result = await _client.SendAsync(new RuntimeEvaluateRequest
                {
                    Expression = expressionWithSourceUrl,
                    ContextId = _contextId,
                    ReturnByValue = returnByValue,
                    AwaitPromise = true,
                    UserGesture = true,
                }).ConfigureAwait(false);

                if (result.ExceptionDetails != null)
                {
                    throw new PlaywrightSharpException($"Evaluation failed: {GetExceptionMessage(result.ExceptionDetails)}");
                }

                remoteObject = result.Result;
            }
            else
            {
                var result = await _client.SendAsync(new RuntimeCallFunctionOnRequest
                {
                    FunctionDeclaration = $"{script}\n{suffix}\n",
                    ExecutionContextId = _contextId,
                    Arguments = args.Select(a => FormatArgument(a, context)).ToArray(),
                    ReturnByValue = returnByValue,
                    AwaitPromise = true,
                    UserGesture = true,
                }).ConfigureAwait(false);

                if (result.ExceptionDetails != null)
                {
                    throw new PlaywrightSharpException($"Evaluation failed: {GetExceptionMessage(result.ExceptionDetails)}");
                }

                remoteObject = result.Result;
            }

            return (T)(returnByValue ? GetValueFromRemoteObject<T>(remoteObject) : context.CreateHandle(remoteObject));
        }

        private static object ValueFromUnserializableValue(RemoteObject remoteObject, string unserializableValue)
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

        private object GetValueFromRemoteObject<T>(RemoteObject remoteObject)
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

        private object GetExceptionMessage(ExceptionDetails exceptionDetails)
        {
            if (exceptionDetails.Exception != null)
            {
                return exceptionDetails.Exception.Description ?? exceptionDetails.Exception.Value;
            }

            string message = exceptionDetails.Text;
            if (exceptionDetails.StackTrace != null)
            {
                foreach (var callframe in exceptionDetails.StackTrace.CallFrames)
                {
                    string location = $"{callframe.Url}:{callframe.LineNumber}:{callframe.ColumnNumber}";
                    string functionName = string.IsNullOrEmpty(callframe.FunctionName) ? "<anonymous>" : callframe.FunctionName;
                    message += $"\n at ${functionName} (${location})";
                }
            }

            return message;
        }
    }
}
