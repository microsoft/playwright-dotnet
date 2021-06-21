/*
 * MIT License
 *
 * Copyright (c) Microsoft Corporation.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

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

namespace Microsoft.Playwright.Core
{
    internal static class ScriptsHelper
    {
        private static readonly MethodInfo _parseEvaluateResult = typeof(ScriptsHelper)
            .GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
            .Single(m => m.Name == nameof(ParseEvaluateResult) && m.IsGenericMethod);

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

        internal static string EvaluationScript(string content, string path)
        {
            if (!string.IsNullOrEmpty(content))
            {
                return content;
            }
            else if (!string.IsNullOrEmpty(path))
            {
                return File.ReadAllText(path);
            }

            throw new ArgumentException("Either path or content property must be present");
        }
    }
}
