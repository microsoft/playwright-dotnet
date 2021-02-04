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
 *
 *
 * ------------------------------------------------------------------------------ 
 * <auto-generated> 
 * This code was generated by a tool at:
 * /utils/doclint/generateDotnetApi.js
 * 
 * Changes to this file may cause incorrect behavior and will be lost if 
 * the code is regenerated. 
 * </auto-generated> 
 * ------------------------------------------------------------------------------
 */
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace PlaywrightSharp
{
    /// <summary>
	/// The Worker class represents a <a href="https://developer.mozilla.org/en-US/docs/Web/API/Web_Workers_API">WebWorker</a>. `worker`
	/// event is emitted on the page object to signal a worker creation. `close` event is emitted on the worker object when the worker
	/// is gone.
	/// </summary>
	public partial interface IWorker
	{
		event EventHandler<IWorker> Close;
		/// <summary>
		/// Returns the return value of {PARAM}
		/// If the function passed to the `worker.evaluate` returns a [Promise], then `worker.evaluate` would wait for the promise to resolve and return its value.
		/// If the function passed to the `worker.evaluate` returns a non-[Serializable] value, then `worker.evaluate` returns `undefined`. DevTools Protocol also supports transferring some additional values that are not serializable by `JSON`: `-0`, `NaN`, `Infinity`, `-Infinity`, and bigint literals.
		/// </summary>
		Task<T> EvaluateAsync<T>(object arg);
		/// <summary>
		/// Returns the return value of {PARAM} as in-page object (JSHandle).
		/// The only difference between `worker.evaluate` and `worker.evaluateHandle` is that `worker.evaluateHandle` returns in-page
		/// object (JSHandle).
		/// If the function passed to the `worker.evaluateHandle` returns a [Promise], then `worker.evaluateHandle` would wait for the promise to resolve and return its value.
		/// </summary>
		Task<IJSHandle> EvaluateHandleAsync(object arg);
		string GetUrl();
	}
}