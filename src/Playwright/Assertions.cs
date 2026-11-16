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
using System.Threading.Tasks;
using Microsoft.Playwright.Core;

namespace Microsoft.Playwright;

public static class Assertions
{
    /// <summary>
    /// Sets the default timeout for all future <c>Expect</c> calls.
    /// </summary>
    /// <param name="timeout">The timeout in milliseconds.</param>
    /// <remarks>
    /// <para>
    /// The default timeout is 5 seconds.
    /// </para>
    /// </remarks>
    public static void SetDefaultExpectTimeout(float timeout) => AssertionsBase.SetDefaultTimeout(timeout);

    /// <summary>
    /// Sets the default timeout for all future <see cref="IFunctionAssertions.ToPassAsync"/> calls.
    /// </summary>
    /// <param name="timeout">The timeout in milliseconds.</param>
    /// <remarks>
    /// <para>
    /// The default timeout is 30 seconds. <see cref="SetDefaultExpectTimeout"/> does not change it.
    /// </para>
    /// </remarks>
    public static void SetDefaultExpectToPassTimeout(float timeout) => FunctionAssertions.SetDefaultTimeout(timeout);

    public static ILocatorAssertions Expect(ILocator locator) => new LocatorAssertions(locator, false);

    public static IPageAssertions Expect(IPage page) => new PageAssertions(page, false);

    public static IAPIResponseAssertions Expect(IAPIResponse response) => new APIResponseAssertions(response, false);

    public static IFunctionAssertions Expect(Func<Task> callback) => new FunctionAssertions(callback);

    /// <summary>
    /// Creates assertions that retry a synchronous <paramref name="callback"/>.
    /// </summary>
    /// <param name="callback">Callback to retry.</param>
    /// <returns>Assertions for the given callback.</returns>
    public static IFunctionAssertions Expect(Action callback) => new FunctionAssertions(callback);

    /// <summary>
    /// Creates assertions that prefix any failure message with <paramref name="message"/>, providing
    /// extra context in test reports.
    /// </summary>
    /// <param name="locator">The locator to assert against.</param>
    /// <param name="message">Message to prepend to any assertion failure.</param>
    /// <returns>Assertions for the given locator.</returns>
    public static ILocatorAssertions Expect(ILocator locator, string message) => new LocatorAssertions(locator, false, message);

    /// <inheritdoc cref="Expect(ILocator, string)" />
    /// <param name="page">The page to assert against.</param>
    /// <param name="message">Message to prepend to any assertion failure.</param>
    public static IPageAssertions Expect(IPage page, string message) => new PageAssertions(page, false, message);

    /// <inheritdoc cref="Expect(ILocator, string)" />
    /// <param name="response">The API response to assert against.</param>
    /// <param name="message">Message to prepend to any assertion failure.</param>
    public static IAPIResponseAssertions Expect(IAPIResponse response, string message) => new APIResponseAssertions(response, false, message);

    /// <inheritdoc cref="Expect(ILocator, string)" />
    /// <param name="callback">Callback to retry.</param>
    /// <param name="message">Message to prepend to any assertion failure.</param>
    public static IFunctionAssertions Expect(Func<Task> callback, string message) => new FunctionAssertions(callback, message);

    /// <inheritdoc cref="Expect(ILocator, string)" />
    /// <param name="callback">Callback to retry.</param>
    /// <param name="message">Message to prepend to any assertion failure.</param>
    public static IFunctionAssertions Expect(Action callback, string message) => new FunctionAssertions(callback, message);
}
