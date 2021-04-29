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
