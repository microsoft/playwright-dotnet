using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Playwright.Testing.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Playwright.Tests
{
    public class SkipAttribute : SkipBaseAttribute
    {
        public SkipAttribute(params TestTargets[] combinations) : base(combinations)
        {
        }

        public bool ShouldSkipTest() => base.ShouldSkip();
    }
}
