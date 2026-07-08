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
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Playwright.Helpers;
using Microsoft.Playwright.Transport.Converters;

namespace Microsoft.Playwright.Core;

internal static class ScriptsHelper
{
    internal static object? ParseEvaluateResultUntyped(JsonElement? element)
    {
        if (element == null)
        {
            return null;
        }

        var result = (JsonElement)element;

        if (result.ValueKind == JsonValueKind.Object && result.TryGetProperty("value", out var valueProperty))
        {
            result = valueProperty;
        }

        return result;
    }

    internal static T ParseEvaluateResult<T>(JsonElement? resultOrNull)
    {
        if (resultOrNull == null)
        {
            return default!;
        }

        var result = (JsonElement)resultOrNull;

        if (result.ValueKind == JsonValueKind.Object && result.TryGetProperty("value", out var valueProperty))
        {
            result = valueProperty;
        }

        var parsed = EvaluateArgumentValueConverter.Deserialize(result, typeof(T));
        if (parsed == null)
        {
            return default!;
        }

        return (T)parsed;
    }

    internal static JsonObject SerializedArgument(object? arg)
    {
        var handles = new List<EvaluateArgumentGuidElement>();
        var value = EvaluateArgumentValueConverter.Serialize(arg, handles, new());
        var handlesList = new List<JsonNode?>();
        foreach (var h in handles)
        {
            handlesList.Add(new JsonObject { ["guid"] = JsonValue.Create(h.Guid) });
        }
        return new JsonObject
        {
            ["value"] = value,
            ["handles"] = new JsonArray(handlesList.ToArray()),
        };
    }

    internal static string EvaluationScript(string? content, string? path, bool addSourceUrl)
    {
        if (!content.IsNullOrEmpty())
        {
            return content;
        }
        else if (!path.IsNullOrEmpty())
        {
            var source = File.ReadAllText(path);
            return addSourceUrl ? AddSourceUrlToScript(source, path) : source;
        }

        throw new ArgumentException("Either path or content property must be present");
    }

    internal static string AddSourceUrlToScript(string source, string path)
    {
        return source + "\n//# sourceURL=" + path.Replace("\n", string.Empty);
    }
}

internal class EvaluateArgumentGuidElement
{
    public string Guid { get; set; } = null!;
}
