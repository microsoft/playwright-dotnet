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

namespace Microsoft.Playwright;

/// <summary>
/// <para>
/// The Touchscreen class operates in main-frame CSS pixels relative to the top-left
/// corner of the viewport. Methods on the touchscreen can only be used in browser contexts
/// that have been initialized with <c>hasTouch</c> set to true.
/// </para>
/// <para>
/// This class is limited to emulating tap gestures. For examples of other gestures
/// simulated by manually dispatching touch events, see the <a href="https://playwright.dev/dotnet/docs/touch-events">emulating
/// legacy touch events</a> page.
/// </para>
/// </summary>
public partial interface ITouchscreen
{
    /// <summary>
    /// <para>
    /// Dispatches a <c>touchstart</c> and <c>touchend</c> event with a single touch at
    /// the position (<see cref="ITouchscreen.TapAsync"/>,<see cref="ITouchscreen.TapAsync"/>).
    /// </para>
    /// <para>
    /// <see cref="IPage.TapAsync"/> the method will throw if <see cref="IBrowser.NewContextAsync"/>
    /// option of the browser context is false.
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="IPage.TapAsync"/> the method will throw if <see cref="IBrowser.NewContextAsync"/>
    /// option of the browser context is false.
    /// </para>
    /// </remarks>
    /// <param name="x">X coordinate relative to the main frame's viewport in CSS pixels.</param>
    /// <param name="y">Y coordinate relative to the main frame's viewport in CSS pixels.</param>
    Task TapAsync(float x, float y);
}
