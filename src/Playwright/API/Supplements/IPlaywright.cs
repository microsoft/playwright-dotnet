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

namespace Microsoft.Playwright;

/// <summary>
/// The main entry point for Playwright.
/// </summary>
public partial interface IPlaywright : IDisposable
{
    /// <summary>
    /// Gets a <see cref="IBrowserType"/>.
    /// </summary>
    /// <param name="browserType"><see cref="IBrowserType"/> name. You can get the names from <see cref="BrowserType"/>.
    /// e.g.: <see cref="BrowserType.Chromium"/>, <see cref="BrowserType.Firefox"/> or <see cref="BrowserType.Webkit"/>.
    /// </param>
    IBrowserType this[string browserType] { get; }
}
