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
using System.IO;
using System.Text.Json.Serialization;

namespace Microsoft.Playwright.NUnit.Configuration
{
    internal class FilePlaywrightConfiguration : PlaywrightConfiguration
    {
        private readonly string _path;
        readonly Lazy<PlaywrightConfiguration> _playwrightConfiguration;

        public FilePlaywrightConfiguration(string path) : base(null) // we won't let this actually inherit, as it's file based
        {
            if (!File.Exists(path))
                throw new InvalidOperationException($"Configuration {path} does not exist.");

            _path = path;
            _playwrightConfiguration = new(() =>
            {
                var confText = File.ReadAllText(path);
                return System.Text.Json.JsonSerializer.Deserialize<PlaywrightConfiguration>(confText, new() { });
            });
        }

        private string _browserName = null;
        public override string BrowserName
        {
            get
            {
                if (_browserName is null)
                    return (_playwrightConfiguration.Value.BrowserName ?? base.BrowserName).ToLower();

                return _browserName;
            }

            set
            {
                _browserName = value?.ToLower();
            }
        }

        internal override PlaywrightConfiguration Cascade() => new FilePlaywrightConfiguration(_path);
    }
}
