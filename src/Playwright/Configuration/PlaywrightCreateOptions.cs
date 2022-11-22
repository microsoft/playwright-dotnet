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

namespace Microsoft.Playwright;

/// <summary>
/// Represents options available through Playwright environment variables.
/// </summary>
public class PlaywrightCreateOptions
{
    /// <summary>
    /// Selenium Grid address used for remote test execution.
    /// </summary>
    public Uri SeleniumRemoteUri { get; set; }

    /// <summary>
    /// Proxy address used for Selenium Grid connection.
    /// </summary>
    public Uri SeleniumRemoteProxyUri { get; set; }

    /// <summary>
    /// Debug mode set by PWDEBUG variable.
    /// </summary>
    public DebugMode DebugMode { get; set; }

    /// <summary>
    /// Defines the level of detail in debug logs outputted by Playwright.<br />
    /// "*" - displays all available logs.<br />
    /// "pw:browser*" - filters the logs starting with <i>pw:browser</i>.<br />
    /// Empty string disables logging.
    /// </summary>
    public string LoggingConfiguration { get; set; }
}
