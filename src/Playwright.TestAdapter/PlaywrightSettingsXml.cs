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
using System.Text.Json;
using System.Xml;

namespace Microsoft.Playwright.TestAdapter;

public class PlaywrightSettingsXml
{
    public PlaywrightSettingsXml()
    {
    }

    public PlaywrightSettingsXml(XmlReader reader)
    {
        // Skip Playwright root Element.
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
                    BrowserName = ReadTextElement(reader, "Playwright>BrowserName");
                    break;
                case "LaunchOptions":
                    LaunchOptions = ParseLaunchOptions(reader);
                    break;
                case "ExpectTimeout":
                    ExpectTimeout = ParseFloat(ReadTextElement(reader, "Playwright>ExpectTimeout")!);
                    break;
                case "Headless":
                    Headless = bool.Parse(ReadTextElement(reader, "Playwright>Headless")!);
                    break;
                case "Retries":
                    Retries = int.Parse(ReadTextElement(reader, "Playwright>Retries")!, CultureInfo.InvariantCulture);
                    break;
                default:
                    Console.WriteLine($"Playwright RunSettings Parsing Error: Playwright>{reader.Name} is not implemented");
                    SkipCurrentElement(reader);
                    break;
            }
        }
    }

    private static BrowserTypeLaunchOptions ParseLaunchOptions(XmlReader reader)
    {
        var endTag = reader.Name;
        var options = new BrowserTypeLaunchOptions();
        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.EndElement && reader.Name == endTag)
            {
                break;
            }
            if (reader.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            var key = reader.Name;
            var path = $"Playwright>{endTag}>{key}";
            switch (key)
            {
                case nameof(BrowserTypeLaunchOptions.Args):
                    options.Args = ParseStringList(ReadTextElement(reader, path)!);
                    break;
                case nameof(BrowserTypeLaunchOptions.ArtifactsDir):
                    options.ArtifactsDir = ReadTextElement(reader, path);
                    break;
                case nameof(BrowserTypeLaunchOptions.Channel):
                    options.Channel = ReadTextElement(reader, path);
                    break;
                case nameof(BrowserTypeLaunchOptions.ChromiumSandbox):
                    options.ChromiumSandbox = bool.Parse(ReadTextElement(reader, path)!);
                    break;
                case nameof(BrowserTypeLaunchOptions.DownloadsPath):
                    options.DownloadsPath = ReadTextElement(reader, path);
                    break;
                case nameof(BrowserTypeLaunchOptions.Env):
                    options.Env = ParseStringDictionary(ReadTextElement(reader, path)!);
                    break;
                case nameof(BrowserTypeLaunchOptions.ExecutablePath):
                    options.ExecutablePath = ReadTextElement(reader, path);
                    break;
                case nameof(BrowserTypeLaunchOptions.FirefoxUserPrefs):
                    options.FirefoxUserPrefs = ParseObjectDictionary(ReadTextElement(reader, path)!);
                    break;
                case nameof(BrowserTypeLaunchOptions.HandleSIGHUP):
                    options.HandleSIGHUP = bool.Parse(ReadTextElement(reader, path)!);
                    break;
                case nameof(BrowserTypeLaunchOptions.HandleSIGINT):
                    options.HandleSIGINT = bool.Parse(ReadTextElement(reader, path)!);
                    break;
                case nameof(BrowserTypeLaunchOptions.HandleSIGTERM):
                    options.HandleSIGTERM = bool.Parse(ReadTextElement(reader, path)!);
                    break;
                case nameof(BrowserTypeLaunchOptions.Headless):
                    options.Headless = bool.Parse(ReadTextElement(reader, path)!);
                    break;
                case nameof(BrowserTypeLaunchOptions.IgnoreAllDefaultArgs):
                    options.IgnoreAllDefaultArgs = bool.Parse(ReadTextElement(reader, path)!);
                    break;
                case nameof(BrowserTypeLaunchOptions.IgnoreDefaultArgs):
                    options.IgnoreDefaultArgs = ParseStringList(ReadTextElement(reader, path)!);
                    break;
                case nameof(BrowserTypeLaunchOptions.Proxy):
                    options.Proxy = ParseProxy(reader);
                    break;
                case nameof(BrowserTypeLaunchOptions.SlowMo):
                    options.SlowMo = ParseFloat(ReadTextElement(reader, path)!);
                    break;
                case nameof(BrowserTypeLaunchOptions.Timeout):
                    options.Timeout = ParseFloat(ReadTextElement(reader, path)!);
                    break;
                case nameof(BrowserTypeLaunchOptions.TracesDir):
                    options.TracesDir = ReadTextElement(reader, path);
                    break;
                default:
                    Console.WriteLine($"Playwright RunSettings Parsing Error: {path} is not supported");
                    SkipCurrentElement(reader);
                    break;
            }
        }

        return options;
    }

    private static Proxy ParseProxy(XmlReader reader)
    {
        var endTag = reader.Name;
        var proxy = new Proxy();
        while (reader.Read())
        {
            if (reader.NodeType == XmlNodeType.EndElement && reader.Name == endTag)
            {
                break;
            }
            if (reader.NodeType != XmlNodeType.Element)
            {
                continue;
            }

            var key = reader.Name;
            var path = $"Playwright>LaunchOptions>{endTag}>{key}";
            switch (key)
            {
                case nameof(Proxy.Server):
                    proxy.Server = ReadTextElement(reader, path)!;
                    break;
                case nameof(Proxy.Bypass):
                    proxy.Bypass = ReadTextElement(reader, path);
                    break;
                case nameof(Proxy.Username):
                    proxy.Username = ReadTextElement(reader, path);
                    break;
                case nameof(Proxy.Password):
                    proxy.Password = ReadTextElement(reader, path);
                    break;
                default:
                    Console.WriteLine($"Playwright RunSettings Parsing Error: {path} is not supported");
                    SkipCurrentElement(reader);
                    break;
            }
        }

        return proxy;
    }

    private static string[] ParseStringList(string value)
        => JsonSerializer.Deserialize(NormalizeJson(value), TestAdapterJsonContext.Default.StringArray)!;

    private static Dictionary<string, string> ParseStringDictionary(string value)
        => JsonSerializer.Deserialize(NormalizeJson(value), TestAdapterJsonContext.Default.DictionaryOfStringToString)!;

    private static Dictionary<string, object> ParseObjectDictionary(string value)
        => JsonSerializer.Deserialize(NormalizeJson(value), TestAdapterJsonContext.Default.DictionaryOfStringToObject)!;

    private static float ParseFloat(string value)
        => float.Parse(value, CultureInfo.InvariantCulture);

    private static string NormalizeJson(string value)
        => value.Replace('\'', '"');

    private static string? ReadTextElement(XmlReader reader, string path)
    {
        var elementName = reader.Name;
        if (reader.IsEmptyElement)
        {
            Console.WriteLine($"Playwright RunSettings Parsing Error: {path} is not supported");
            return null;
        }

        if (!reader.Read() || reader.NodeType != XmlNodeType.Text)
        {
            Console.WriteLine($"Playwright RunSettings Parsing Error: {path} is not supported");
            SkipToEndElement(reader, elementName);
            return null;
        }

        var value = reader.Value;
        SkipToEndElement(reader, elementName);
        return value;
    }

    private static void SkipCurrentElement(XmlReader reader)
    {
        if (!reader.IsEmptyElement)
        {
            SkipToEndElement(reader, reader.Name);
        }
    }

    private static void SkipToEndElement(XmlReader reader, string elementName)
    {
        while (!(reader.NodeType == XmlNodeType.EndElement && reader.Name == elementName) && reader.Read())
        {
        }
    }

    public BrowserTypeLaunchOptions? LaunchOptions { get; set; }
    public string? BrowserName { get; set; }
    public bool? Headless { get; set; }
    public float? ExpectTimeout { get; set; }
    public int? Retries { get; set; }
}
