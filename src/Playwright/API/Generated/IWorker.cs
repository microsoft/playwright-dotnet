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
using System.Threading.Tasks;

#nullable enable

namespace Microsoft.Playwright;

/// <summary>
/// <para>
/// The Worker class represents a <a href="https://developer.mozilla.org/en-US/docs/Web/API/Web_Workers_API">WebWorker</a>.
/// <c>worker</c> event is emitted on the page object to signal a worker creation. <c>close</c>
/// event is emitted on the worker object when the worker is gone.
/// </para>
/// <code>
/// page.Worker += (_, worker) =&gt;<br/>
/// {<br/>
///     Console.WriteLine($"Worker created: {worker.Url}");<br/>
///     worker.Close += (_, _) =&gt; Console.WriteLine($"Worker closed {worker.Url}");<br/>
/// };<br/>
/// <br/>
/// Console.WriteLine("Current Workers:");<br/>
/// foreach(var pageWorker in page.Workers)<br/>
/// {<br/>
///     Console.WriteLine($"\tWorker: {pageWorker.Url}");<br/>
/// }
/// </code>
/// </summary>
public partial interface IWorker
{
    /// <summary>
    /// <para>
    /// Emitted when this dedicated <a href="https://developer.mozilla.org/en-US/docs/Web/API/Web_Workers_API">WebWorker</a>
    /// is terminated.
    /// </para>
    /// </summary>
    event EventHandler<IWorker> Close;

    /// <summary>
    /// <para>Returns the return value of <paramref name="expression"/>.</para>
    /// <para>
    /// If the function passed to the <see cref="IWorker.EvaluateAsync"/> returns a <see
    /// cref="Task"/>, then <see cref="IWorker.EvaluateAsync"/> would wait for the promise
    /// to resolve and return its value.
    /// </para>
    /// <para>
    /// If the function passed to the <see cref="IWorker.EvaluateAsync"/> returns a non-<see
    /// cref="Serializable"/> value, then <see cref="IWorker.EvaluateAsync"/> returns <c>undefined</c>.
    /// Playwright also supports transferring some additional values that are not serializable
    /// by <c>JSON</c>: <c>-0</c>, <c>NaN</c>, <c>Infinity</c>, <c>-Infinity</c>.
    /// </para>
    /// </summary>
    /// <param name="expression">
    /// JavaScript expression to be evaluated in the browser context. If the expresion evaluates
    /// to a function, the function is automatically invoked.
    /// </param>
    /// <param name="arg">Optional argument to pass to <paramref name="expression"/>.</param>
    Task<T> EvaluateAsync<T>(string expression, object? arg = default);

    /// <summary>
    /// <para>Returns the return value of <paramref name="expression"/> as a <see cref="IJSHandle"/>.</para>
    /// <para>
    /// The only difference between <see cref="IWorker.EvaluateAsync"/> and <see cref="IWorker.EvaluateHandleAsync"/>
    /// is that <see cref="IWorker.EvaluateHandleAsync"/> returns <see cref="IJSHandle"/>.
    /// </para>
    /// <para>
    /// If the function passed to the <see cref="IWorker.EvaluateHandleAsync"/> returns
    /// a <see cref="Task"/>, then <see cref="IWorker.EvaluateHandleAsync"/> would wait
    /// for the promise to resolve and return its value.
    /// </para>
    /// </summary>
    /// <param name="expression">
    /// JavaScript expression to be evaluated in the browser context. If the expresion evaluates
    /// to a function, the function is automatically invoked.
    /// </param>
    /// <param name="arg">Optional argument to pass to <paramref name="expression"/>.</param>
    Task<IJSHandle> EvaluateHandleAsync(string expression, object? arg = default);

    string Url { get; }
}

#nullable disable
