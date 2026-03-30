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
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Microsoft.Playwright;

public class ScreencastStartOptions
{
    public ScreencastStartOptions() { }

    public ScreencastStartOptions(ScreencastStartOptions clone)
    {
        if (clone == null)
        {
            return;
        }

        OnFrame = clone.OnFrame;
        Path = clone.Path;
        Quality = clone.Quality;
    }

    /// <summary><para>Callback that receives JPEG-encoded frame data.</para></summary>
    [JsonPropertyName("onFrame")]
    public Func<ScreencastFrame, Task>? OnFrame { get; set; }

    /// <summary>
    /// <para>
    /// Path where the video should be saved when the screencast is stopped. When provided,
    /// video recording is started.
    /// </para>
    /// </summary>
    [JsonPropertyName("path")]
    public string? Path { get; set; }

    /// <summary><para>The quality of the image, between 0-100.</para></summary>
    [JsonPropertyName("quality")]
    public int? Quality { get; set; }
}
