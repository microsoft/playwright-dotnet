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

using System.Threading.Tasks;

#nullable enable

namespace Microsoft.Playwright;

/// <summary>
/// <para>
/// The Mouse class operates in main-frame CSS pixels relative to the top-left corner
/// of the viewport.
/// </para>
/// <para>Every <c>page</c> object has its own Mouse, accessible with <see cref="IPage.Mouse"/>.</para>
/// <code>
/// await Page.Mouse.MoveAsync(0, 0);<br/>
/// await Page.Mouse.DownAsync();<br/>
/// await Page.Mouse.MoveAsync(0, 100);<br/>
/// await Page.Mouse.MoveAsync(100, 100);<br/>
/// await Page.Mouse.MoveAsync(100, 0);<br/>
/// await Page.Mouse.MoveAsync(0, 0);<br/>
/// await Page.Mouse.UpAsync();
/// </code>
/// </summary>
public partial interface IMouse
{
    /// <summary>
    /// <para>
    /// Shortcut for <see cref="IMouse.MoveAsync"/>, <see cref="IMouse.DownAsync"/>, <see
    /// cref="IMouse.UpAsync"/>.
    /// </para>
    /// </summary>
    /// <param name="x">
    /// </param>
    /// <param name="y">
    /// </param>
    /// <param name="options">Call options</param>
    Task ClickAsync(float x, float y, MouseClickOptions? options = default);

    /// <summary>
    /// <para>
    /// Shortcut for <see cref="IMouse.MoveAsync"/>, <see cref="IMouse.DownAsync"/>, <see
    /// cref="IMouse.UpAsync"/>, <see cref="IMouse.DownAsync"/> and <see cref="IMouse.UpAsync"/>.
    /// </para>
    /// </summary>
    /// <param name="x">
    /// </param>
    /// <param name="y">
    /// </param>
    /// <param name="options">Call options</param>
    Task DblClickAsync(float x, float y, MouseDblClickOptions? options = default);

    /// <summary><para>Dispatches a <c>mousedown</c> event.</para></summary>
    /// <param name="options">Call options</param>
    Task DownAsync(MouseDownOptions? options = default);

    /// <summary><para>Dispatches a <c>mousemove</c> event.</para></summary>
    /// <param name="x">
    /// </param>
    /// <param name="y">
    /// </param>
    /// <param name="options">Call options</param>
    Task MoveAsync(float x, float y, MouseMoveOptions? options = default);

    /// <summary><para>Dispatches a <c>mouseup</c> event.</para></summary>
    /// <param name="options">Call options</param>
    Task UpAsync(MouseUpOptions? options = default);

    /// <summary><para>Dispatches a <c>wheel</c> event.</para></summary>
    /// <remarks>
    /// <para>
    /// Wheel events may cause scrolling if they are not handled, and this method does not
    /// wait for the scrolling to finish before returning.
    /// </para>
    /// </remarks>
    /// <param name="deltaX">Pixels to scroll horizontally.</param>
    /// <param name="deltaY">Pixels to scroll vertically.</param>
    Task WheelAsync(float deltaX, float deltaY);
}

#nullable disable
