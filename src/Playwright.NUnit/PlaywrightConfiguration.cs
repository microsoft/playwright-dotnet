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
    public abstract class PlaywrightConfiguration
    {
        public static PlaywrightConfiguration Current { get; private set; } = new EnvironmentVariablePlaywrightConfiguration();

        internal PlaywrightConfiguration()
        {

        }

        public static PlaywrightConfiguration UseEnvironmentVariables()
        {
            Current = new EnvironmentVariablePlaywrightConfiguration();
            return Current;
        }

        public abstract string BaseURL { get; }
        public abstract string BrowserName { get; }
        public abstract bool? BypassCSP { get; }
        public abstract string Channel { get; }
        public abstract bool? Headless { get; }
        public abstract ViewportSize ViewportSize { get; }
        public abstract float? SlowMo { get; }

        public OptionsBuilder<BrowserNewContextOptions> BrowserNewContextOptionsBuilder { get; } = new();

        public OptionsBuilder<BrowserTypeLaunchOptions> BrowserTypeLaunchOptionsBuilder { get; } = new();

        public class OptionsBuilder<T> where T : class
        {
            private Action<T> _conf;

            public void Configure(Action<T> conf)
            {
                _conf = conf;
            }

            internal T Build(T options)
            {
                _conf?.Invoke(options);
                return options;
            }
        }
    }
}
