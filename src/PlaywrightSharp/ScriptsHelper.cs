using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json;
using PlaywrightSharp.Helpers;
using PlaywrightSharp.Transport.Channels;

namespace PlaywrightSharp
{
    internal static class ScriptsHelper
    {
        internal static bool IsPrimitiveValue(Type type)
            => type == typeof(string) ||
            type == typeof(decimal) ||
            type == typeof(double) ||
            type == typeof(bool) ||
            type == typeof(decimal?) ||
            type == typeof(double?) ||
            type == typeof(bool?);

        internal static T ParseEvaluateResult<T>(JsonElement? result)
        {
            if (!result.HasValue)
            {
                return default;
            }

            if (result.Value.ValueKind == JsonValueKind.Object && result.Value.TryGetProperty("v", out var value) && value.ToString() == "undefined")
            {
                return default;
            }

            if (typeof(T) == typeof(JsonElement?))
            {
                return (T)(object)result;
            }

            if (result.Value.ValueKind == JsonValueKind.Object && result.Value.TryGetProperty("o", out var obj))
            {
                return obj.ToObject<T>();
            }

            if (result.Value.ValueKind == JsonValueKind.Object && result.Value.TryGetProperty("v", out var vNull) && vNull.ValueKind == JsonValueKind.Null)
            {
                return default;
            }

            return result.Value.ToObject<T>();
        }

        internal static EvaluateArgument SerializedArgument(object args)
        {
            var result = new EvaluateArgument();
            var guids = new List<EvaluateArgumentGuidElement>();

            int PushHandle(string guid)
            {
                guids.Add(new EvaluateArgumentGuidElement { Guid = guid });
                return guids.Count - 1;
            }

            object value = SerializeAsCallArgument(args, value =>
            {
                if (value is IChannelOwner channelOwner)
                {
                    return new EvaluateArgumentValueElement
                    {
                        H = PushHandle(channelOwner.Channel.Guid),
                    };
                }

                return new EvaluateArgumentValueElement
                {
                    FallThrough = value,
                };
            });

            return new EvaluateArgument
            {
                Value = value,
                Guids = guids,
            };
        }

        internal static object SerializeAsCallArgument(object value, Func<object, EvaluateArgumentValueElement> jsHandleSerializer)
            => Serialize(value, jsHandleSerializer, new List<object>());

        internal static object Serialize(object value, Func<object, EvaluateArgumentValueElement> jsHandleSerializer, List<object> visited)
        {
            // This will endupt being a converter when we need to fully implement this
            value = jsHandleSerializer(value);

            if (value is EvaluateArgumentValueElement valueElement && valueElement.FallbackSet)
            {
                value = valueElement.FallThrough;
            }
            else
            {
                return value;
            }

            if (visited.Contains(value))
            {
                throw new PlaywrightSharpException("Argument is a circular structure");
            }

            if (value == null)
            {
                return new EvaluateArgumentValueElement.SpecialType { V = "null" };
            }

            if (value is double nan && double.IsNaN(nan))
            {
                return new EvaluateArgumentValueElement.SpecialType { V = "NaN" };
            }

            if (value is double infinity && double.IsInfinity(infinity))
            {
                return new EvaluateArgumentValueElement.SpecialType { V = "Infinity" };
            }

            if (value is double negativeInfinity && double.IsNegativeInfinity(negativeInfinity))
            {
                return new EvaluateArgumentValueElement.SpecialType { V = "Infinity" };
            }

            if (value is double negativeZero && negativeZero == -0)
            {
                return new EvaluateArgumentValueElement.SpecialType { V = "-0" };
            }

            if (IsPrimitiveValue(value.GetType()))
            {
                return value;
            }

            if (value is DateTime date)
            {
                return new EvaluateArgumentValueElement.Datetime { D = date };
            }

            if (value is IEnumerable enumerable)
            {
                var result = new List<object>();
                visited.Add(value);

                foreach (object item in enumerable)
                {
                    result.Add(Serialize(item, jsHandleSerializer, visited));
                }

                visited.Remove(value);

                return new EvaluateArgumentValueElement.Array { A = result.ToArray() };
            }

            return new EvaluateArgumentValueElement.Object { O = value };
        }
    }
}
