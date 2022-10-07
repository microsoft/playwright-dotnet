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

#nullable enable

namespace Microsoft.Playwright;

/// <summary>
/// <para>
/// <see cref="IFileChooser"/> objects are dispatched by the page in the <see cref="IPage.FileChooser"/>
/// event.
/// </para>
/// <code>
/// var fileChooser = await page.RunAndWaitForFileChooserAsync(async () =&gt;<br/>
/// {<br/>
///     await page.GetByText("Upload").ClickAsync();<br/>
/// });<br/>
/// await fileChooser.SetFilesAsync("temp.txt");
/// </code>
/// </summary>
public partial interface IFileChooser
{
    /// <summary><para>Returns input element associated with this file chooser.</para></summary>
    IElementHandle Element { get; }

    /// <summary><para>Returns whether this file chooser accepts multiple files.</para></summary>
    bool IsMultiple { get; }

    /// <summary><para>Returns page this file chooser belongs to.</para></summary>
    IPage Page { get; }

    /// <summary>
    /// <para>
    /// Sets the value of the file input this chooser is associated with. If some of the
    /// <c>filePaths</c> are relative paths, then they are resolved relative to the current
    /// working directory. For empty array, clears the selected files.
    /// </para>
    /// </summary>
    /// <param name="files">
    /// </param>
    /// <param name="options">Call options</param>
    Task SetFilesAsync(string files, FileChooserSetFilesOptions? options = default);

    /// <summary>
    /// <para>
    /// Sets the value of the file input this chooser is associated with. If some of the
    /// <c>filePaths</c> are relative paths, then they are resolved relative to the current
    /// working directory. For empty array, clears the selected files.
    /// </para>
    /// </summary>
    /// <param name="files">
    /// </param>
    /// <param name="options">Call options</param>
    Task SetFilesAsync(IEnumerable<string> files, FileChooserSetFilesOptions? options = default);

    /// <summary>
    /// <para>
    /// Sets the value of the file input this chooser is associated with. If some of the
    /// <c>filePaths</c> are relative paths, then they are resolved relative to the current
    /// working directory. For empty array, clears the selected files.
    /// </para>
    /// </summary>
    /// <param name="files">
    /// </param>
    /// <param name="options">Call options</param>
    Task SetFilesAsync(FilePayload files, FileChooserSetFilesOptions? options = default);

    /// <summary>
    /// <para>
    /// Sets the value of the file input this chooser is associated with. If some of the
    /// <c>filePaths</c> are relative paths, then they are resolved relative to the current
    /// working directory. For empty array, clears the selected files.
    /// </para>
    /// </summary>
    /// <param name="files">
    /// </param>
    /// <param name="options">Call options</param>
    Task SetFilesAsync(IEnumerable<FilePayload> files, FileChooserSetFilesOptions? options = default);
}

#nullable disable
