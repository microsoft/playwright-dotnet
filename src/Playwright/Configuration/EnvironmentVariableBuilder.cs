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

using System.Collections.Generic;

namespace Microsoft.Playwright;

internal static class EnvironmentVariableBuilder
{
    internal static Dictionary<string, string> BuildEnvironmentVariables(this PlaywrightCreateOptions playwrightCreateOptions)
        => new()
        {
            { "SELENIUM_REMOTE_URL", playwrightCreateOptions.SeleniumRemoteUri?.ToString() },
            { "http_proxy", playwrightCreateOptions.SeleniumRemoteProxyUri?.ToString() },
            { "https_proxy", playwrightCreateOptions.SeleniumRemoteProxyUri?.ToString() },
            { "PWDEBUG", playwrightCreateOptions.DebugMode.Parse() },
            { "DEBUG", playwrightCreateOptions.LoggingConfiguration },
        };


    private static string Parse(this DebugMode debugMode)
        => debugMode switch
        {
            DebugMode.Full => "1",
            DebugMode.Console => "console",
            DebugMode.None or _ => string.Empty,
        };
}
