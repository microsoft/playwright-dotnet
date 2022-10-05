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

using System.IO;
using System.Threading.Tasks;

#nullable enable

namespace Microsoft.Playwright;

/// <summary>
/// <para>
/// <see cref="IDownload"/> objects are dispatched by page via the <see cref="IPage.Download"/>
/// event.
/// </para>
/// <para>
/// All the downloaded files belonging to the browser context are deleted when the browser
/// context is closed.
/// </para>
/// <para>
/// Download event is emitted once the download starts. Download path becomes available
/// once download completes:
/// </para>
/// <code>
/// var download = await page.RunAndWaitForDownloadAsync(async () =&gt;<br/>
/// {<br/>
///     await page.GetByText("Download file").ClickAsync();<br/>
/// });<br/>
/// Console.WriteLine(await download.PathAsync());
/// </code>
/// </summary>
public partial interface IDownload
{
    /// <summary>
    /// <para>
    /// Cancels a download. Will not fail if the download is already finished or canceled.
    /// Upon successful cancellations, <c>download.failure()</c> would resolve to <c>'canceled'</c>.
    /// </para>
    /// </summary>
    Task CancelAsync();

    /// <summary><para>Returns readable stream for current download or <c>null</c> if download failed.</para></summary>
    Task<Stream?> CreateReadStreamAsync();

    /// <summary><para>Deletes the downloaded file. Will wait for the download to finish if necessary.</para></summary>
    Task DeleteAsync();

    /// <summary><para>Returns download error if any. Will wait for the download to finish if necessary.</para></summary>
    Task<string?> FailureAsync();

    /// <summary><para>Get the page that the download belongs to.</para></summary>
    IPage Page { get; }

    /// <summary>
    /// <para>
    /// Returns path to the downloaded file in case of successful download. The method will
    /// wait for the download to finish if necessary. The method throws when connected remotely.
    /// </para>
    /// <para>
    /// Note that the download's file name is a random GUID, use <see cref="IDownload.SuggestedFilename"/>
    /// to get suggested file name.
    /// </para>
    /// </summary>
    Task<string?> PathAsync();

    /// <summary>
    /// <para>
    /// Copy the download to a user-specified path. It is safe to call this method while
    /// the download is still in progress. Will wait for the download to finish if necessary.
    /// </para>
    /// </summary>
    /// <param name="path">Path where the download should be copied.</param>
    Task SaveAsAsync(string path);

    /// <summary>
    /// <para>
    /// Returns suggested filename for this download. It is typically computed by the browser
    /// from the <a href="https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Content-Disposition"><c>Content-Disposition</c></a>
    /// response header or the <c>download</c> attribute. See the spec on <a href="https://html.spec.whatwg.org/#downloading-resources">whatwg</a>.
    /// Different browsers can use different logic for computing it.
    /// </para>
    /// </summary>
    string SuggestedFilename { get; }

    /// <summary><para>Returns downloaded url.</para></summary>
    string Url { get; }
}

#nullable disable
