using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
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

        internal static string SerializeScriptCall(string script, object[] args)
        {
            args ??= Array.Empty<object>();

            if (script.IsJavascriptFunction())
            {
                return $"({script})({string.Join(",", args.Select(a => JsonSerializer.Serialize(a, JsonExtensions.GetNewDefaultSerializerOptions())))})";
            }

            if (args.Length > 0)
            {
                throw new PlaywrightSharpException("Cannot evaluate a string with arguments");
            }

            return script;
        }

        internal static T ParseEvaluateResult<T>(JsonElement? result)
        {
            object parsed = ParseEvaluateResult(result, typeof(T));

            if (parsed == null)
            {
                return default;
            }

            return (T)parsed;
        }

        internal static object ParseEvaluateResult(JsonElement? result, Type t)
        {
            if (!result.HasValue)
            {
                return null;
            }

            if (result.Value.ValueKind == JsonValueKind.Object && result.Value.TryGetProperty("v", out var value))
            {
                if (value.ValueKind == JsonValueKind.Null)
                {
                    return default;
                }

                return value.ToString() switch
                {
                    "undefined" => default,
                    "Infinity" => double.PositiveInfinity,
                    "-Infinity" => double.NegativeInfinity,
                    "-0" => -0d,
                    "NaN" => double.NaN,
                    _ => value.ToObject(t),
                };
            }

            if (result.Value.ValueKind == JsonValueKind.Object && result.Value.TryGetProperty("d", out var date))
            {
                return date.ToObject<DateTime>();
            }

            if (result.Value.ValueKind == JsonValueKind.Object && result.Value.TryGetProperty("o", out var obj))
            {
                if (t == typeof(ExpandoObject) || t == typeof(object))
                {
                    return ReadObject(obj);
                }

                return obj.ToObject(t);
            }

            if (result.Value.ValueKind == JsonValueKind.Object && result.Value.TryGetProperty("v", out var vNull) && vNull.ValueKind == JsonValueKind.Null)
            {
                return default;
            }

            if (result.Value.ValueKind == JsonValueKind.Object && result.Value.TryGetProperty("a", out var array) && array.ValueKind == JsonValueKind.Array)
            {
                if (t == typeof(ExpandoObject) || t == typeof(object))
                {
                    return ReadList(array);
                }

                return array.ToObject(t);
            }

            if (t == typeof(JsonElement?))
            {
                return result;
            }

            return result.Value.ToObject(t);
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

        internal static string EvaluationScript(string content, string path, bool addSourceUrl = true)
        {
            if (!string.IsNullOrEmpty(content))
            {
                return content;
            }
            else if (!string.IsNullOrEmpty(path))
            {
                string contents = File.ReadAllText(path);

                if (addSourceUrl)
                {
                    contents += "//# sourceURL=" + path.Replace(" ", string.Empty);
                }

                return contents;
            }

            throw new ArgumentException("Either path or content property must be present");
        }

        private static object ReadObject(JsonElement jsonElement)
        {
            IDictionary<string, object> expandoObject = new ExpandoObject();
            foreach (var obj in jsonElement.EnumerateObject())
            {
                expandoObject[obj.Name] = ParseEvaluateResult(obj.Value, ValueKindToType(obj.Value));
            }

            return expandoObject;
        }

        private static Type ValueKindToType(JsonElement element)
            => element.ValueKind switch
            {
                JsonValueKind.Array => typeof(Array),
                JsonValueKind.String => typeof(string),
                JsonValueKind.Number => decimal.Truncate(element.ToObject<decimal>()) != element.ToObject<decimal>() ? typeof(decimal) : typeof(int),
                JsonValueKind.True => typeof(bool),
                JsonValueKind.False => typeof(bool),
                _ => typeof(object),
            };

        private static object ReadList(JsonElement jsonElement)
        {
            IList<object> list = new List<object>();
            foreach (var item in jsonElement.EnumerateArray())
            {
                list.Add(ParseEvaluateResult(item, ValueKindToType(item)));
            }

            return list.Count == 0 ? null : list;
        }
    }
}
