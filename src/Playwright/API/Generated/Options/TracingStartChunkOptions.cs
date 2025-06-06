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

using System.Text.Json.Serialization;

namespace Microsoft.Playwright;

public class TracingStartChunkOptions
{
    public TracingStartChunkOptions() { }

    public TracingStartChunkOptions(TracingStartChunkOptions clone)
    {
        if (clone == null)
        {
            return;
        }

        Name = clone.Name;
        Title = clone.Title;
    }

    /// <summary>
    /// <para>
    /// If specified, intermediate trace files are going to be saved into the files with
    /// the given name prefix inside the <see cref="IBrowserType.LaunchAsync"/> directory
    /// specified in <see cref="IBrowserType.LaunchAsync"/>. To specify the final trace
    /// zip file name, you need to pass <c>path</c> option to <see cref="ITracing.StopChunkAsync"/>
    /// instead.
    /// </para>
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary><para>Trace name to be shown in the Trace Viewer.</para></summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }
}
