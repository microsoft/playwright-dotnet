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
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

[assembly: InternalsVisibleToAttribute("Microsoft.Playwright.MSTest, PublicKey=0024000004800000940000000602000000240000525341310004000001000100059a04ca5ca77c9b4eb2addd1afe3f8464b20ee6aefe73b8c23c0e6ca278d1a378b33382e7e18d4aa8300dd22d81f146e528d88368f73a288e5b8157da9710fe6f9fa9911fb786193f983408c5ebae0b1ba5d1d00111af2816f5db55871db03d7536f4a7a6c5152d630c1e1886b1a0fb68ba5e7f64a7f24ac372090889be2ffb")]
[assembly: InternalsVisibleToAttribute("Microsoft.Playwright.NUnit, PublicKey=0024000004800000940000000602000000240000525341310004000001000100059a04ca5ca77c9b4eb2addd1afe3f8464b20ee6aefe73b8c23c0e6ca278d1a378b33382e7e18d4aa8300dd22d81f146e528d88368f73a288e5b8157da9710fe6f9fa9911fb786193f983408c5ebae0b1ba5d1d00111af2816f5db55871db03d7536f4a7a6c5152d630c1e1886b1a0fb68ba5e7f64a7f24ac372090889be2ffb")]

namespace Microsoft.Playwright.Core.Shared
{
    internal class RunSettingsParser
    {
        private readonly IDictionary<string, string> _settings;
        public BrowserTypeLaunchOptions LaunchOptions;
        public string BrowserName;

        internal RunSettingsParser(IDictionary<string, string> settings)
        {
            _settings = settings;
            LaunchOptions = ParseTestParameters<BrowserTypeLaunchOptions>(settings);
            BrowserName = DetermineBrowserType();
        }

        private static T ParseTestParameters<T>(IDictionary<string, string> parameters)
        where T : class, new()
        {
            var options = new T();
            foreach (var parameter in parameters)
            {
                if (!string.IsNullOrEmpty(parameter.Value))
                {
                    ApplyParameter(parameter.Key, parameter.Value, options);
                }
            }
            return options;
        }

        private static void ApplyParameter(string key, string value, object options)
        {
            // Try to match it by the exact name of the property.
            var property = options.GetType().GetProperty(key);
            if (property == null)
            {
                // If not found, we try to fallback to the JsonPropertyNameAttribute
                property = options.GetType().GetProperties().Where(p => p.GetCustomAttributes(typeof(JsonPropertyNameAttribute), false).Where(a => ((JsonPropertyNameAttribute)a).Name == key).Any()).FirstOrDefault();
                if (property == null)
                {
                    return;
                }
            }
            var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            switch (type)
            {
                case Type t when t == typeof(string):
                    property.SetValue(options, value);
                    break;
                case Type t when type == typeof(bool):
                    property.SetValue(options, bool.Parse(value));
                    break;
                case Type t when t == typeof(float):
                    property.SetValue(options, float.Parse(value));
                    break;
                case Type t when type?.IsEnum == true:
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
                        property.SetValue(options, Enum.Parse(type, enumValue));
                    }
                    break;
                // special case for IEnumerable<KeyValuePair<X, Y>> which we need to convert into a Dictionary<X, Y>
                case Type t when t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IEnumerable<>) && t.GetGenericArguments()[0].IsGenericType && t.GetGenericArguments()[0].GetGenericTypeDefinition() == typeof(KeyValuePair<,>):
                    {
                        var dictKvGenericTypes = type.GetGenericArguments()[0].GetGenericArguments();
                        var dictType = typeof(Dictionary<,>).MakeGenericType(dictKvGenericTypes[0], dictKvGenericTypes[1]);
                        property.SetValue(options, ParseAsJson(value, dictType));
                    }
                    break;
                default:
                    property.SetValue(options, ParseAsJson(value, type));
                    break;
            }
        }

        private static object ParseAsJson(string value, Type type)
        {
            return JsonSerializer.Deserialize(value.Replace('\'', '"'), type);
        }

        private static void ValidateBrowserName(string browserName)
        {
            if (browserName != Microsoft.Playwright.BrowserType.Chromium &&
                browserName != Microsoft.Playwright.BrowserType.Firefox &&
                browserName != Microsoft.Playwright.BrowserType.Webkit)
            {
                throw new ArgumentException($"Invalid browser name: {browserName}");
            }
        }

        private void ApplyDeviceOptions(BrowserNewContextOptions contextOptions, IPlaywright playwright)
        {
            if (!_settings.ContainsKey("device"))
            {
                return;
            }
            if (!playwright.Devices.ContainsKey(_settings["device"]))
            {
                throw new ArgumentException($"Device '{_settings["device"]}' not found!");
            }
            var pwDevice = playwright.Devices[_settings["device"]];
            contextOptions.UserAgent = pwDevice.UserAgent;
            contextOptions.ViewportSize = pwDevice.ViewportSize;
            contextOptions.DeviceScaleFactor = pwDevice.DeviceScaleFactor;
            contextOptions.IsMobile = pwDevice.IsMobile;
            contextOptions.HasTouch = pwDevice.HasTouch;
        }

        public BrowserNewContextOptions ContextOptions(IPlaywright playwright)
        {
            var contextOptions = ParseTestParameters<BrowserNewContextOptions>(_settings);
            ApplyDeviceOptions(contextOptions, playwright);
            return contextOptions;
        }

        private string DetermineBrowserType()
        {
            var browserFromEnv = Environment.GetEnvironmentVariable("BROWSER")?.ToLower();
            if (browserFromEnv != null)
            {
                ValidateBrowserName(browserFromEnv);
                return browserFromEnv;
            }
            if (_settings.TryGetValue("browser", out var browser))
            {
                browser = browser.ToLower();
                ValidateBrowserName(browser);
                return browser;
            }
            return Microsoft.Playwright.BrowserType.Chromium;
        }
    }
}
