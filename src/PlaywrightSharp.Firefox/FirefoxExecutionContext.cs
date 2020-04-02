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

        public FirefoxExecutionContext(FirefoxSession workerSession, string executionContextId)
        {
            _session = workerSession;
            ExecutionContextId = executionContextId;
        }

        internal string ExecutionContextId { get; }

        public async Task<T> EvaluateAsync<T>(FrameExecutionContext frameExecutionContext, bool returnByValue, string pageFunction, object[] args)
        {
            if (!StringExtensions.IsJavascriptFunction(ref pageFunction))
            {
                var result = await _session.SendAsync(new RuntimeEvaluateRequest
                {
                    Expression = pageFunction.Trim(),
                    ReturnByValue = returnByValue,
                    ExecutionContextId = ExecutionContextId,
                }).ConfigureAwait(false);
                return ExtractResult<T>(result.ExceptionDetails, result.Result, returnByValue, frameExecutionContext);
            }

            RuntimeCallFunctionResponse payload = null;

            try
            {
                string functionText = pageFunction;
                payload = await _session.SendAsync(new RuntimeCallFunctionRequest
                {
                    FunctionDeclaration = functionText,
                    Args = Array.ConvertAll(args, arg => FormatArgument(arg, frameExecutionContext)),
                    ReturnByValue = returnByValue,
                    ExecutionContextId = ExecutionContextId,
                }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                payload = RewriteError(ex);
            }

            return ExtractResult<T>(payload.ExceptionDetails, payload.Result, returnByValue, frameExecutionContext);
        }

        public string HandleToString(IJSHandle handle, bool includeType)
        {
            var payload = ((JSHandle)handle).RemoteObject;
            if (!string.IsNullOrEmpty(payload.ObjectId))
            {
                return "JSHandle@" + (string.IsNullOrEmpty(payload.Subtype) ? payload.Type : payload.Subtype);
            }

            return (includeType ? "JSHandle:" : string.Empty) + DeserializeValue<object>((RemoteObject)payload);
        }

        public async Task<T> HandleJSONValueAsync<T>(IJSHandle jsHandle)
        {
            var payload = (RemoteObject)((JSHandle)jsHandle).RemoteObject;
            if (string.IsNullOrEmpty(payload.ObjectId))
            {
                return DeserializeValue<T>(payload);
            }

            var simpleValue = await _session.SendAsync(new RuntimeCallFunctionRequest
            {
                ExecutionContextId = ExecutionContextId,
                ReturnByValue = true,
                FunctionDeclaration = "e => e",
                Args = new[] { ToCallArgument(payload) },
            }).ConfigureAwait(false);
            return DeserializeValue<T>(simpleValue.Result);
        }

        public Task ReleaseHandleAsync(JSHandle handle)
        {
            if (string.IsNullOrEmpty(handle?.RemoteObject.ObjectId))
            {
                return Task.CompletedTask;
            }

            return _session.SendAsync(new RuntimeDisposeObjectRequest
            {
                ExecutionContextId = ExecutionContextId,
                ObjectId = handle.RemoteObject.ObjectId,
            });
        }

        private RuntimeCallFunctionResponse RewriteError(Exception error)
        {
            if (error.Message.Contains("Cyclic object value") || error.Message.Contains("Object is not serializable"))
            {
                return new RuntimeCallFunctionResponse { Result = new RemoteObject { Type = RemoteObjectType.Undefined, Value = null } };
            }

            if (error.Message.Contains("Failed to find execution context with id") || error.Message.Contains("Execution context was destroyed!"))
            {
                throw new PlaywrightSharpException("Execution context was destroyed, most likely because of a navigation.");
            }

            throw error;
        }

        private T ExtractResult<T>(ExceptionDetails exceptionDetails, RemoteObject remoteObject, bool returnByValue, FrameExecutionContext context)
        {
            CheckException(exceptionDetails);
            if (returnByValue)
            {
                return DeserializeValue<T>(remoteObject);
            }

            return (T)context.CreateHandle(remoteObject);
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
                object raw = ValueFromUnserializableValue(unserializableValue.Value);
                return (T)raw;
            }

            if (remoteObject.Value == null)
            {
                return default;
            }

            return typeof(T) == typeof(JsonElement) ? (T)remoteObject.Value : (T)ValueFromType<T>((JsonElement)remoteObject.Value, remoteObject.Type ?? RemoteObjectType.Object);
        }

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
                RemoteObjectType.Number => typeof(T) == typeof(int) ? value.GetInt32() : value.GetDouble(),
                RemoteObjectType.Bigint => value.GetDouble(),
                RemoteObjectType.Boolean => value.GetBoolean(),
                _ => value.ToObject<T>()
            };
    }
}
