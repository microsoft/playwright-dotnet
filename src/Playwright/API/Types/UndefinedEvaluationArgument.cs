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
using System.Text;

namespace Microsoft.Playwright.Contracts.Models
{
    /// <summary>
    /// Represents an `Undefined` argument.
    /// </summary>
    public sealed class UndefinedEvaluationArgument : IEquatable<UndefinedEvaluationArgument>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UndefinedEvaluationArgument"/> class.
        /// </summary>
        private UndefinedEvaluationArgument()
        {
            // NOOP
        }

        /// <summary>
        /// Gets a representation of the <c>Undefined</c> argument.
        /// </summary>
        public static UndefinedEvaluationArgument Undefined { get; } = new UndefinedEvaluationArgument();

        /// <inheritdoc/>
        public bool Equals(UndefinedEvaluationArgument other) => ReferenceEquals(this, other);

        /// <inheritdoc/>
        public override bool Equals(object obj) => Equals(obj as UndefinedEvaluationArgument);

        /// <inheritdoc/>
        public override int GetHashCode() => base.GetHashCode();
    }
}
