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

#nullable enable

namespace Microsoft.Playwright;

/// <summary>
/// <para>
/// <see cref="IConsoleMessage"/> objects are dispatched by page via the <see cref="IPage.Console"/>
/// event. For each console messages logged in the page there will be corresponding
/// event in the Playwright context.
/// </para>
/// <code>
/// // Listen for all System.out.printlns<br/>
/// page.Console += (_, msg) =&gt; Console.WriteLine(msg.Text);<br/>
/// <br/>
/// // Listen for all console events and handle errors<br/>
/// page.Console += (_, msg) =&gt;<br/>
/// {<br/>
///     if ("error".Equals(msg.Type))<br/>
///         Console.WriteLine("Error text: " + msg.Text);<br/>
/// };<br/>
/// <br/>
/// // Get the next System.out.println<br/>
/// var waitForMessageTask = page.WaitForConsoleMessageAsync();<br/>
/// await page.EvaluateAsync("console.log('hello', 42, { foo: 'bar' });");<br/>
/// var message = await waitForMessageTask;<br/>
/// // Deconstruct console.log arguments<br/>
/// await message.Args.ElementAt(0).JsonValueAsync&lt;string&gt;(); // hello<br/>
/// await message.Args.ElementAt(1).JsonValueAsync&lt;int&gt;(); // 42
/// </code>
/// </summary>
public partial interface IConsoleMessage
{
    /// <summary><para>List of arguments passed to a <c>console</c> function call. See also <see cref="IPage.Console"/>.</para></summary>
    IReadOnlyList<IJSHandle> Args { get; }

    /// <summary>
    /// <para>
    /// URL of the resource followed by 0-based line and column numbers in the resource
    /// formatted as <c>URL:line:column</c>.
    /// </para>
    /// </summary>
    string Location { get; }

    /// <summary><para>The text of the console message.</para></summary>
    string Text { get; }

    /// <summary>
    /// <para>
    /// One of the following values: <c>'log'</c>, <c>'debug'</c>, <c>'info'</c>, <c>'error'</c>,
    /// <c>'warning'</c>, <c>'dir'</c>, <c>'dirxml'</c>, <c>'table'</c>, <c>'trace'</c>,
    /// <c>'clear'</c>, <c>'startGroup'</c>, <c>'startGroupCollapsed'</c>, <c>'endGroup'</c>,
    /// <c>'assert'</c>, <c>'profile'</c>, <c>'profileEnd'</c>, <c>'count'</c>, <c>'timeEnd'</c>.
    /// </para>
    /// </summary>
    string Type { get; }
}

#nullable disable
