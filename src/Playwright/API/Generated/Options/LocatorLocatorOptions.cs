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
using System.Text.RegularExpressions;

#nullable enable

namespace Microsoft.Playwright;

public class LocatorLocatorOptions
{
    public LocatorLocatorOptions() { }

    public LocatorLocatorOptions(LocatorLocatorOptions clone)
    {
        if (clone == null)
        {
            return;
        }

        Has = clone.Has;
        HasNot = clone.HasNot;
        HasNotText = clone.HasNotText;
        HasNotTextRegex = clone.HasNotTextRegex;
        HasNotTextString = clone.HasNotTextString;
        HasText = clone.HasText;
        HasTextRegex = clone.HasTextRegex;
        HasTextString = clone.HasTextString;
    }

    /// <summary>
    /// <para>
    /// Narrows down the results of the method to those which contain elements matching
    /// this relative locator. For example, <c>article</c> that has <c>text=Playwright</c>
    /// matches <c>&lt;article&gt;&lt;div&gt;Playwright&lt;/div&gt;&lt;/article&gt;</c>.
    /// </para>
    /// <para>
    /// Inner locator **must be relative** to the outer locator and is queried starting
    /// with the outer locator match, not the document root. For example, you can find <c>content</c>
    /// that has <c>div</c> in <c>&lt;article&gt;&lt;content&gt;&lt;div&gt;Playwright&lt;/div&gt;&lt;/content&gt;&lt;/article&gt;</c>.
    /// However, looking for <c>content</c> that has <c>article div</c> will fail, because
    /// the inner locator must be relative and should not use any elements outside the <c>content</c>.
    /// </para>
    /// <para>
    /// Note that outer and inner locators must belong to the same frame. Inner locator
    /// must not contain <see cref="IFrameLocator"/>s.
    /// </para>
    /// </summary>
    [JsonPropertyName("has")]
    public ILocator? Has { get; set; }

    /// <summary>
    /// <para>
    /// Matches elements that do not contain an element that matches an inner locator. Inner
    /// locator is queried against the outer one. For example, <c>article</c> that does
    /// not have <c>div</c> matches <c>&lt;article&gt;&lt;span&gt;Playwright&lt;/span&gt;&lt;/article&gt;</c>.
    /// </para>
    /// <para>
    /// Note that outer and inner locators must belong to the same frame. Inner locator
    /// must not contain <see cref="IFrameLocator"/>s.
    /// </para>
    /// </summary>
    [JsonPropertyName("hasNot")]
    public ILocator? HasNot { get; set; }

    /// <summary>
    /// <para>
    /// Matches elements that do not contain specified text somewhere inside, possibly in
    /// a child or a descendant element. When passed a <see cref="string"/>, matching is
    /// case-insensitive and searches for a substring.
    /// </para>
    /// </summary>
    [JsonPropertyName("hasNotText")]
    public string? HasNotText { get; set; }

    /// <summary>
    /// <para>
    /// Matches elements that do not contain specified text somewhere inside, possibly in
    /// a child or a descendant element. When passed a <see cref="string"/>, matching is
    /// case-insensitive and searches for a substring.
    /// </para>
    /// </summary>
    [JsonPropertyName("hasNotTextRegex")]
    public Regex? HasNotTextRegex { get; set; }

    /// <summary>
    /// <para>
    /// Matches elements that do not contain specified text somewhere inside, possibly in
    /// a child or a descendant element. When passed a <see cref="string"/>, matching is
    /// case-insensitive and searches for a substring.
    /// </para>
    /// </summary>
    [JsonPropertyName("hasNotTextString")]
    public string? HasNotTextString { get; set; }

    /// <summary>
    /// <para>
    /// Matches elements containing specified text somewhere inside, possibly in a child
    /// or a descendant element. When passed a <see cref="string"/>, matching is case-insensitive
    /// and searches for a substring. For example, <c>"Playwright"</c> matches <c>&lt;article&gt;&lt;div&gt;Playwright&lt;/div&gt;&lt;/article&gt;</c>.
    /// </para>
    /// </summary>
    [JsonPropertyName("hasText")]
    public string? HasText { get; set; }

    /// <summary>
    /// <para>
    /// Matches elements containing specified text somewhere inside, possibly in a child
    /// or a descendant element. When passed a <see cref="string"/>, matching is case-insensitive
    /// and searches for a substring. For example, <c>"Playwright"</c> matches <c>&lt;article&gt;&lt;div&gt;Playwright&lt;/div&gt;&lt;/article&gt;</c>.
    /// </para>
    /// </summary>
    [JsonPropertyName("hasTextRegex")]
    public Regex? HasTextRegex { get; set; }

    /// <summary>
    /// <para>
    /// Matches elements containing specified text somewhere inside, possibly in a child
    /// or a descendant element. When passed a <see cref="string"/>, matching is case-insensitive
    /// and searches for a substring. For example, <c>"Playwright"</c> matches <c>&lt;article&gt;&lt;div&gt;Playwright&lt;/div&gt;&lt;/article&gt;</c>.
    /// </para>
    /// </summary>
    [JsonPropertyName("hasTextString")]
    public string? HasTextString { get; set; }
}

#nullable disable
