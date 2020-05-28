using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Accessibility
{
    ///<playwright-file>accessibility.spec.js</playwright-file>
    ///<playwright-describe>root option</playwright-describe>
    [Trait("Category", "chromium")]
    [Trait("Category", "firefox")]
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class RootOptionTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public RootOptionTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>accessibility.spec.js</playwright-file>
        ///<playwright-describe>root option</playwright-describe>
        ///<playwright-it>should work a button</playwright-it>
        [Retry]
        public async Task ShouldWorkAButton()
        {
            await Page.SetContentAsync("<button>My Button</button>");
            var button = await Page.QuerySelectorAsync("button");

            Assert.Equal(
                new SerializedAXNode
                {
                    Role = "button",
                    Name = "My Button"
                },
                await Page.Accessibility.SnapshotAsync(new AccessibilitySnapshotOptions { Root = button }));
        }

        ///<playwright-file>accessibility.spec.js</playwright-file>
        ///<playwright-describe>root option</playwright-describe>
        ///<playwright-it>should work an input</playwright-it>
        [Retry]
        public async Task ShouldWorkAnInput()
        {
            await Page.SetContentAsync("<input title=\"My Input\" value=\"My Value\">");
            var input = await Page.QuerySelectorAsync("input");

            Assert.Equal(
                new SerializedAXNode
                {
                    Role = "textbox",
                    Name = "My Input",
                    Value = "My Value"
                },
                await Page.Accessibility.SnapshotAsync(new AccessibilitySnapshotOptions { Root = input }));
        }

        ///<playwright-file>accessibility.spec.js</playwright-file>
        ///<playwright-describe>root option</playwright-describe>
        ///<playwright-it>should work on a menu</playwright-it>
        [Retry]
        public async Task ShouldWorkOnAMenu()
        {
            await Page.SetContentAsync(@"
            <div role=""menu"" title=""My Menu"">
              <div role=""menuitem"">First Item</div>
              <div role=""menuitem"">Second Item</div>
              <div role=""menuitem"">Third Item</div>
            </div>
            ");
            var menu = await Page.QuerySelectorAsync("div[role=\"menu\"]");

            Assert.Equal(
                new SerializedAXNode
                {
                    Role = "menu",
                    Name = "My Menu",
                    Children = new SerializedAXNode[]
                    {
                        new SerializedAXNode
                        {
                            Role = "menuitem",
                            Name = "First Item"
                        },
                        new SerializedAXNode
                        {
                            Role = "menuitem",
                            Name = "Second Item"
                        },
                        new SerializedAXNode
                        {
                            Role = "menuitem",
                            Name = "Third Item"
                        }
                    },
                    Orientation = TestConstants.IsWebKit ? "vertical" : null
                },
                await Page.Accessibility.SnapshotAsync(new AccessibilitySnapshotOptions { Root = menu }));
        }

        ///<playwright-file>accessibility.spec.js</playwright-file>
        ///<playwright-describe>root option</playwright-describe>
        ///<playwright-it>should return null when the element is no longer in DOM</playwright-it>
        [Retry]
        public async Task ShouldReturnNullWhenTheElementIsNoLongerInDOM()
        {
            await Page.SetContentAsync("<button>My Button</button>");
            var button = await Page.QuerySelectorAsync("button");
            await Page.QuerySelectorEvaluateAsync("button", "button => button.remove()");

            Assert.Null(await Page.Accessibility.SnapshotAsync(new AccessibilitySnapshotOptions { Root = button }));
        }

        ///<playwright-file>accessibility.spec.js</playwright-file>
        ///<playwright-describe>root option</playwright-describe>
        ///<playwright-it>should show uninteresting nodes</playwright-it>
        [Retry]
        public async Task ShouldReportUninterestingNodes()
        {
            await Page.SetContentAsync(@"
            <div id=""root"" role=""textbox"">
              <div>
                hello
                <div>
                  world
                </div>
              </div>
            </div>
            ");

            var root = await Page.QuerySelectorAsync("#root");
            var snapshot = await Page.Accessibility.SnapshotAsync(new AccessibilitySnapshotOptions
            {
                Root = root,
                InterestingOnly = false
            });
            Assert.Equal("textbox", snapshot.Role);
            Assert.Contains("hello", snapshot.Value);
            Assert.Contains("world", snapshot.Value);
            Assert.NotEmpty(snapshot.Children);
        }
    }
}
