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
using System.Text.Json;
using Microsoft.Playwright.Transport;

#nullable enable
#pragma warning disable IDE0018 // Inline variable declaration will cause issues due to Roslyn design: https://github.com/dotnet/roslyn/issues/54711

namespace Microsoft.Playwright.Helpers;

internal static class ChannelHelpers
{
    internal static string? GetString(this JsonElement? element, string name, bool optional)
    {
        if (!element.HasValue)
        {
            return null;
        }

        System.Text.Json.JsonElement retElement;
        if (element.Value.TryGetProperty(name, out retElement))
        {
            return retElement.ToString();
        }

        if (optional)
        {
            return null;
        }

        throw new ArgumentNullException($"Element {name} was expected to not be optional, yet was not present in the data.");
    }

    internal static T? GetObject<T>(this JsonElement? element, string name, Connection connection)
        where T : ChannelOwner
    {
        if (!element.HasValue)
        {
            return null;
        }

        JsonElement retElement;
        if (!element.Value.TryGetProperty(name, out retElement))
        {
            return null;
        }

        var guid = retElement.GetProperty("guid").ToString();
        return connection.GetObject(guid) as T;
    }

    internal static T GetObject<T>(this JsonElement element, string name, Connection connection)
        where T : ChannelOwner
    {
        var result = GetObject<T>((JsonElement?)element, name, connection);
        if (result == null)
        {
            throw new ArgumentNullException($"Element {name} expected, but was not found.");
        }

        return result;
    }

    // this method is needed, because a converter for <see cref="Exception"/> is no longer supported
    // since .NET5 and throws an exception of "Serialization and deserialization of 'System.Type' instances are not supported and should be avoided since they can lead to security issues."}
    internal static dynamic ToObject(this Exception exception)
    {
        if (exception == null)
        {
            return new { };
        }

        return new
        {
            error = new
            {
                message = exception.Message,
                stack = exception.StackTrace,
                name = exception.GetType().Name,
            },
        };
    }
}

#nullable disable
#pragma warning restore IDE0018 // Inline variable declaration
