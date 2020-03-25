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

                // TODO: rewriteError
                return ExtractResult(payload.ExceptionDetails, payload.Result);
            }

            string functionText = pageFunction.ToFunctionText();
            var callFunctionTask = _session.SendAsync(new RuntimeCallFunctionRequest
            {
                FunctionDeclaration = functionText,
                Args = Array.ConvertAll(args, FormatArgument),
                ReturnByValue = returnByValue,
                ExecutionContextId = _executionContextId,
            });

            // TODO: validate request
            {
                var payload = await callFunctionTask.ConfigureAwait(false);
                return ExtractResult(payload.ExceptionDetails, payload.Result);
            }

            T ExtractResult(ExceptionDetails exceptionDetails, RemoteObject remoteObject)
            {
                CheckException(exceptionDetails);
                if (returnByValue)
                {
                    return DeserializeValue(remoteObject);
                }

                return (T)CreateHandle(remoteObject);
            }

            void CheckException(ExceptionDetails exceptionDetails)
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

            CallFunctionArgument FormatArgument(object arg)
            {
                switch (arg)
                {
                    case int integer when integer == -0:
                        return new CallFunctionArgument { UnserializableValue = RemoteObjectUnserializableValue.NegativeZero };

                    case double d:
                        if (double.IsPositiveInfinity(d))
                        {
                            return new CallFunctionArgument { UnserializableValue = RemoteObjectUnserializableValue.Infinity };
                        }

                        if (double.IsNegativeInfinity(d))
                        {
                            return new CallFunctionArgument { UnserializableValue = RemoteObjectUnserializableValue.NegativeZero };
                        }

                        if (double.IsNaN(d))
                        {
                            return new CallFunctionArgument { UnserializableValue = RemoteObjectUnserializableValue.NaN };
                        }

                        break;

                    case JSHandle objectHandle:
                        if (objectHandle.Context != frameExecutionContext)
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

            CallFunctionArgument ToCallArgument(IRemoteObject remoteObject) => new CallFunctionArgument
            {
                Value = remoteObject.Value,
                UnserializableValue = RemoteObject.GetUnserializableValueFromRaw(remoteObject.UnserializableValue),
                ObjectId = remoteObject.ObjectId,
            };

            T DeserializeValue(RemoteObject remoteObject)
            {
                var unserializableValue = remoteObject.UnserializableValue;
                if (unserializableValue != null)
                {
                    return (T)ValueFromUnserializableValue(remoteObject, unserializableValue.Value);
                }

                if (remoteObject.Value == null)
                {
                    return default;
                }

                return typeof(T) == typeof(JsonElement) ? (T)remoteObject.Value : (T)ValueFromType<T>((JsonElement)remoteObject.Value, remoteObject.Type ?? RemoteObjectType.Object);
            }

            object CreateHandle(RemoteObject remoteObject) => new JSHandle(frameExecutionContext, remoteObject);
        }

        public Task ReleaseHandleAsync(JSHandle handle)
        {
            throw new System.NotImplementedException();
        }

        private object ValueFromUnserializableValue(RemoteObject remoteObject, RemoteObjectUnserializableValue unserializableValue)
        {
            return unserializableValue switch
            {
                RemoteObjectUnserializableValue.NegativeZero => -0,
                RemoteObjectUnserializableValue.NaN => double.NaN,
                RemoteObjectUnserializableValue.Infinity => double.PositiveInfinity,
                RemoteObjectUnserializableValue.NegativeInfinity => double.NegativeInfinity,
                _ => throw new Exception("Unsupported unserializable value: " + unserializableValue),
            };
        }

        private object ValueFromType<T>(JsonElement value, RemoteObjectType objectType)
        {
            switch (objectType)
            {
                case RemoteObjectType.Object:
                    return value.ToObject<T>();
                case RemoteObjectType.Undefined:
                    return null;
                case RemoteObjectType.Number:
                case RemoteObjectType.Bigint:
                    return value.GetDouble();
                case RemoteObjectType.Boolean:
                    return value.GetBoolean();
                default: // string, symbol, function
                    return value.ToObject<T>();
            }
        }
    }
}
