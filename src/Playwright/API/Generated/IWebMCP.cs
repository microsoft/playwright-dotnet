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

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Playwright;

/// <summary>
/// <para>
/// <see cref="IWebMCP"/> exposes the tools that a frame registers through the experimental
/// <see cref="IWebMCP"/> browser API, <c>navigator.modelContext</c>. It lists the tools
/// and calls them.
/// </para>
/// <para>
/// Instances are accessed through <see cref="IFrame.Webmcp"/>. <see cref="IPage.Webmcp"/>
/// is the instance of the main frame.
/// </para>
/// <para>
/// WebMCP is an experimental browser feature. Chromium enables it with the <c>--enable-features=WebMCP</c>
/// launch argument, Firefox with the <c>dom.modelcontext.enabled</c> and <c>dom.modelcontext.testing.enabled</c>
/// preferences. WebKit does not implement it.
/// </para>
/// <para>
/// Tool names, descriptions, input schemas and results are provided by the page, so
/// treat them as untrusted input.
/// </para>
/// </summary>
/// <remarks>
/// <para>
/// WebMCP is an experimental browser feature. Chromium enables it with the <c>--enable-features=WebMCP</c>
/// launch argument, Firefox with the <c>dom.modelcontext.enabled</c> and <c>dom.modelcontext.testing.enabled</c>
/// preferences. WebKit does not implement it.
/// </para>
/// </remarks>
public partial interface IWebMCP
{
    /// <summary>
    /// <para>
    /// Calls a tool registered by the frame and returns its result. The result is whatever
    /// the tool's <c>execute</c> function resolved to, typically an object with a <c>content</c>
    /// array. A result with <c>isError: true</c> is returned as is. The method throws when
    /// the tool is not registered or its <c>execute</c> function throws.
    /// </para>
    /// </summary>
    /// <param name="name">Name of the tool, as reported by <see cref="IWebMCP.ToolsAsync"/>.</param>
    /// <param name="input">Input for the tool, matching its <c>inputSchema</c>. Defaults to an empty object.</param>
    /// <param name="options">Call options</param>
    Task<T> CallToolAsync<T>(string name, object? input = default, WebMCPCallToolOptions? options = default);

    /// <summary>
    /// <para>
    /// Returns the tools currently registered by the frame. Throws if the browser was launched
    /// without WebMCP support, see the note above for the launch options that enable it.
    /// </para>
    /// <para>
    /// <see cref="IPage.Webmcp"/> covers the main frame only, child frames list their tools
    /// through their own <see cref="IFrame.Webmcp"/>.
    /// </para>
    /// </summary>
    /// <param name="options">Call options</param>
    Task<IReadOnlyList<WebMCPTool>> ToolsAsync(WebMCPToolsOptions? options = default);
}
