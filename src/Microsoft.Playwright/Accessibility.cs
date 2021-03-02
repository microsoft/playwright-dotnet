using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Playwright
{
    /// <inheritdoc/>
    public class Accessibility : IAccessibility
    {
        /// <inheritdoc/>
        public Task<string> SnapshotAsync(bool? interestingOnly = null, IElementHandle root = null) => throw new NotImplementedException();
    }
}
