using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using Microsoft.Playwright.Helpers;
using Microsoft.Playwright.Transport.Channels;
using Microsoft.Playwright.Transport.Converters;

namespace Microsoft.Playwright
{
    internal static class ScriptsHelper
    {
        private static readonly MethodInfo _parseEvaluateResult = typeof(ScriptsHelper)
            .GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
            .Single(m => m.Name == nameof(ParseEvaluateResult) && m.IsGenericMethod);

        internal static string SerializeScriptCall(string script, object[] args = null)
        {
            args ??= Array.Empty<object>();

            if (script.IsJavascriptFunction())
            {
                if (args?.Any() == true)
                {
                    return $"({script})({string.Join(",", args.Select(a => JsonSerializer.Serialize(a, JsonExtensions.GetNewDefaultSerializerOptions())))})";
                }

                return $"({script})()";
            }

            if (args.Length > 0)
            {
                throw new PlaywrightException("Cannot evaluate a string with arguments");
            }

            return script;
        }

        internal static object ParseEvaluateResult(JsonElement? element, Type t)
        {
            var genericMethod = _parseEvaluateResult.MakeGenericMethod(t);
            return genericMethod.Invoke(null, new object[] { element });
        }

        internal static T ParseEvaluateResult<T>(JsonElement? result)
        {
            var serializerOptions = JsonExtensions.GetNewDefaultSerializerOptions();
            serializerOptions.Converters.Add(new EvaluateArgumentValueConverter<T>());

            return result == null ? default : result.Value.ToObject<T>(serializerOptions);
        }

        internal static object SerializedArgument(object arg)
        {
            var converter = new EvaluateArgumentValueConverter<JsonElement>();
            return new { value = converter.Serialize(arg), handles = converter.Handles };
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
