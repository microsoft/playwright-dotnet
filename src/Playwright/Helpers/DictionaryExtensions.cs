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
using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.Playwright.Core;
using Microsoft.Playwright.Transport.Channels;

internal static class DictionaryExtensions
{
    public static Dictionary<string, T> FilterNullValues<T>(this Dictionary<string, T> dictionary)
    {
        var newDict = new Dictionary<string, T>();
        foreach (var key in dictionary.Keys)
        {
            if (dictionary[key] is null)
            {
                continue;
            }
            newDict.Add(key.ToString(), (T)FilterNullValuesInternal(dictionary[key]));
        }
        return newDict;
    }

    private static object FilterNullValuesInternal(object value)
    {
        if (value is ChannelBase)
        {
            return new Dictionary<string, object>() { { "guid", ((ChannelBase)value).Guid } };
        }

        if (value is IEnumerable<WritableStream> streams)
        {
            var newDict = new List<Dictionary<string, string>>();
            foreach (var stream in streams)
            {
                newDict.Add(new Dictionary<string, string>() { { "guid", stream.Channel.Guid } });
            }
            return newDict;
        }

        if (value is null)
        {
            return null;
        }
        if (value is string || value is int || value is bool || value is double || value is float || value is long || value is short || value is byte || value is uint || value is ulong || value is ushort || value is sbyte || value is decimal)
        {
            return value;
        }
        if (value is IDictionary dictionary)
        {
            var newDict = new Dictionary<string, object>();
            foreach (var key in dictionary.Keys)
            {
                if (dictionary[key] is null)
                {
                    continue;
                }
                newDict.Add(key.ToString(), FilterNullValuesInternal(dictionary[key]));
            }
            return newDict;
        }
        if (value is IEnumerable array)
        {
            var newArray = new List<object>();
            foreach (var item in array)
            {
                newArray.Add(FilterNullValuesInternal(item));
            }
            return newArray;
        }
        if (value.GetType().IsEnum)
        {
            return value;
        }

        if (value is object obj)
        {
            var type = obj.GetType();
            var properties = type.GetProperties();
            var newDict = new Dictionary<string, object>();
            foreach (var property in properties)
            {
                var jsonProperty = property.GetCustomAttribute<JsonPropertyNameAttribute>();
                var propertyName = jsonProperty?.Name ?? ToCamelCase(property.Name);
                var propertyValue = property.GetValue(obj);
                if (propertyValue is null)
                {
                    continue;
                }
                newDict.Add(propertyName, FilterNullValuesInternal(propertyValue));
            }
            return newDict;
        }
        return value;
    }

    private static string ToCamelCase(string str)
    {
        if (string.IsNullOrEmpty(str))
        {
            return str;
        }

        return char.ToLowerInvariant(str[0]) + str.Substring(1);
    }
}
