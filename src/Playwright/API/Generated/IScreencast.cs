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

/// <summary><para>Interface for capturing screencast frames from a page.</para></summary>
public partial interface IScreencast
{
    /// <summary>
    /// <para>
    /// Starts the screencast. When <see cref="IScreencast.StartAsync"/> is provided, it
    /// saves video recording to the specified file. When <see cref="IScreencast.StartAsync"/>
    /// is provided, delivers JPEG-encoded frames to the callback. Both can be used together.
    /// </para>
    /// <para>**Usage**</para>
    /// </summary>
    /// <param name="options">Call options</param>
    Task<IAsyncDisposable> StartAsync(ScreencastStartOptions? options = default);

    /// <summary>
    /// <para>
    /// Stops the screencast and video recording if active. If a video was being recorded,
    /// saves it to the path specified in <see cref="IScreencast.StartAsync"/>.
    /// </para>
    /// </summary>
    Task StopAsync();

    /// <summary>
    /// <para>
    /// Adds an overlay with the given HTML content. The overlay is displayed on top of
    /// the page until removed. Returns a disposable that removes the overlay when disposed.
    /// </para>
    /// </summary>
    /// <param name="html">HTML content for the overlay.</param>
    /// <param name="options">Call options</param>
    Task<IAsyncDisposable> ShowOverlayAsync(string html, ScreencastShowOverlayOptions? options = default);

    /// <summary>
    /// <para>
    /// Shows a chapter overlay with a title and optional description, centered on the page
    /// with a blurred backdrop. Useful for narrating video recordings. The overlay is removed
    /// after the specified duration, or 2000ms.
    /// </para>
    /// </summary>
    /// <param name="title">Title text displayed prominently in the overlay.</param>
    /// <param name="options">Call options</param>
    Task ShowChapterAsync(string title, ScreencastShowChapterOptions? options = default);

    /// <summary>
    /// <para>
    /// Enables visual annotations on interacted elements. Returns a disposable that stops
    /// showing actions when disposed.
    /// </para>
    /// </summary>
    /// <param name="options">Call options</param>
    Task<IAsyncDisposable> ShowActionsAsync(ScreencastShowActionsOptions? options = default);

    /// <summary><para>Shows overlays.</para></summary>
    Task ShowOverlaysAsync();

    /// <summary><para>Removes action decorations.</para></summary>
    Task HideActionsAsync();

    /// <summary><para>Hides overlays without removing them.</para></summary>
    Task HideOverlaysAsync();
}
