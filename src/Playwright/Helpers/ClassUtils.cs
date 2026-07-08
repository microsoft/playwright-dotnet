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

using System.Text.Json;
using Microsoft.Playwright.Transport;

namespace Microsoft.Playwright.Helpers;

internal static class ClassUtils
{
    internal static T Clone<T>(object? source)
        where T : new()
    {
        T target = new();
        if (source == null)
        {
            return target;
        }

        var sourceTypeInfo = PlaywrightJsonContext.Default.GetTypeInfo(source.GetType());
        if (sourceTypeInfo == null)
        {
            throw new System.InvalidOperationException(
                $"Type '{source.GetType().FullName}' is not registered in PlaywrightJsonContext. " +
                $"Add [JsonSerializable(typeof({source.GetType().Name}))] to enable AOT-safe cloning.");
        }
        var targetTypeInfo = PlaywrightJsonContext.Default.GetTypeInfo(typeof(T));
        if (targetTypeInfo == null)
        {
            throw new System.InvalidOperationException(
                $"Type '{typeof(T).FullName}' is not registered in PlaywrightJsonContext. " +
                $"Add [JsonSerializable(typeof({typeof(T).Name}))] to enable AOT-safe cloning.");
        }
        var node = JsonSerializer.SerializeToNode(source, sourceTypeInfo);
        return (T)JsonSerializer.Deserialize(node, targetTypeInfo)!;
    }
}
