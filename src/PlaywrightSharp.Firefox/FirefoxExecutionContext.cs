using System;
using System.Numerics;
using System.Text.Json;
using System.Threading.Tasks;
using PlaywrightSharp.Firefox.Protocol.Runtime;
using PlaywrightSharp.Helpers;

namespace PlaywrightSharp.Firefox
{
    internal class FirefoxExecutionContext : IExecutionContextDelegate
    {
        private readonly FirefoxSession _session;
        private readonly string _executionContextId;

        public FirefoxExecutionContext(FirefoxSession workerSession, string executionContextId)
        {
            _session = workerSession;
            _executionContextId = executionContextId;
        }

        public async Task<T> EvaluateAsync<T>(FrameExecutionContext frameExecutionContext, bool returnByValue, string pageFunction, object[] args)
        {
            if (!pageFunction.IsJavascriptFunction())
            {
                var payload = await _session.SendAsync(new RuntimeEvaluateRequest
                {
                    Expression = pageFunction.Trim(),
                    ReturnByValue = returnByValue,
                    ExecutionContextId = _executionContextId,
                }).ConfigureAwait(false);
                return ExtractResult<T>(payload.ExceptionDetails, payload.Result, returnByValue, frameExecutionContext);
            }

            string functionText = pageFunction;
            var callFunctionTask = _session.SendAsync(new RuntimeCallFunctionRequest
            {
                FunctionDeclaration = functionText,
                Args = Array.ConvertAll(args, arg => FormatArgument(arg, frameExecutionContext)),
                ReturnByValue = returnByValue,
                ExecutionContextId = _executionContextId,
            });
            {
                var payload = await callFunctionTask.ConfigureAwait(false);
                return ExtractResult<T>(payload.ExceptionDetails, payload.Result, returnByValue, frameExecutionContext);
            }
        }

        public string HandleToString(IJSHandle arg, bool includeType)
        {
            throw new System.NotImplementedException();
        }

        public Task ReleaseHandleAsync(JSHandle handle)
        {
            throw new System.NotImplementedException();
        }

        private T ExtractResult<T>(ExceptionDetails exceptionDetails, RemoteObject remoteObject, bool returnByValue, FrameExecutionContext context)
        {
            CheckException(exceptionDetails);
            if (returnByValue)
            {
                return DeserializeValue<T>(remoteObject);
            }

            return (T)CreateHandle(remoteObject, context);
        }

        private void CheckException(ExceptionDetails exceptionDetails)
        {
            if (exceptionDetails != null)
            {
                if (exceptionDetails.Value != null)
                {
                    throw new PlaywrightSharpException("Evaluation failed: " + exceptionDetails.Value?.ToJson());
                }
                else
                {
                    throw new PlaywrightSharpException("Evaluation failed: " + exceptionDetails.Text + '\n' + exceptionDetails.Stack);
                }
            }
        }

        private CallFunctionArgument FormatArgument(object arg, FrameExecutionContext context)
        {
            switch (arg)
            {
                case int integer when integer == -0:
                    return new CallFunctionArgument { UnserializableValue = RemoteObjectUnserializableValue.NegativeZero };
                case double d when double.IsPositiveInfinity(d):
                    return new CallFunctionArgument { UnserializableValue = RemoteObjectUnserializableValue.Infinity };
                case double d when double.IsNegativeInfinity(d):
                    return new CallFunctionArgument { UnserializableValue = RemoteObjectUnserializableValue.NegativeZero };
                case double d when double.IsNaN(d):
                    return new CallFunctionArgument { UnserializableValue = RemoteObjectUnserializableValue.NaN };
                case JSHandle objectHandle:
                    if (objectHandle.Context != context)
                    {
                        throw new PlaywrightSharpException("JSHandles can be evaluated only in the context they were created!");
                    }

                    if (objectHandle.Disposed)
                    {
                        throw new PlaywrightSharpException("JSHandle is disposed!");
                    }

                    return ToCallArgument(objectHandle.RemoteObject);
            }

            return new CallFunctionArgument
            {
                Value = arg,
            };
        }

        private CallFunctionArgument ToCallArgument(IRemoteObject remoteObject)
            => new CallFunctionArgument
            {
                Value = remoteObject.Value,
                UnserializableValue = RemoteObject.GetUnserializableValueFromRaw(remoteObject.UnserializableValue),
                ObjectId = remoteObject.ObjectId,
            };

        private T DeserializeValue<T>(RemoteObject remoteObject)
        {
            var unserializableValue = remoteObject.UnserializableValue;
            if (unserializableValue != null)
            {
                return (T)ValueFromUnserializableValue(unserializableValue.Value);
            }

            if (remoteObject.Value == null)
            {
                return default;
            }

            return typeof(T) == typeof(JsonElement) ? (T)remoteObject.Value : (T)ValueFromType<T>((JsonElement)remoteObject.Value, remoteObject.Type ?? RemoteObjectType.Object);
        }

        private object CreateHandle(RemoteObject remoteObject, FrameExecutionContext context) => new JSHandle(context, remoteObject);

        private object ValueFromUnserializableValue(RemoteObjectUnserializableValue unserializableValue)
            => unserializableValue switch
            {
                RemoteObjectUnserializableValue.NegativeZero => -0,
                RemoteObjectUnserializableValue.NaN => double.NaN,
                RemoteObjectUnserializableValue.Infinity => double.PositiveInfinity,
                RemoteObjectUnserializableValue.NegativeInfinity => double.NegativeInfinity,
                _ => throw new Exception("Unsupported unserializable value: " + unserializableValue),
            };

        private object ValueFromType<T>(JsonElement value, RemoteObjectType objectType)
            => objectType switch
            {
                RemoteObjectType.Object => value.ToObject<T>(),
                RemoteObjectType.Undefined => null,
                RemoteObjectType.Number => value.GetDouble(),
                RemoteObjectType.Bigint => value.GetDouble(),
                RemoteObjectType.Boolean => value.GetBoolean(),
                _ => value.ToObject<T>()
            };
    }
}
