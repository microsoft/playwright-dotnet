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


#nullable enable

namespace Microsoft.Playwright;

/// <summary><para>The <see cref="IFormData"/> is used create form data that is sent via <see cref="IAPIRequestContext"/>.</para></summary>
public partial interface IFormData
{
    /// <summary>
    /// <para>
    /// Sets a field on the form. File values can be passed either as <c>Path</c> or as
    /// <c>FilePayload</c>.
    /// </para>
    /// </summary>
    /// <param name="name">Field name.</param>
    /// <param name="value">Field value.</param>
    IFormData Set(string name, string value);

    /// <summary>
    /// <para>
    /// Sets a field on the form. File values can be passed either as <c>Path</c> or as
    /// <c>FilePayload</c>.
    /// </para>
    /// </summary>
    /// <param name="name">Field name.</param>
    /// <param name="value">Field value.</param>
    IFormData Set(string name, bool value);

    /// <summary>
    /// <para>
    /// Sets a field on the form. File values can be passed either as <c>Path</c> or as
    /// <c>FilePayload</c>.
    /// </para>
    /// </summary>
    /// <param name="name">Field name.</param>
    /// <param name="value">Field value.</param>
    IFormData Set(string name, int value);

    /// <summary>
    /// <para>
    /// Sets a field on the form. File values can be passed either as <c>Path</c> or as
    /// <c>FilePayload</c>.
    /// </para>
    /// </summary>
    /// <param name="name">Field name.</param>
    /// <param name="value">Field value.</param>
    IFormData Set(string name, FilePayload value);
}

#nullable disable
