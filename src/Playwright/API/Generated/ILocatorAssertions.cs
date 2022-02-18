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
    /// <summary>
    /// <para>
    /// The <see cref="ILocatorAssertions"/> class provides assertion methods that can be
    /// used to make assertions about the <see cref="ILocator"/> state in the tests. A new
    /// instance of <see cref="ILocatorAssertions"/> is created by calling <see cref="IPlaywrightAssertions.ExpectLocator"/>:
    /// </para>
    /// </summary>
    public partial interface ILocatorAssertions
    {
        /// <summary>
        /// <para>
        /// Makes the assertion check for the opposite condition. For example, this code tests
        /// that the Locator doesn't contain text <c>"error"</c>:
        /// </para>
        /// </summary>
        ILocatorAssertions Not { get; }

        /// <summary><para>Ensures the <see cref="ILocator"/> points to a checked input.</para></summary>
        /// <param name="options">Call options</param>
        Task ToBeCheckedAsync(LocatorAssertionsToBeCheckedOptions? options = default);

        /// <summary><para>Ensures the <see cref="ILocator"/> points to a disabled element.</para></summary>
        /// <param name="options">Call options</param>
        Task ToBeDisabledAsync(LocatorAssertionsToBeDisabledOptions? options = default);

        /// <summary><para>Ensures the <see cref="ILocator"/> points to an editable element.</para></summary>
        /// <param name="options">Call options</param>
        Task ToBeEditableAsync(LocatorAssertionsToBeEditableOptions? options = default);

        /// <summary>
        /// <para>
        /// Ensures the <see cref="ILocator"/> points to an empty editable element or to a DOM
        /// node that has no text.
        /// </para>
        /// </summary>
        /// <param name="options">Call options</param>
        Task ToBeEmptyAsync(LocatorAssertionsToBeEmptyOptions? options = default);

        /// <summary><para>Ensures the <see cref="ILocator"/> points to an enabled element.</para></summary>
        /// <param name="options">Call options</param>
        Task ToBeEnabledAsync(LocatorAssertionsToBeEnabledOptions? options = default);

        /// <summary><para>Ensures the <see cref="ILocator"/> points to a focused DOM node.</para></summary>
        /// <param name="options">Call options</param>
        Task ToBeFocusedAsync(LocatorAssertionsToBeFocusedOptions? options = default);

        /// <summary>
        /// <para>
        /// Ensures the <see cref="ILocator"/> points to a hidden DOM node, which is the opposite
        /// of <a href="./actionability.md#visible">visible</a>.
        /// </para>
        /// </summary>
        /// <param name="options">Call options</param>
        Task ToBeHiddenAsync(LocatorAssertionsToBeHiddenOptions? options = default);

        /// <summary>
        /// <para>
        /// Ensures the <see cref="ILocator"/> points to a <a href="./actionability.md#visible">visible</a>
        /// DOM node.
        /// </para>
        /// </summary>
        /// <param name="options">Call options</param>
        Task ToBeVisibleAsync(LocatorAssertionsToBeVisibleOptions? options = default);

        /// <summary>
        /// <para>
        /// Ensures the <see cref="ILocator"/> points to an element that contains the given
        /// text. You can use regular expressions for the value as well.
        /// </para>
        /// <para>
        /// Note that if array is passed as an expected value, entire lists of elements can
        /// be asserted:
        /// </para>
        /// </summary>
        /// <param name="expected">Expected substring or RegExp or a list of those.</param>
        /// <param name="options">Call options</param>
        Task ToContainTextAsync(string expected, LocatorAssertionsToContainTextOptions? options = default);

        /// <summary>
        /// <para>
        /// Ensures the <see cref="ILocator"/> points to an element that contains the given
        /// text. You can use regular expressions for the value as well.
        /// </para>
        /// <para>
        /// Note that if array is passed as an expected value, entire lists of elements can
        /// be asserted:
        /// </para>
        /// </summary>
        /// <param name="expected">Expected substring or RegExp or a list of those.</param>
        /// <param name="options">Call options</param>
        Task ToContainTextAsync(Regex expected, LocatorAssertionsToContainTextOptions? options = default);

        /// <summary>
        /// <para>
        /// Ensures the <see cref="ILocator"/> points to an element that contains the given
        /// text. You can use regular expressions for the value as well.
        /// </para>
        /// <para>
        /// Note that if array is passed as an expected value, entire lists of elements can
        /// be asserted:
        /// </para>
        /// </summary>
        /// <param name="expected">Expected substring or RegExp or a list of those.</param>
        /// <param name="options">Call options</param>
        Task ToContainTextAsync(IEnumerable<string> expected, LocatorAssertionsToContainTextOptions? options = default);

        /// <summary>
        /// <para>
        /// Ensures the <see cref="ILocator"/> points to an element that contains the given
        /// text. You can use regular expressions for the value as well.
        /// </para>
        /// <para>
        /// Note that if array is passed as an expected value, entire lists of elements can
        /// be asserted:
        /// </para>
        /// </summary>
        /// <param name="expected">Expected substring or RegExp or a list of those.</param>
        /// <param name="options">Call options</param>
        Task ToContainTextAsync(IEnumerable<Regex> expected, LocatorAssertionsToContainTextOptions? options = default);

        /// <summary><para>Ensures the <see cref="ILocator"/> points to an element with given attribute.</para></summary>
        /// <param name="name">Attribute name.</param>
        /// <param name="value">Expected attribute value.</param>
        /// <param name="options">Call options</param>
        Task ToHaveAttributeAsync(string name, string value, LocatorAssertionsToHaveAttributeOptions? options = default);

        /// <summary><para>Ensures the <see cref="ILocator"/> points to an element with given attribute.</para></summary>
        /// <param name="name">Attribute name.</param>
        /// <param name="value">Expected attribute value.</param>
        /// <param name="options">Call options</param>
        Task ToHaveAttributeAsync(string name, Regex value, LocatorAssertionsToHaveAttributeOptions? options = default);

        /// <summary>
        /// <para>Ensures the <see cref="ILocator"/> points to an element with given CSS class.</para>
        /// <para>
        /// Note that if array is passed as an expected value, entire lists of elements can
        /// be asserted:
        /// </para>
        /// </summary>
        /// <param name="expected">Expected class or RegExp or a list of those.</param>
        /// <param name="options">Call options</param>
        Task ToHaveClassAsync(string expected, LocatorAssertionsToHaveClassOptions? options = default);

        /// <summary>
        /// <para>Ensures the <see cref="ILocator"/> points to an element with given CSS class.</para>
        /// <para>
        /// Note that if array is passed as an expected value, entire lists of elements can
        /// be asserted:
        /// </para>
        /// </summary>
        /// <param name="expected">Expected class or RegExp or a list of those.</param>
        /// <param name="options">Call options</param>
        Task ToHaveClassAsync(Regex expected, LocatorAssertionsToHaveClassOptions? options = default);

        /// <summary>
        /// <para>Ensures the <see cref="ILocator"/> points to an element with given CSS class.</para>
        /// <para>
        /// Note that if array is passed as an expected value, entire lists of elements can
        /// be asserted:
        /// </para>
        /// </summary>
        /// <param name="expected">Expected class or RegExp or a list of those.</param>
        /// <param name="options">Call options</param>
        Task ToHaveClassAsync(IEnumerable<string> expected, LocatorAssertionsToHaveClassOptions? options = default);

        /// <summary>
        /// <para>Ensures the <see cref="ILocator"/> points to an element with given CSS class.</para>
        /// <para>
        /// Note that if array is passed as an expected value, entire lists of elements can
        /// be asserted:
        /// </para>
        /// </summary>
        /// <param name="expected">Expected class or RegExp or a list of those.</param>
        /// <param name="options">Call options</param>
        Task ToHaveClassAsync(IEnumerable<Regex> expected, LocatorAssertionsToHaveClassOptions? options = default);

        /// <summary><para>Ensures the <see cref="ILocator"/> resolves to an exact number of DOM nodes.</para></summary>
        /// <param name="count">Expected count.</param>
        /// <param name="options">Call options</param>
        Task ToHaveCountAsync(int count, LocatorAssertionsToHaveCountOptions? options = default);

        /// <summary>
        /// <para>
        /// Ensures the <see cref="ILocator"/> resolves to an element with the given computed
        /// CSS style.
        /// </para>
        /// </summary>
        /// <param name="name">CSS property name.</param>
        /// <param name="value">CSS property value.</param>
        /// <param name="options">Call options</param>
        Task ToHaveCSSAsync(string name, string value, LocatorAssertionsToHaveCSSOptions? options = default);

        /// <summary>
        /// <para>
        /// Ensures the <see cref="ILocator"/> resolves to an element with the given computed
        /// CSS style.
        /// </para>
        /// </summary>
        /// <param name="name">CSS property name.</param>
        /// <param name="value">CSS property value.</param>
        /// <param name="options">Call options</param>
        Task ToHaveCSSAsync(string name, Regex value, LocatorAssertionsToHaveCSSOptions? options = default);

        /// <summary>
        /// <para>
        /// Ensures the <see cref="ILocator"/> points to an element with the given DOM Node
        /// ID.
        /// </para>
        /// </summary>
        /// <param name="id">Element id.</param>
        /// <param name="options">Call options</param>
        Task ToHaveIdAsync(string id, LocatorAssertionsToHaveIdOptions? options = default);

        /// <summary>
        /// <para>
        /// Ensures the <see cref="ILocator"/> points to an element with the given DOM Node
        /// ID.
        /// </para>
        /// </summary>
        /// <param name="id">Element id.</param>
        /// <param name="options">Call options</param>
        Task ToHaveIdAsync(Regex id, LocatorAssertionsToHaveIdOptions? options = default);

        /// <summary>
        /// <para>
        /// Ensures the <see cref="ILocator"/> points to an element with given JavaScript property.
        /// Note that this property can be of a primitive type as well as a plain serializable
        /// JavaScript object.
        /// </para>
        /// </summary>
        /// <param name="name">Property name.</param>
        /// <param name="value">Property value.</param>
        /// <param name="options">Call options</param>
        Task ToHaveJSPropertyAsync(string name, object value, LocatorAssertionsToHaveJSPropertyOptions? options = default);

        /// <summary>
        /// <para>
        /// Ensures the <see cref="ILocator"/> points to an element with the given text. You
        /// can use regular expressions for the value as well.
        /// </para>
        /// <para>
        /// Note that if array is passed as an expected value, entire lists of elements can
        /// be asserted:
        /// </para>
        /// </summary>
        /// <param name="expected">Expected substring or RegExp or a list of those.</param>
        /// <param name="options">Call options</param>
        Task ToHaveTextAsync(string expected, LocatorAssertionsToHaveTextOptions? options = default);

        /// <summary>
        /// <para>
        /// Ensures the <see cref="ILocator"/> points to an element with the given text. You
        /// can use regular expressions for the value as well.
        /// </para>
        /// <para>
        /// Note that if array is passed as an expected value, entire lists of elements can
        /// be asserted:
        /// </para>
        /// </summary>
        /// <param name="expected">Expected substring or RegExp or a list of those.</param>
        /// <param name="options">Call options</param>
        Task ToHaveTextAsync(Regex expected, LocatorAssertionsToHaveTextOptions? options = default);

        /// <summary>
        /// <para>
        /// Ensures the <see cref="ILocator"/> points to an element with the given text. You
        /// can use regular expressions for the value as well.
        /// </para>
        /// <para>
        /// Note that if array is passed as an expected value, entire lists of elements can
        /// be asserted:
        /// </para>
        /// </summary>
        /// <param name="expected">Expected substring or RegExp or a list of those.</param>
        /// <param name="options">Call options</param>
        Task ToHaveTextAsync(IEnumerable<string> expected, LocatorAssertionsToHaveTextOptions? options = default);

        /// <summary>
        /// <para>
        /// Ensures the <see cref="ILocator"/> points to an element with the given text. You
        /// can use regular expressions for the value as well.
        /// </para>
        /// <para>
        /// Note that if array is passed as an expected value, entire lists of elements can
        /// be asserted:
        /// </para>
        /// </summary>
        /// <param name="expected">Expected substring or RegExp or a list of those.</param>
        /// <param name="options">Call options</param>
        Task ToHaveTextAsync(IEnumerable<Regex> expected, LocatorAssertionsToHaveTextOptions? options = default);

        /// <summary>
        /// <para>
        /// Ensures the <see cref="ILocator"/> points to an element with the given input value.
        /// You can use regular expressions for the value as well.
        /// </para>
        /// </summary>
        /// <param name="value">Expected value.</param>
        /// <param name="options">Call options</param>
        Task ToHaveValueAsync(string value, LocatorAssertionsToHaveValueOptions? options = default);

        /// <summary>
        /// <para>
        /// Ensures the <see cref="ILocator"/> points to an element with the given input value.
        /// You can use regular expressions for the value as well.
        /// </para>
        /// </summary>
        /// <param name="value">Expected value.</param>
        /// <param name="options">Call options</param>
        Task ToHaveValueAsync(Regex value, LocatorAssertionsToHaveValueOptions? options = default);
    }
}

#nullable disable
