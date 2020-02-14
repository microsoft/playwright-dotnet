using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Page
{
    ///<playwright-file>emulation.spec.js</playwright-file>
    ///<playwright-describe>Page.emulateMedia type</playwright-describe>
    public class EmulateMediaTypeTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public EmulateMediaTypeTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>emulation.spec.js</playwright-file>
        ///<playwright-describe>Page.emulateMedia type</playwright-describe>
        ///<playwright-it>should work</playwright-it>
        [Fact]
        public async Task ShouldWork()
        {
            Assert.True(await Page.EvaluateAsync<bool>("matchMedia('screen').matches"));
            Assert.False(await Page.EvaluateAsync<bool>("matchMedia('print').matches"));
            await Page.EmulateMediaAsync(new EmulateMedia { Media = MediaType.Print });
            Assert.False(await Page.EvaluateAsync<bool>("matchMedia('screen').matches"));
            Assert.True(await Page.EvaluateAsync<bool>("matchMedia('print').matches"));
            await Page.EmulateMediaAsync(new EmulateMedia());
            Assert.False(await Page.EvaluateAsync<bool>("matchMedia('screen').matches"));
            Assert.True(await Page.EvaluateAsync<bool>("matchMedia('print').matches"));
            await Page.EmulateMediaAsync(new EmulateMedia { Media = MediaType.None });
            Assert.True(await Page.EvaluateAsync<bool>("matchMedia('screen').matches"));
            Assert.False(await Page.EvaluateAsync<bool>("matchMedia('print').matches"));
        }

        ///<playwright-file>emulation.spec.js</playwright-file>
        ///<playwright-describe>Page.emulateMedia type</playwright-describe>
        ///<playwright-it>should throw in case of bad type argument</playwright-it>
        [Fact(Skip = "We don't need this test. Leaving for tracking purposes")]
        public void ShouldThrowInCaseOfBadTypeArgument() { }
    }
}
