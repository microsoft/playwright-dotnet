using System;
using System.Linq;
using System.Threading.Tasks;
using KellermanSoftware.CompareNetObjects;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests
{
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class PageAccessibilityTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PageAccessibilityTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("page-accessibility.spec.ts", "should work")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWork()
        {
            await Page.SetContentAsync(@"
            <head>
                <title>Accessibility Test</title>
            </head>
            <body>
                <h1>Inputs</h1>
                <input placeholder='Empty input' autofocus />
                <input placeholder='readonly input' readonly />
                <input placeholder='disabled input' disabled />
                <input aria-label='Input with whitespace' value='  ' />
                <input value='value only' />
                <input aria-placeholder='placeholder' value='and a value' />
                <div aria-hidden='true' id='desc'>This is a description!</div>
                <input aria-placeholder='placeholder' value='and a value' aria-describedby='desc' />
            </body>");

            AccessibilitySnapshotResult nodeToCheck;

            if (TestConstants.IsFirefox)
            {
                nodeToCheck = new AccessibilitySnapshotResult
                {
                    Role = "document",
                    Name = "Accessibility Test",
                    Children = new AccessibilitySnapshotResult[]
                    {
                        new AccessibilitySnapshotResult
                        {
                            Role = "heading",
                            Name = "Inputs",
                            Level = 1
                        },
                        new AccessibilitySnapshotResult{
                            Role = "textbox",
                            Name = "Empty input",
                            Focused = true
                        },
                        new AccessibilitySnapshotResult{
                            Role = "textbox",
                            Name = "readonly input",
                            Readonly = true
                        },
                        new AccessibilitySnapshotResult{
                            Role = "textbox",
                            Name = "disabled input",
                            Disabled= true
                        },
                        new AccessibilitySnapshotResult{
                            Role = "textbox",
                            Name = "Input with whitespace",
                            Value= "  "
                        },
                        new AccessibilitySnapshotResult{
                            Role = "textbox",
                            Name = string.Empty,
                            Value= "value only"
                        },
                        new AccessibilitySnapshotResult{
                            Role = "textbox",
                            Name = string.Empty,
                            Value= "and a value"
                        },
                        new AccessibilitySnapshotResult{
                            Role = "textbox",
                            Name = string.Empty,
                            Value= "and a value",
                            Description= "This is a description!"}
                    }.ToList()
                };
            }
            else if (TestConstants.IsChromium)
            {
                nodeToCheck = new AccessibilitySnapshotResult
                {
                    Role = "WebArea",
                    Name = "Accessibility Test",
                    Children = new AccessibilitySnapshotResult[]
                    {
                        new AccessibilitySnapshotResult
                        {
                            Role = "heading",
                            Name = "Inputs",
                            Level = 1
                        },
                        new AccessibilitySnapshotResult{
                            Role = "textbox",
                            Name = "Empty input",
                            Focused = true
                        },
                        new AccessibilitySnapshotResult{
                            Role = "textbox",
                            Name = "readonly input",
                            Readonly = true
                        },
                        new AccessibilitySnapshotResult{
                            Role = "textbox",
                            Name = "disabled input",
                            Disabled= true
                        },
                        new AccessibilitySnapshotResult{
                            Role = "textbox",
                            Name = "Input with whitespace",
                            Value= "  "
                        },
                        new AccessibilitySnapshotResult{
                            Role = "textbox",
                            Name = "",
                            Value= "value only"
                        },
                        new AccessibilitySnapshotResult{
                            Role = "textbox",
                            Name = "placeholder",
                            Value= "and a value"
                        },
                        new AccessibilitySnapshotResult{
                            Role = "textbox",
                            Name = "placeholder",
                            Value= "and a value",
                            Description= "This is a description!"}
                    }.ToList()
                };
            }
            else
            {
                nodeToCheck = new AccessibilitySnapshotResult
                {
                    Role = "WebArea",
                    Name = "Accessibility Test",
                    Children = new AccessibilitySnapshotResult[]
                    {
                        new AccessibilitySnapshotResult
                        {
                            Role = "heading",
                            Name = "Inputs",
                            Level = 1
                        },
                        new AccessibilitySnapshotResult {
                            Role = "textbox",
                            Name = "Empty input",
                            Focused = true
                        },
                        new AccessibilitySnapshotResult {
                            Role = "textbox",
                            Name = "readonly input",
                            Readonly = true
                        },
                        new AccessibilitySnapshotResult {
                            Role = "textbox",
                            Name = "disabled input",
                            Disabled = true
                        },
                        new AccessibilitySnapshotResult {
                            Role = "textbox",
                            Name = "Input with whitespace",
                            Value = "  "
                        },
                        new AccessibilitySnapshotResult {
                            Role = "textbox",
                            Name = "",
                            Value = "value only"
                        },
                        new AccessibilitySnapshotResult {
                            Role = "textbox",
                            Name = "placeholder",
                            Value = "and a value"
                        },
                        new AccessibilitySnapshotResult {
                            Role = "textbox",
                            Name = "This is a description!",
                            Value = "and a value" }
                    }.ToList()
                };
            }

            var snapshot = (await Page.Accessibility.SnapshotAsync());

            CompareLogic compareLogic = new CompareLogic();
            var result = compareLogic.Compare(nodeToCheck, snapshot);
            Assert.True(result.AreEqual, result.DifferencesString);
        }

        [PlaywrightTest("page-accessibility.spec.ts", "should work with regular text")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithRegularRext()
        {
            await Page.SetContentAsync("<div>Hello World</div>");
            var snapshot = (await Page.Accessibility.SnapshotAsync());
            Assert.Equal(new AccessibilitySnapshotResult
            {
                Role = TestConstants.IsFirefox ? "text leaf" : "text",
                Name = "Hello World",
            }, snapshot.Children.First());
        }

        [PlaywrightTest("page-accessibility.spec.ts", "roledescription")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task RoleDescription()
        {
            await Page.SetContentAsync("<div tabIndex=-1 aria-roledescription=\"foo\">Hi</div>");
            var snapshot = (await Page.Accessibility.SnapshotAsync());
            Assert.Equal("foo", snapshot.Children.First().Roledescription);
        }

        [PlaywrightTest("page-accessibility.spec.ts", "orientation")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task Orientation()
        {
            await Page.SetContentAsync("<a href=\"\" role=\"slider\" aria-orientation=\"vertical\">11</a>");
            var snapshot = (await Page.Accessibility.SnapshotAsync());
            Assert.Equal("vertical", snapshot.Children.First().Orientation);
        }

        [PlaywrightTest("page-accessibility.spec.ts", "autocomplete")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task Autocomplete()
        {
            await Page.SetContentAsync("<div role=\"textbox\" aria-autocomplete=\"list\">hi</div>");
            var snapshot = (await Page.Accessibility.SnapshotAsync());
            Assert.Equal("list", snapshot.Children.First().Autocomplete);
        }

        [PlaywrightTest("page-accessibility.spec.ts", "multiselectable")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task Multiselectable()
        {
            await Page.SetContentAsync("<div role=\"grid\" tabIndex=-1 aria-multiselectable=true>hey</div>");
            var snapshot = (await Page.Accessibility.SnapshotAsync());
            Assert.True(snapshot.Children.First().Multiselectable);
        }

        [PlaywrightTest("page-accessibility.spec.ts", "keyshortcuts")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task KeyShortcuts()
        {
            await Page.SetContentAsync("<div role=\"grid\" tabIndex=-1 aria-keyshortcuts=\"foo\">hey</div>");
            var snapshot = (await Page.Accessibility.SnapshotAsync());
            Assert.Equal("foo", snapshot.Children.First().Keyshortcuts);
        }

        [PlaywrightTest("page-accessibility.spec.ts", "filtering children of leaf nodes")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task FilteringChildrenOfLeafNodes()
        {
            await Page.SetContentAsync(@"
            <div role=""tablist"">
                <div role=""tab"" aria-selected=""true""><b>Tab1</b></div>
                <div role=""tab"">Tab2</div>
            </div>
            ");
            var snapshot = (await Page.Accessibility.SnapshotAsync());
            Assert.Equal(
                new AccessibilitySnapshotResult
                {
                    Role = TestConstants.IsFirefox ? "document" : "WebArea",
                    Name = "",
                    Children = new[]
                    {
                        new AccessibilitySnapshotResult
                        {
                            Role = "tab",
                            Name = "Tab1",
                            Selected = true
                        },
                        new AccessibilitySnapshotResult
                        {
                            Role = "tab",
                            Name = "Tab2",
                        }
                    }
                },
                snapshot);
        }

        [PlaywrightTest("page-accessibility.spec.ts", "rich text editable fields should have children")]
        [SkipBrowserAndPlatformFact(skipWebkit: true)]
        public async Task RichTextEditableFieldsShouldHaveChildren()
        {
            await Page.SetContentAsync(@"
            <div contenteditable='true'>
                Edit this image: <img src='fakeimage.png' alt='my fake image'>
            </div>");

            AccessibilitySnapshotResult node;

            if (TestConstants.IsFirefox)
            {
                node = new AccessibilitySnapshotResult
                {
                    Role = "section",
                    Name = "",
                    Children = new AccessibilitySnapshotResult[]
                    {
                        new AccessibilitySnapshotResult
                        {
                            Role = "text leaf",
                            Name = "Edit this image: "
                        },
                        new AccessibilitySnapshotResult
                        {
                            Role = "text",
                            Name = "my fake image"
                        }
                    }
                };
            }
            else
            {
                node = new AccessibilitySnapshotResult
                {
                    Role = "generic",
                    Name = "",
                    Value = "Edit this image: ",
                    Children = new AccessibilitySnapshotResult[]
                    {
                        new AccessibilitySnapshotResult
                        {
                            Role = "text",
                            Name = "Edit this image:"
                        },
                        new AccessibilitySnapshotResult
                        {
                            Role = "img",
                            Name = "my fake image"
                        }
                    }
                };
            }

            var snapshot = (await Page.Accessibility.SnapshotAsync());
            Assert.Equal(node, snapshot.Children.First());
        }

        [PlaywrightTest("page-accessibility.spec.ts", "rich text editable fields with role should have children")]
        [SkipBrowserAndPlatformFact(skipWebkit: true)]
        public async Task RichTextEditableFieldsWithRoleShouldHaveChildren()
        {
            await Page.SetContentAsync(@"
            <div contenteditable='true' role='textbox'>
                Edit this image: <img src='fakeimage.png' alt='my fake image'>
            </div>");

            AccessibilitySnapshotResult node;

            if (TestConstants.IsFirefox)
            {
                node = new AccessibilitySnapshotResult
                {
                    Role = "textbox",
                    Name = "",
                    Value = "Edit this image: my fake image",
                    Children = new AccessibilitySnapshotResult[]
                    {
                        new AccessibilitySnapshotResult
                        {
                            Role = "text",
                            Name = "my fake image"
                        }
                    }
                };
            }
            else
            {
                node = new AccessibilitySnapshotResult
                {
                    Role = "textbox",
                    Name = "",
                    Value = "Edit this image: ",
                    Children = new AccessibilitySnapshotResult[]
                    {
                        new AccessibilitySnapshotResult
                        {
                            Role = "text",
                            Name = "Edit this image:"
                        },
                        new AccessibilitySnapshotResult
                        {
                            Role = "img",
                            Name = "my fake image"
                        }
                    }
                };
            }

            Assert.Equal(node, (await Page.Accessibility.SnapshotAsync()).Children.First());
        }

        [PlaywrightTest("page-accessibility.spec.ts", "should work a button")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkAButton()
        {
            await Page.SetContentAsync("<button>My Button</button>");
            var button = await Page.QuerySelectorAsync("button");

            Assert.Equal(
                new AccessibilitySnapshotResult
                {
                    Role = "button",
                    Name = "My Button"
                },
                (await Page.Accessibility.SnapshotAsync(root: button)));
        }

        [PlaywrightTest("page-accessibility.spec.ts", "should work an input")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkAnInput()
        {
            await Page.SetContentAsync("<input title=\"My Input\" value=\"My Value\">");
            var input = await Page.QuerySelectorAsync("input");

            Assert.Equal(
                new AccessibilitySnapshotResult
                {
                    Role = "textbox",
                    Name = "My Input",
                    Value = "My Value"
                },
                (await Page.Accessibility.SnapshotAsync(root: input)));
        }

        [PlaywrightTest("page-accessibility.spec.ts", "should work on a menu")]
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

            var nodeToCheck = new AccessibilitySnapshotResult
            {
                Role = "menu",
                Name = "My Menu",
                Children = new AccessibilitySnapshotResult[]
                    {
                        new AccessibilitySnapshotResult
                        {
                            Role = "menuitem",
                            Name = "First Item"
                        },
                        new AccessibilitySnapshotResult
                        {
                            Role = "menuitem",
                            Name = "Second Item"
                        },
                        new AccessibilitySnapshotResult
                        {
                            Role = "menuitem",
                            Name = "Third Item"
                        }
                    }.ToList(),
                Orientation = TestConstants.IsWebKit ? "vertical" : null
            };

            CompareLogic compareLogic = new CompareLogic();
            var result = compareLogic.Compare(nodeToCheck, (await Page.Accessibility.SnapshotAsync(root: menu)));
            Assert.True(result.AreEqual, result.DifferencesString);
        }

        [PlaywrightTest("page-accessibility.spec.ts", "should return null when the element is no longer in DOM")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldReturnNullWhenTheElementIsNoLongerInDOM()
        {
            await Page.SetContentAsync("<button>My Button</button>");
            var button = await Page.QuerySelectorAsync("button");
            await Page.EvalOnSelectorAsync("button", "button => button.remove()");

            Assert.Null(await Page.Accessibility.SnapshotAsync(root: button));
        }

        [PlaywrightTest("page-accessibility.spec.ts", "should show uninteresting nodes")]
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
            var snapshot = (await Page.Accessibility.SnapshotAsync(false, root));
            Assert.Equal("textbox", snapshot.Role);
            Assert.Contains("hello", snapshot.Value.ToString());
            Assert.Contains("world", snapshot.Value.ToString());
            Assert.NotEmpty(snapshot.Children);
        }
    }
}
