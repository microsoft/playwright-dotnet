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


#nullable enable

namespace Microsoft.Playwright;

/// <summary>
/// <para>
/// <see cref="IWebError"/> class represents an unhandled exception thrown in the page.
/// It is dispatched via the <see cref="IBrowserContext.WebError"/> event.
/// </para>
/// <code>
/// // Log all uncaught errors to the terminal<br/>
/// context.WebError += (_, webError) =&gt;<br/>
/// {<br/>
///   Console.WriteLine("Uncaught exception: " + webError.Error);<br/>
/// };
/// </code>
/// </summary>
public partial interface IWebError
{
    /// <summary><para>The page that produced this unhandled exception, if any.</para></summary>
    IPage? Page { get; }

    /// <summary><para>Unhandled error that was thrown.</para></summary>
    string Error { get; }
}

#nullable disable
