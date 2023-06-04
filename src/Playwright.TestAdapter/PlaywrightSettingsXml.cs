/*
 * MIT License
 *
 * Copyright (c) Microsoft Corporation.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
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
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Xml;

namespace Microsoft.Playwright.TestAdapter;

public class PlaywrightSettingsXml
{
    public PlaywrightSettingsXml(XmlReader reader)
    {
        // Skip Playwright root Element
        reader.Read();
        while (reader.Read())
        {
            if (reader.NodeType != XmlNodeType.Element)
            {
                continue;
            }
            switch (reader.Name)
            {
                case "BrowserName":
                    reader.Read();
                    BrowserName = reader.Value;
                    break;
                case "LaunchOptions":
                    LaunchOptions = (BrowserTypeLaunchOptions)ParseXmlIntoClass(typeof(BrowserTypeLaunchOptions), reader);
                    break;
                case "ExpectTimeout":
                    reader.Read();
                    ExpectTimeout = float.Parse(reader.Value, CultureInfo.InvariantCulture);
                    break;
                case "Retries":
                    reader.Read();
                    Retries = int.Parse(reader.Value, CultureInfo.InvariantCulture);
                    break;
                default:
                    Console.WriteLine($"Playwright RunSettings Parsing Error: Playwright>{reader.Name} is not implemented");
                    break;
            }
        }
    }

    private static object ParseXmlIntoClass(Type classType, XmlReader reader)
    {
        var endTag = reader.Name;
        var options = Activator.CreateInstance(classType);
        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.EndElement && reader.Name == endTag)
            {
                break;
            }
            if (reader.NodeType == XmlNodeType.Element)
            {
                var key = reader.Name;
                var property = classType.GetProperty(key);
                if (property == null)
                {
                    Console.WriteLine($"Playwright RunSettings Parsing Error: Playwright>{endTag}>{key} is not supported");
                    continue;
                }
                var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
                var isArrayLike = type.IsArray || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>));
                if (type.IsPrimitive || type == typeof(string) || isArrayLike)
                {
                    reader.Read();
                    if (reader.NodeType != XmlNodeType.Text)
                    {
                        Console.WriteLine($"Playwright RunSettings Parsing Error: Playwright>{endTag}>{key} is not supported");
                        continue;
                    }
                    ApplyParameter(key, reader.Value, options);
                }
                else
                {
                    property.SetValue(options, ParseXmlIntoClass(type, reader));
                }
            }
        }
        return options;
    }

    private static void ApplyParameter(string key, string value, object options)
    {
        var property = options.GetType().GetProperty(key);
        if (property == null)
        {
            return;
        }
        var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
        switch (type)
        {
            case Type t when t == typeof(string):
                property.SetValue(options, value);
                break;
            case Type t when t == typeof(bool):
                property.SetValue(options, bool.Parse(value));
                break;
            case Type t when t == typeof(float):
                property.SetValue(options, float.Parse(value, CultureInfo.InvariantCulture));
                break;
            case Type t when t?.IsEnum == true:
                {
                    var enumValue = Enum.GetNames(t).Where(name =>
                    {
                        var field = t.GetField(name);
                        return field.GetCustomAttribute<EnumMemberAttribute>()?.Value == value;
                    }).FirstOrDefault();
                    if (enumValue == null)
                    {
                        throw new ArgumentException($"Invalid value '{value}' for enum {t.Name}");
                    }
                    property.SetValue(options, Enum.Parse(t, enumValue));
                }
                break;
            // special case for IEnumerable<KeyValuePair<X, Y>> which we need to convert into a Dictionary<X, Y>
            case Type t when t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>) && t.GetGenericArguments()[0].IsGenericType && t.GetGenericArguments()[0].GetGenericTypeDefinition() == typeof(KeyValuePair<,>):
                {
                    var dictKvGenericTypes = t.GetGenericArguments()[0].GetGenericArguments();
                    var dictType = typeof(Dictionary<,>).MakeGenericType(dictKvGenericTypes[0], dictKvGenericTypes[1]);
                    property.SetValue(options, ParseAsJson(value, dictType));
                }
                break;
            default:
                {
                    property.SetValue(options, ParseAsJson(value, type));
                    break;
                }
        }
    }

    private static object ParseAsJson(string value, Type type)
    {
        return JsonSerializer.Deserialize(value.Replace('\'', '"'), type)!;
    }

    public BrowserTypeLaunchOptions? LaunchOptions { get; private set; }
    public string? BrowserName { get; private set; }
    public bool? Headless { get; private set; }
    public float? ExpectTimeout { get; private set; }
    public int? Retries { get; private set; }
}

