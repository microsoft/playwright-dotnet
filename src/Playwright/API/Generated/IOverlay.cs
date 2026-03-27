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

namespace Microsoft.Playwright;

/// <summary>
/// <para>
/// Interface for managing page overlays that display persistent visual indicators on
/// top of the page.
/// </para>
/// </summary>
public partial interface IOverlay
{
    /// <summary>
    /// <para>
    /// Adds an overlay with the given HTML content. The overlay is displayed on top of
    /// the page until removed. Returns a disposable that removes the overlay when disposed.
    /// </para>
    /// </summary>
    /// <param name="html">HTML content for the overlay.</param>
    /// <param name="options">Call options</param>
    Task<IAsyncDisposable> ShowAsync(string html, OverlayShowOptions? options = default);

    /// <summary>
    /// <para>
    /// Shows a chapter overlay with a title and optional description, centered on the page
    /// with a blurred backdrop. Useful for narrating video recordings. The overlay is removed
    /// after the specified duration, or 2000ms.
    /// </para>
    /// </summary>
    /// <param name="title">Title text displayed prominently in the overlay.</param>
    /// <param name="options">Call options</param>
    Task ChapterAsync(string title, OverlayChapterOptions? options = default);

    /// <summary><para>Sets visibility of all overlays without removing them.</para></summary>
    /// <param name="visible">Whether overlays should be visible.</param>
    Task SetVisibleAsync(bool visible);
}
