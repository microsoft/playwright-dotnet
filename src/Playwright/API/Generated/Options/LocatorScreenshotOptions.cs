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
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace Microsoft.Playwright
{
    public class LocatorScreenshotOptions
    {
        public LocatorScreenshotOptions() { }

        public LocatorScreenshotOptions(LocatorScreenshotOptions clone)
        {
            if (clone == null)
            {
                return;
            }

            OmitBackground = clone.OmitBackground;
            Path = clone.Path;
            Quality = clone.Quality;
            Timeout = clone.Timeout;
            Type = clone.Type;
        }

        /// <summary>
        /// <para>
        /// Hides default white background and allows capturing screenshots with transparency.
        /// Not applicable to <c>jpeg</c> images. Defaults to <c>false</c>.
        /// </para>
        /// </summary>
        [JsonPropertyName("omitBackground")]
        public bool? OmitBackground { get; set; }

        /// <summary>
        /// <para>
        /// The file path to save the image to. The screenshot type will be inferred from file
        /// extension. If <paramref name="path"/> is a relative path, then it is resolved relative
        /// to the current working directory. If no path is provided, the image won't be saved
        /// to the disk.
        /// </para>
        /// </summary>
        [JsonPropertyName("path")]
        public string? Path { get; set; }

        /// <summary><para>The quality of the image, between 0-100. Not applicable to <c>png</c> images.</para></summary>
        [JsonPropertyName("quality")]
        public int? Quality { get; set; }

        /// <summary>
        /// <para>
        /// Maximum time in milliseconds, defaults to 30 seconds, pass <c>0</c> to disable timeout.
        /// The default value can be changed by using the <see cref="IBrowserContext.SetDefaultTimeout"/>
        /// or <see cref="IPage.SetDefaultTimeout"/> methods.
        /// </para>
        /// </summary>
        [JsonPropertyName("timeout")]
        public float? Timeout { get; set; }

        /// <summary><para>Specify screenshot type, defaults to <c>png</c>.</para></summary>
        [JsonPropertyName("type")]
        public ScreenshotType? Type { get; set; }
    }
}

#nullable disable
