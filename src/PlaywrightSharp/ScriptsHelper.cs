using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using PlaywrightSharp.Helpers;
using PlaywrightSharp.Transport.Channels;
using PlaywrightSharp.Transport.Converters;

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

        internal static object ParseEvaluateResult(JsonElement? element, Type t)
        {
            var parseEvaluateResult = typeof(ScriptsHelper)
                .GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
                .FirstOrDefault(m => m.Name == "ParseEvaluateResult" && m.GetGenericArguments().Any());

            var genericMethod = parseEvaluateResult.MakeGenericMethod(new[] { t });
            return genericMethod.Invoke(null, new object[] { element });
        }

        internal static T ParseEvaluateResult<T>(JsonElement? result)
        {
            var serializerOptions = JsonExtensions.GetNewDefaultSerializerOptions(false);
            serializerOptions.Converters.Add(new EvaluateArgumentValueConverter<T>(null));

            return result == null ? default : result.Value.ToObject<T>(serializerOptions);
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
                    return new EvaluateArgumentValueElement.Handle
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
                Handles = guids,
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

            if (value is string stringValue)
            {
                return new EvaluateArgumentValueElement.String { S = stringValue };
            }

            if (
                value.GetType() == typeof(int) ||
                value.GetType() == typeof(decimal) ||
                value.GetType() == typeof(long) ||
                value.GetType() == typeof(short) ||
                value.GetType() == typeof(double) ||
                value.GetType() == typeof(int?) ||
                value.GetType() == typeof(decimal?) ||
                value.GetType() == typeof(long?) ||
                value.GetType() == typeof(short?) ||
                value.GetType() == typeof(double?))
            {
                return new EvaluateArgumentValueElement.Number { N = value };
            }

            if (value is bool boolean)
            {
                return new EvaluateArgumentValueElement.Boolean { B = boolean };
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

            var kvList = new List<KeyValueObject>();

            foreach (var property in value.GetType().GetProperties())
            {
                kvList.Add(new KeyValueObject { K = property.Name, V = Serialize(property.GetValue(value), jsHandleSerializer, visited) });
            }

            return new EvaluateArgumentValueElement.Object { O = kvList.ToArray() };
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
    }
}