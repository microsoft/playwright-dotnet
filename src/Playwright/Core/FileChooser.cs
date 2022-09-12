/*
 * MIT License
 *
 * Copyright (c) Microsoft Corporation.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
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

namespace Microsoft.Playwright.Core;

/// <summary>
/// see <see cref="IPage.FileChooser"/> arguments.
/// </summary>
internal partial class FileChooser : IFileChooser
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FileChooser"/> class.
    /// </summary>
    /// <param name="page">The page this file chooser belongs to.</param>
    /// <param name="element">The input element.</param>
    /// <param name="multiple">The multiple option.</param>
    public FileChooser(IPage page, ElementHandle element, bool multiple)
    {
        Page = page;
        Element = element;
        ElementImpl = element;
        IsMultiple = multiple;
    }

    public IPage Page { get; set; }

    public IElementHandle Element { get; set; }

    public ElementHandle ElementImpl { get; set; }

    public bool IsMultiple { get; set; }

    public Task SetFilesAsync(string files, FileChooserSetFilesOptions options = default)
        => ElementImpl.SetInputFilesAsync(files, Map(options));

    public Task SetFilesAsync(IEnumerable<string> files, FileChooserSetFilesOptions options = default)
        => ElementImpl.SetInputFilesAsync(files, Map(options));

    public Task SetFilesAsync(FilePayload files, FileChooserSetFilesOptions options = default)
        => ElementImpl.SetInputFilesAsync(files, Map(options));

    public Task SetFilesAsync(IEnumerable<FilePayload> files, FileChooserSetFilesOptions options = default)
        => ElementImpl.SetInputFilesAsync(files, Map(options));

    private ElementHandleSetInputFilesOptions Map(FileChooserSetFilesOptions options)
    {
        if (options == null)
        {
            return null;
        }

        return new()
        {
            NoWaitAfter = options?.NoWaitAfter,
            Timeout = options?.Timeout,
        };
    }
}
