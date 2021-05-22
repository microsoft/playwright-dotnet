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
using System.Linq;

namespace Microsoft.Playwright
{
    /// <summary>
    /// Result of calling <see cref="IAccessibility.SnapshotAsync" />.
    /// </summary>
    public partial class AccessibilitySnapshotResult : IEquatable<AccessibilitySnapshotResult>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AccessibilitySnapshotResult"/> class.
        /// </summary>
        public AccessibilitySnapshotResult()
        {
            Children = Array.Empty<AccessibilitySnapshotResult>();
        }

        /// <inheritdoc/>
        public bool Equals(AccessibilitySnapshotResult other)
            => other != null &&
            (ReferenceEquals(this, other) || (
            Role == other.Role &&
            Name == other.Name &&
            Value == other.Value &&
            Description == other.Description &&
            Keyshortcuts == other.Keyshortcuts &&
            Roledescription == other.Roledescription &&
            Valuetext == other.Valuetext &&
            Disabled == other.Disabled &&
            Expanded == other.Expanded &&
            Focused == other.Focused &&
            Modal == other.Modal &&
            Multiline == other.Multiline &&
            Multiselectable == other.Multiselectable &&
            Readonly == other.Readonly &&
            Required == other.Required &&
            Selected == other.Selected &&
            Checked == other.Checked &&
            Pressed == other.Pressed &&
            Level == other.Level &&
            Valuemin == other.Valuemin &&
            Valuemax == other.Valuemax &&
            Autocomplete == other.Autocomplete &&
            Haspopup == other.Haspopup &&
            Invalid == other.Invalid &&
            Orientation == other.Orientation &&
            (Children == other.Children || Children.SequenceEqual(other.Children))));

        /// <inheritdoc/>
        public override bool Equals(object obj) => obj is AccessibilitySnapshotResult && base.Equals(obj);

        /// <inheritdoc/>
        public override int GetHashCode()
            => (Role ?? string.Empty).GetHashCode() ^
                (Name ?? string.Empty).GetHashCode() ^
                (Value ?? string.Empty).GetHashCode() ^
                (Description ?? string.Empty).GetHashCode() ^
                (Keyshortcuts ?? string.Empty).GetHashCode() ^
                (Roledescription ?? string.Empty).GetHashCode() ^
                (Valuetext ?? string.Empty).GetHashCode() ^
                Disabled.GetHashCode() ^
                Expanded.GetHashCode() ^
                Focused.GetHashCode() ^
                Modal.GetHashCode() ^
                Multiline.GetHashCode() ^
                Multiselectable.GetHashCode() ^
                Readonly.GetHashCode() ^
                Required.GetHashCode() ^
                Selected.GetHashCode() ^
                Checked.GetHashCode() ^
                Pressed.GetHashCode() ^
                Level.GetHashCode() ^
                Valuemin.GetHashCode() ^
                Valuemax.GetHashCode() ^
                (Autocomplete ?? string.Empty).GetHashCode() ^
                (Haspopup ?? string.Empty).GetHashCode() ^
                (Invalid ?? string.Empty).GetHashCode() ^
                (Orientation ?? string.Empty).GetHashCode() ^
                Children.GetHashCode();
    }
}
