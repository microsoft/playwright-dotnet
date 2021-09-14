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

namespace Microsoft.Playwright.NUnit.Configuration
{
    internal class EnvironmentVariablePlaywrightConfiguration : PlaywrightConfiguration
    {
        public override string BaseURL => Environment.GetEnvironmentVariable("BASEURL");

        public override string BrowserName => (Environment.GetEnvironmentVariable("BROWSER") ?? Microsoft.Playwright.BrowserType.Chromium).ToLower();

        public override bool? BypassCSP => bool.TryParse(Environment.GetEnvironmentVariable("BYPASSCSP"), out bool bypassCSP) ? bypassCSP : null;

        public override string Channel => Environment.GetEnvironmentVariable("CHANNEL");

        public override bool? Headless => Environment.GetEnvironmentVariable("HEADED") != "1";

        public override ViewportSize ViewportSize => null;

        public override float? SlowMo => float.TryParse(Environment.GetEnvironmentVariable("SLOWMO"), out float slowMo) ? slowMo : null;
    }
}
