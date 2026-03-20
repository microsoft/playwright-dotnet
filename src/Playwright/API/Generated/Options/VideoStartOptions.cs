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

public class VideoStartOptions
{
    public VideoStartOptions() { }

    public VideoStartOptions(VideoStartOptions clone)
    {
        if (clone == null)
        {
            return;
        }

        Annotate = clone.Annotate;
        Path = clone.Path;
        Size = clone.Size;
    }

    /// <summary>
    /// <para>
    /// If specified, enables visual annotations on interacted elements during video recording.
    /// Interacted elements are highlighted with a semi-transparent blue box and click points
    /// are shown as red circles.
    /// </para>
    /// </summary>
    [JsonPropertyName("annotate")]
    public Annotate? Annotate { get; set; }

    /// <summary><para>Path where the video should be saved when the recording is stopped.</para></summary>
    [JsonPropertyName("path")]
    public string? Path { get; set; }

    /// <summary>
    /// <para>
    /// Optional dimensions of the recorded video. If not specified the size will be equal
    /// to page viewport scaled down to fit into 800x800. Actual picture of the page will
    /// be scaled down if necessary to fit the specified size.
    /// </para>
    /// </summary>
    [JsonPropertyName("size")]
    public RequestSizesResult? Size { get; set; }
}
