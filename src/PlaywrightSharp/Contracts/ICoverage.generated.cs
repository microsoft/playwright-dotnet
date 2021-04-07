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
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace PlaywrightSharp
{
    /// <summary>
	/// <para>
	/// Coverage gathers information about parts of JavaScript and CSS that were used by
	/// the page.
	/// </para>
	/// <para>An example of using JavaScript coverage to produce Istanbul report for page load:</para>
	/// </summary>
	/// <remarks><para>Coverage APIs are only supported on Chromium-based browsers.</para></remarks>
	public partial interface ICoverage
	{
		/// <summary><para>Returns coverage is started</para></summary>
		/// <param name="resetOnNavigation">Whether to reset coverage on every navigation. Defaults to <c>true</c>.</param>
		Task StartCSSCoverageAsync(bool? resetOnNavigation = default);
	
		/// <summary><para>Returns coverage is started</para></summary>
		/// <remarks>
		/// <para>
		/// Anonymous scripts are ones that don't have an associated url. These are scripts
		/// that are dynamically created on the page using <c>eval</c> or <c>new Function</c>.
		/// If <paramref name="reportAnonymousScripts"/> is set to <c>true</c>, anonymous scripts
		/// will have <c>__playwright_evaluation_script__</c> as their URL.
		/// </para>
		/// </remarks>
		/// <param name="resetOnNavigation">Whether to reset coverage on every navigation. Defaults to <c>true</c>.</param>
		/// <param name="reportAnonymousScripts">
		/// Whether anonymous scripts generated by the page should be reported. Defaults to
		/// <c>false</c>.
		/// </param>
		Task StartJSCoverageAsync(bool? resetOnNavigation = default, bool? reportAnonymousScripts = default);
	
		/// <summary><para>Returns the array of coverage reports for all stylesheets</para></summary>
		/// <remarks><para>CSS Coverage doesn't include dynamically injected style tags without sourceURLs.</para></remarks>
		Task<IReadOnlyCollection<CoverageStopCSSCoverageResult>> StopCSSCoverageAsync();
	
		/// <summary><para>Returns the array of coverage reports for all scripts</para></summary>
		/// <remarks>
		/// <para>
		/// JavaScript Coverage doesn't include anonymous scripts by default. However, scripts
		/// with sourceURLs are reported.
		/// </para>
		/// </remarks>
		Task<IReadOnlyCollection<CoverageStopJSCoverageResult>> StopJSCoverageAsync();
	}
}
