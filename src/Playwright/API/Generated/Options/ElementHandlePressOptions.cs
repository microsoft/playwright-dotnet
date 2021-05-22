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
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Playwright
{
    public class ElementHandlePressOptions
    {
        public ElementHandlePressOptions() { }

        public ElementHandlePressOptions(ElementHandlePressOptions clone)
        {
            if (clone == null) return;
            Delay = clone.Delay;
            NoWaitAfter = clone.NoWaitAfter;
            Timeout = clone.Timeout;
        }

        /// <summary>
        /// <para>
        /// Time to wait between <c>keydown</c> and <c>keyup</c> in milliseconds. Defaults to
        /// 0.
        /// </para>
        /// </summary>
        [JsonPropertyName("delay")]
        public float? Delay { get; set; }

        /// <summary>
        /// <para>
        /// Actions that initiate navigations are waiting for these navigations to happen and
        /// for pages to start loading. You can opt out of waiting via setting this flag. You
        /// would only need this option in the exceptional cases such as navigating to inaccessible
        /// pages. Defaults to <c>false</c>.
        /// </para>
        /// </summary>
        [JsonPropertyName("noWaitAfter")]
        public bool? NoWaitAfter { get; set; }

        /// <summary>
        /// <para>
        /// Maximum time in milliseconds, defaults to 30 seconds, pass <c>0</c> to disable timeout.
        /// The default value can be changed by using the <see cref="IBrowserContext.SetDefaultTimeout"/>
        /// or <see cref="IPage.SetDefaultTimeout"/> methods.
        /// </para>
        /// </summary>
        [JsonPropertyName("timeout")]
        public float? Timeout { get; set; }
    }
}
