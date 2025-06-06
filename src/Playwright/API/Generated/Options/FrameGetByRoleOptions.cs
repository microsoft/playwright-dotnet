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

namespace Microsoft.Playwright;

public class FrameGetByRoleOptions
{
    public FrameGetByRoleOptions() { }

    public FrameGetByRoleOptions(FrameGetByRoleOptions clone)
    {
        if (clone == null)
        {
            return;
        }

        Checked = clone.Checked;
        Disabled = clone.Disabled;
        Exact = clone.Exact;
        Expanded = clone.Expanded;
        IncludeHidden = clone.IncludeHidden;
        Level = clone.Level;
        Name = clone.Name;
        NameRegex = clone.NameRegex;
        NameString = clone.NameString;
        Pressed = clone.Pressed;
        Selected = clone.Selected;
    }

    /// <summary>
    /// <para>
    /// An attribute that is usually set by <c>aria-checked</c> or native <c>&lt;input type=checkbox&gt;</c>
    /// controls.
    /// </para>
    /// <para>Learn more about <a href="https://www.w3.org/TR/wai-aria-1.2/#aria-checked"><c>aria-checked</c></a>.</para>
    /// </summary>
    [JsonPropertyName("checked")]
    public bool? Checked { get; set; }

    /// <summary>
    /// <para>An attribute that is usually set by <c>aria-disabled</c> or <c>disabled</c>.</para>
    /// <para>
    /// Unlike most other attributes, <c>disabled</c> is inherited through the DOM hierarchy.
    /// Learn more about <a href="https://www.w3.org/TR/wai-aria-1.2/#aria-disabled"><c>aria-disabled</c></a>.
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>
    /// Unlike most other attributes, <c>disabled</c> is inherited through the DOM hierarchy.
    /// Learn more about <a href="https://www.w3.org/TR/wai-aria-1.2/#aria-disabled"><c>aria-disabled</c></a>.
    ///
    /// </para>
    /// </remarks>
    [JsonPropertyName("disabled")]
    public bool? Disabled { get; set; }

    /// <summary>
    /// <para>
    /// Whether <see cref="IFrame.GetByRole"/> is matched exactly: case-sensitive and whole-string.
    /// Defaults to false. Ignored when <see cref="IFrame.GetByRole"/> is a regular expression.
    /// Note that exact match still trims whitespace.
    /// </para>
    /// </summary>
    [JsonPropertyName("exact")]
    public bool? Exact { get; set; }

    /// <summary>
    /// <para>An attribute that is usually set by <c>aria-expanded</c>.</para>
    /// <para>Learn more about <a href="https://www.w3.org/TR/wai-aria-1.2/#aria-expanded"><c>aria-expanded</c></a>.</para>
    /// </summary>
    [JsonPropertyName("expanded")]
    public bool? Expanded { get; set; }

    /// <summary>
    /// <para>
    /// Option that controls whether hidden elements are matched. By default, only non-hidden
    /// elements, as <a href="https://www.w3.org/TR/wai-aria-1.2/#tree_exclusion">defined
    /// by ARIA</a>, are matched by role selector.
    /// </para>
    /// <para>Learn more about <a href="https://www.w3.org/TR/wai-aria-1.2/#aria-hidden"><c>aria-hidden</c></a>.</para>
    /// </summary>
    [JsonPropertyName("includeHidden")]
    public bool? IncludeHidden { get; set; }

    /// <summary>
    /// <para>
    /// A number attribute that is usually present for roles <c>heading</c>, <c>listitem</c>,
    /// <c>row</c>, <c>treeitem</c>, with default values for <c>&lt;h1&gt;-&lt;h6&gt;</c>
    /// elements.
    /// </para>
    /// <para>Learn more about <a href="https://www.w3.org/TR/wai-aria-1.2/#aria-level"><c>aria-level</c></a>.</para>
    /// </summary>
    [JsonPropertyName("level")]
    public int? Level { get; set; }

    /// <summary>
    /// <para>
    /// Option to match the <a href="https://w3c.github.io/accname/#dfn-accessible-name">accessible
    /// name</a>. By default, matching is case-insensitive and searches for a substring,
    /// use <see cref="IFrame.GetByRole"/> to control this behavior.
    /// </para>
    /// <para>
    /// Learn more about <a href="https://w3c.github.io/accname/#dfn-accessible-name">accessible
    /// name</a>.
    /// </para>
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// <para>
    /// Option to match the <a href="https://w3c.github.io/accname/#dfn-accessible-name">accessible
    /// name</a>. By default, matching is case-insensitive and searches for a substring,
    /// use <see cref="IFrame.GetByRole"/> to control this behavior.
    /// </para>
    /// <para>
    /// Learn more about <a href="https://w3c.github.io/accname/#dfn-accessible-name">accessible
    /// name</a>.
    /// </para>
    /// </summary>
    [JsonPropertyName("nameRegex")]
    public Regex? NameRegex { get; set; }

    /// <summary>
    /// <para>
    /// Option to match the <a href="https://w3c.github.io/accname/#dfn-accessible-name">accessible
    /// name</a>. By default, matching is case-insensitive and searches for a substring,
    /// use <see cref="IFrame.GetByRole"/> to control this behavior.
    /// </para>
    /// <para>
    /// Learn more about <a href="https://w3c.github.io/accname/#dfn-accessible-name">accessible
    /// name</a>.
    /// </para>
    /// </summary>
    [JsonPropertyName("nameString")]
    public string? NameString { get; set; }

    /// <summary>
    /// <para>An attribute that is usually set by <c>aria-pressed</c>.</para>
    /// <para>Learn more about <a href="https://www.w3.org/TR/wai-aria-1.2/#aria-pressed"><c>aria-pressed</c></a>.</para>
    /// </summary>
    [JsonPropertyName("pressed")]
    public bool? Pressed { get; set; }

    /// <summary>
    /// <para>An attribute that is usually set by <c>aria-selected</c>.</para>
    /// <para>Learn more about <a href="https://www.w3.org/TR/wai-aria-1.2/#aria-selected"><c>aria-selected</c></a>.</para>
    /// </summary>
    [JsonPropertyName("selected")]
    public bool? Selected { get; set; }
}
