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
/// <see cref="IDialog"/> objects are dispatched by page via the <see cref="IPage.Dialog"/>
/// event.
/// </para>
/// <para>An example of using <c>Dialog</c> class:</para>
/// <code>
/// using Microsoft.Playwright;<br/>
/// using System.Threading.Tasks;<br/>
/// <br/>
/// class DialogExample<br/>
/// {<br/>
///     public static async Task Run()<br/>
///     {<br/>
///         using var playwright = await Playwright.CreateAsync();<br/>
///         await using var browser = await playwright.Chromium.LaunchAsync();<br/>
///         var page = await browser.NewPageAsync();<br/>
/// <br/>
///         page.Dialog += async (_, dialog) =&gt;<br/>
///         {<br/>
///             System.Console.WriteLine(dialog.Message);<br/>
///             await dialog.DismissAsync();<br/>
///         };<br/>
/// <br/>
///         await page.EvaluateAsync("alert('1');");<br/>
///     }<br/>
/// }
/// </code>
/// </summary>
/// <remarks>
/// <para>
/// Dialogs are dismissed automatically, unless there is a <see cref="IPage.Dialog"/>
/// listener. When listener is present, it **must** either <see cref="IDialog.AcceptAsync"/>
/// or <see cref="IDialog.DismissAsync"/> the dialog - otherwise the page will <a href="https://developer.mozilla.org/en-US/docs/Web/JavaScript/EventLoop#never_blocking">freeze</a>
/// waiting for the dialog, and actions like click will never finish.
/// </para>
/// </remarks>
public partial interface IDialog
{
    /// <summary><para>Returns when the dialog has been accepted.</para></summary>
    /// <param name="promptText">
    /// A text to enter in prompt. Does not cause any effects if the dialog's <c>type</c>
    /// is not prompt. Optional.
    /// </param>
    Task AcceptAsync(string? promptText = default);

    /// <summary><para>If dialog is prompt, returns default prompt value. Otherwise, returns empty string.</para></summary>
    string DefaultValue { get; }

    /// <summary><para>Returns when the dialog has been dismissed.</para></summary>
    Task DismissAsync();

    /// <summary><para>A message displayed in the dialog.</para></summary>
    string Message { get; }

    /// <summary>
    /// <para>
    /// Returns dialog's type, can be one of <c>alert</c>, <c>beforeunload</c>, <c>confirm</c>
    /// or <c>prompt</c>.
    /// </para>
    /// </summary>
    string Type { get; }
}

#nullable disable
