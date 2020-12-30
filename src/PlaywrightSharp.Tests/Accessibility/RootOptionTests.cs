using System.Threading.Tasks;
using KellermanSoftware.CompareNetObjects;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Accessibility
{
    ///<playwright-file>accessibility.spec.js</playwright-file>
    ///<playwright-describe>root option</playwright-describe>
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
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
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
                await Page.Accessibility.SnapshotAsync(root: button));
        }

        ///<playwright-file>accessibility.spec.js</playwright-file>
        ///<playwright-describe>root option</playwright-describe>
        ///<playwright-it>should work an input</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkAnInput()
        {
            await Page.SetContentAsync("<input title=\"My Input\" value=\"My Value\">");
            var input = await Page.QuerySelectorAsync("input");

            Assert.Equal(
                new SerializedAXNode
                {
                    Role = "textbox",
                    Name = "My Input",
                    ValueString = "My Value"
                },
                await Page.Accessibility.SnapshotAsync(root: input));
        }

        ///<playwright-file>accessibility.spec.js</playwright-file>
        ///<playwright-describe>root option</playwright-describe>
        ///<playwright-it>should work on a menu</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
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

            var nodeToCheck = new SerializedAXNode
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
            };

            CompareLogic compareLogic = new CompareLogic();
            var result = compareLogic.Compare(nodeToCheck, await Page.Accessibility.SnapshotAsync(root: menu));
            Assert.True(result.AreEqual, result.DifferencesString);
        }

        ///<playwright-file>accessibility.spec.js</playwright-file>
        ///<playwright-describe>root option</playwright-describe>
        ///<playwright-it>should return null when the element is no longer in DOM</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldReturnNullWhenTheElementIsNoLongerInDOM()
        {
            await Page.SetContentAsync("<button>My Button</button>");
            var button = await Page.QuerySelectorAsync("button");
            await Page.EvalOnSelectorAsync("button", "button => button.remove()");

            Assert.Null(await Page.Accessibility.SnapshotAsync(root: button));
        }

        ///<playwright-file>accessibility.spec.js</playwright-file>
        ///<playwright-describe>root option</playwright-describe>
        ///<playwright-it>should show uninteresting nodes</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
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
            var snapshot = await Page.Accessibility.SnapshotAsync(false, root);
            Assert.Equal("textbox", snapshot.Role);
            Assert.Contains("hello", snapshot.ValueString.ToString());
            Assert.Contains("world", snapshot.ValueString.ToString());
            Assert.NotEmpty(snapshot.Children);
        }
    }
}
