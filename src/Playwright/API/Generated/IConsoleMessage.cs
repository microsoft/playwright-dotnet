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
    /// <summary>
    /// <para>
    /// <see cref="IConsoleMessage"/> objects are dispatched by page via the <see cref="IPage.Console"/>
    /// event.
    /// </para>
    /// </summary>
    public partial interface IConsoleMessage
    {
        /// <summary><para>List of arguments passed to a <c>console</c> function call. See also <see cref="IPage.Console"/>.</para></summary>
        IReadOnlyCollection<IJSHandle> Args { get; }

        /// <summary>
        /// <para>
        /// URL of the resource followed by 0-based line and column numbers in the resource
        /// formatted as <c>URL:line:column</c>.
        /// </para>
        /// </summary>
        string Location { get; }

        /// <summary><para>The text of the console message.</para></summary>
        string Text { get; }

        /// <summary>
        /// <para>
        /// One of the following values: <c>'log'</c>, <c>'debug'</c>, <c>'info'</c>, <c>'error'</c>,
        /// <c>'warning'</c>, <c>'dir'</c>, <c>'dirxml'</c>, <c>'table'</c>, <c>'trace'</c>,
        /// <c>'clear'</c>, <c>'startGroup'</c>, <c>'startGroupCollapsed'</c>, <c>'endGroup'</c>,
        /// <c>'assert'</c>, <c>'profile'</c>, <c>'profileEnd'</c>, <c>'count'</c>, <c>'timeEnd'</c>.
        /// </para>
        /// </summary>
        string Type { get; }
    }
}
