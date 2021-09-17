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
using Microsoft.Playwright.NUnit.Configuration;

namespace Microsoft.Playwright.NUnit
{
    public class PlaywrightConfiguration
    {
        public static PlaywrightConfiguration Global { get; private set; } = new EnvironmentVariablePlaywrightConfiguration();

        /// <summary>
        /// This constructor should not be called by user-code, but exists here only because
        /// <see cref="System.Text.Json.JsonSerializer"/> does not know how to deserialize the type
        /// without a public constructor.
        /// </summary>
        public PlaywrightConfiguration() : this(null)
        {

        }

        internal PlaywrightConfiguration(PlaywrightConfiguration parent)
        {
            // doing this in the constructor ensures we *clone*, rather than reference
            BrowserNewContextOptions = new(parent?.BrowserNewContextOptions);
            BrowserTypeLaunchOptions = new(parent?.BrowserTypeLaunchOptions);
        }

        public static PlaywrightConfiguration UseEnvironmentVariables()
        {
            Global = new EnvironmentVariablePlaywrightConfiguration();
            return Global;
        }

        public static PlaywrightConfiguration UseFile(string path = null)
        {
            Global = new FilePlaywrightConfiguration(path ?? "playwright.config");
            return Global;
        }

        public virtual string BrowserName { get; set; } = Microsoft.Playwright.BrowserType.Chromium.ToLower();

        internal virtual PlaywrightConfiguration Cascade() => throw new NotImplementedException();

        public BrowserNewContextOptions BrowserNewContextOptions { get; private set; }
        public BrowserTypeLaunchOptions BrowserTypeLaunchOptions { get; private set; }
    }
}
