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
/// When browser context is created with the <c>recordVideo</c> option, each page has
/// a video object associated with it.
/// </para>
/// <code>Console.WriteLine(await page.Video.GetPathAsync());</code>
/// </summary>
public partial interface IVideo
{
    /// <summary><para>Deletes the video file. Will wait for the video to finish if necessary.</para></summary>
    Task DeleteAsync();

    /// <summary>
    /// <para>
    /// Returns the file system path this video will be recorded to. The video is guaranteed
    /// to be written to the filesystem upon closing the browser context. This method throws
    /// when connected remotely.
    /// </para>
    /// </summary>
    Task<string> PathAsync();

    /// <summary>
    /// <para>
    /// Saves the video to a user-specified path. It is safe to call this method while the
    /// video is still in progress, or after the page has closed. This method waits until
    /// the page is closed and the video is fully saved.
    /// </para>
    /// </summary>
    /// <param name="path">Path where the video should be saved.</param>
    Task SaveAsAsync(string path);
}

#nullable disable
