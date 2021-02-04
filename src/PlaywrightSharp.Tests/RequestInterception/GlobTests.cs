using PlaywrightSharp.Helpers;
using PlaywrightSharp.Tests;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.RequestInterception
{
    ///<playwright-file>interception.spec.js</playwright-file>
    ///<playwright-describe>glob</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class GlobTests : PlaywrightSharpBaseTest
    {
        /// <inheritdoc/>
        public GlobTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("interception.spec.js", "glob", "should work with glob")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public void ShouldWorkWithGlob()
        {
            Assert.Matches(StringExtensions.GlobToRegex("**/*.js"), "https://localhost:8080/foo.js");
            Assert.DoesNotMatch(StringExtensions.GlobToRegex("**/*.css"), "https://localhost:8080/foo.js");
            Assert.DoesNotMatch(StringExtensions.GlobToRegex("*.js"), "https://localhost:8080/foo.js");
            Assert.Matches(StringExtensions.GlobToRegex("https://**/*.js"), "https://localhost:8080/foo.js");
            Assert.Matches(StringExtensions.GlobToRegex("http://localhost:8080/simple/path.js"), "http://localhost:8080/simple/path.js");
            Assert.Matches(StringExtensions.GlobToRegex("http://localhost:8080/?imple/path.js"), "http://localhost:8080/Simple/path.js");
            Assert.Matches(StringExtensions.GlobToRegex("**/{a,b}.js"), "https://localhost:8080/a.js");
            Assert.Matches(StringExtensions.GlobToRegex("**/{a,b}.js"), "https://localhost:8080/b.js");
            Assert.DoesNotMatch(StringExtensions.GlobToRegex("**/{a,b}.js"), "https://localhost:8080/c.js");

            Assert.Matches(StringExtensions.GlobToRegex("**/*.{png,jpg,jpeg}"), "https://localhost:8080/c.jpg");
            Assert.Matches(StringExtensions.GlobToRegex("**/*.{png,jpg,jpeg}"), "https://localhost:8080/c.jpeg");
            Assert.Matches(StringExtensions.GlobToRegex("**/*.{png,jpg,jpeg}"), "https://localhost:8080/c.png");
            Assert.DoesNotMatch(StringExtensions.GlobToRegex("**/*.{png,jpg,jpeg}"), "https://localhost:8080/c.css");
        }
    }
}
