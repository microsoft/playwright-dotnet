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
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace Microsoft.Playwright
{
    /// <summary>
    /// <para>
    /// API for collecting and saving Playwright traces. Playwright traces can be opened
    /// using the Playwright CLI after Playwright script runs.
    /// </para>
    /// <para>Start with specifying the folder traces will be stored in:</para>
    /// <code>
    /// await using var browser = playwright.Chromium.LaunchAsync();<br/>
    /// await using var context = await browser.NewContextAsync();<br/>
    /// await context.Tracing.StartAsync(new TracingStartOptions<br/>
    /// {<br/>
    ///   Screenshots: true,<br/>
    ///   Snapshots: true<br/>
    /// });<br/>
    /// var page = context.NewPageAsync();<br/>
    /// await page.GotoAsync("https://playwright.dev");<br/>
    /// await context.Tracing.StopAsync(new TracingStopOptions<br/>
    /// {<br/>
    ///   Path: "trace.zip"<br/>
    /// });
    /// </code>
    /// </summary>
    public partial interface ITracing
    {
        /// <summary>
        /// <para>Start tracing.</para>
        /// <code>
        /// await using var browser = playwright.Chromium.LaunchAsync();<br/>
        /// await using var context = await browser.NewContextAsync();<br/>
        /// await context.Tracing.StartAsync(new TracingStartOptions<br/>
        /// {<br/>
        ///   Screenshots: true,<br/>
        ///   Snapshots: true<br/>
        /// });<br/>
        /// var page = context.NewPageAsync();<br/>
        /// await page.GotoAsync("https://playwright.dev");<br/>
        /// await context.Tracing.StopAsync(new TracingStopOptions<br/>
        /// {<br/>
        ///   Path: "trace.zip"<br/>
        /// });
        /// </code>
        /// </summary>
        /// <param name="options">Call options</param>
        Task StartAsync(TracingStartOptions? options = default);

        /// <summary><para>Stop tracing.</para></summary>
        /// <param name="options">Call options</param>
        Task StopAsync(TracingStopOptions? options = default);
    }
}

#nullable disable
