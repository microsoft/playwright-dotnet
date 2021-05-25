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
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Playwright
{
    /// <summary>
    /// <para>BrowserContexts provide a way to operate multiple independent browser sessions.</para>
    /// <para>
    /// If a page opens another page, e.g. with a <c>window.open</c> call, the popup will
    /// belong to the parent page's browser context.
    /// </para>
    /// <para>
    /// Playwright allows creation of "incognito" browser contexts with <c>browser.newContext()</c>
    /// method. "Incognito" browser contexts don't write any browsing data to disk.
    /// </para>
    /// </summary>
    public partial interface IBrowserContext : IAsyncDisposable
    {
        Task<T> WaitForEventAsync<T>(PlaywrightEvent<T> playwrightEvent, WaitForEventOptions<T> options = default);

        Task<T> RunAndWaitForEventAsync<T>(PlaywrightEvent<T> playwrightEvent, Func<Task> action, WaitForEventOptions<T> options = default);

        /// <inheritdoc cref="ExposeBindingAsync(string, Action, BrowserContextExposeBindingOptions)"/>
        Task ExposeBindingAsync(string name, Action<BindingSource> callback);

        /// <inheritdoc cref="ExposeBindingAsync(string, Action, BrowserContextExposeBindingOptions)"/>
        Task ExposeBindingAsync<T>(string name, Action<BindingSource, T> callback);

        /// <inheritdoc cref="ExposeBindingAsync(string, Action, BrowserContextExposeBindingOptions)"/>
        Task ExposeBindingAsync<TResult>(string name, Func<BindingSource, TResult> callback);

        /// <inheritdoc cref="ExposeBindingAsync(string, Action, BrowserContextExposeBindingOptions)"/>
        Task ExposeBindingAsync<TResult>(string name, Func<BindingSource, IJSHandle, TResult> callback);

        /// <inheritdoc cref="ExposeBindingAsync(string, Action, BrowserContextExposeBindingOptions)"/>
        Task ExposeBindingAsync<T, TResult>(string name, Func<BindingSource, T, TResult> callback);

        /// <inheritdoc cref="ExposeBindingAsync(string, Action, BrowserContextExposeBindingOptions)"/>
        Task ExposeBindingAsync<T1, T2, TResult>(string name, Func<BindingSource, T1, T2, TResult> callback);

        /// <inheritdoc cref="ExposeBindingAsync(string, Action, BrowserContextExposeBindingOptions)"/>
        Task ExposeBindingAsync<T1, T2, T3, TResult>(string name, Func<BindingSource, T1, T2, T3, TResult> callback);

        /// <inheritdoc cref="ExposeBindingAsync(string, Action, BrowserContextExposeBindingOptions)"/>
        Task ExposeBindingAsync<T1, T2, T3, T4, TResult>(string name, Func<BindingSource, T1, T2, T3, T4, TResult> callback);

        /// <inheritdoc cref="ExposeFunctionAsync(string, Action)"/>
        Task ExposeFunctionAsync<T>(string name, Action<T> callback);

        /// <inheritdoc cref="ExposeFunctionAsync(string, Action)"/>
        Task ExposeFunctionAsync<TResult>(string name, Func<TResult> callback);

        /// <inheritdoc cref="ExposeFunctionAsync(string, Action)"/>
        Task ExposeFunctionAsync<T, TResult>(string name, Func<T, TResult> callback);

        /// <inheritdoc cref="ExposeFunctionAsync(string, Action)"/>
        Task ExposeFunctionAsync<T1, T2, TResult>(string name, Func<T1, T2, TResult> callback);

        /// <inheritdoc cref="ExposeFunctionAsync(string, Action)"/>
        Task ExposeFunctionAsync<T1, T2, T3, TResult>(string name, Func<T1, T2, T3, TResult> callback);

        /// <inheritdoc cref="ExposeFunctionAsync(string, Action)"/>
        Task ExposeFunctionAsync<T1, T2, T3, T4, TResult>(string name, Func<T1, T2, T3, T4, TResult> callback);
    }
}
