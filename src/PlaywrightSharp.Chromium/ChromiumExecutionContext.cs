using System;
using System.Numerics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using PlaywrightSharp.Chromium.Helpers;
using PlaywrightSharp.Chromium.Protocol;
using PlaywrightSharp.Chromium.Protocol.Runtime;

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

            if (!IsFunction(script))
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

                return (T)(returnByValue ? GetValueFromRemoteObject<T>(result.Result) : context.CreateHandle(null /*TODO*/));
            }

            return default;
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

        private object GetValueFromRemoteObject<T>(RemoteObject remoteObject)
        {
            string unserializableValue = remoteObject.UnserializableValue;

            if (unserializableValue != null)
            {
                return ValueFromUnserializableValue(remoteObject, unserializableValue);
            }

            var value = remoteObject.Value;

            if (value == null)
            {
                return default(T);
            }

            return remoteObject.Value.HasValue ? remoteObject.Value.Value.ToObject<T>() : default;
        }

        private object GetExceptionMessage(ExceptionDetails exceptionDetails)
        {
            throw new NotImplementedException();
        }

        private bool IsFunction(string script) => false;
    }
}