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
    /// FrameLocator represents a view to the <c>iframe</c> on the page. It captures the
    /// logic sufficient to retrieve the <c>iframe</c> and locate elements in that iframe.
    /// FrameLocator can be created with either <see cref="IPage.FrameLocator"/> or <see
    /// cref="ILocator.FrameLocator"/> method.
    /// </para>
    /// <code>
    /// var locator = page.FrameLocator("#my-frame").Locator("text=Submit");<br/>
    /// await locator.ClickAsync();
    /// </code>
    /// <para>**Strictness**</para>
    /// <para>
    /// Frame locators are strict. This means that all operations on frame locators will
    /// throw if more than one element matches given selector.
    /// </para>
    /// <code>
    /// // Throws if there are several frames in DOM:<br/>
    /// await page.FrameLocator(".result-frame").Locator("button").ClickAsync();<br/>
    /// <br/>
    /// // Works because we explicitly tell locator to pick the first frame:<br/>
    /// await page.FrameLocator(".result-frame").First.Locator("button").ClickAsync();
    /// </code>
    /// </summary>
    public partial interface IFrameLocator
    {
        /// <summary><para>Returns locator to the first matching frame.</para></summary>
        IFrameLocator First { get; }

        /// <summary>
        /// <para>
        /// When working with iframes, you can create a frame locator that will enter the iframe
        /// and allow selecting elements in that iframe.
        /// </para>
        /// </summary>
        /// <param name="selector">
        /// A selector to use when resolving DOM element. See <a href="./selectors.md">working
        /// with selectors</a> for more details.
        /// </param>
        IFrameLocator FrameLocator(string selector);

        /// <summary><para>Returns locator to the last matching frame.</para></summary>
        IFrameLocator Last { get; }

        /// <summary>
        /// <para>
        /// The method finds an element matching the specified selector in the FrameLocator's
        /// subtree.
        /// </para>
        /// </summary>
        /// <param name="selector">
        /// A selector to use when resolving DOM element. See <a href="./selectors.md">working
        /// with selectors</a> for more details.
        /// </param>
        ILocator Locator(string selector);

        /// <summary><para>Returns locator to the n-th matching frame.</para></summary>
        /// <param name="index">
        /// </param>
        IFrameLocator Nth(int index);
    }
}

#nullable disable
