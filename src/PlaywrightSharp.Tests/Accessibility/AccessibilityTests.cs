using System.Threading.Tasks;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Accessibility
{
    ///<playwright-file>accessibility.spec.js</playwright-file>
    ///<playwright-describe>Accessibility</playwright-describe>
    [Trait("Category", "chromium")]
    [Trait("Category", "firefox")]
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class AccessibilityTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public AccessibilityTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>accessibility.spec.js</playwright-file>
        ///<playwright-describe>Accessibility</playwright-describe>
        ///<playwright-it>should work</playwright-it>
        [Retry]
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

            SerializedAXNode nodeToCheck;

            if (TestConstants.IsFirefox)
            {
                nodeToCheck = new SerializedAXNode
                {
                    Role = "document",
                    Name = "Accessibility Test",
                    Children = new SerializedAXNode[]
                    {
                        new SerializedAXNode
                        {
                            Role = "heading",
                            Name = "Inputs",
                            Level = 1
                        },
                        new SerializedAXNode{
                            Role = "textbox",
                            Name = "Empty input",
                            Focused = true
                        },
                        new SerializedAXNode{
                            Role = "textbox",
                            Name = "readonly input",
                            Readonly = true
                        },
                        new SerializedAXNode{
                            Role = "textbox",
                            Name = "disabled input",
                            Disabled= true
                        },
                        new SerializedAXNode{
                            Role = "textbox",
                            Name = "Input with whitespace",
                            Value= "  "
                        },
                        new SerializedAXNode{
                            Role = "textbox",
                            Name = string.Empty,
                            Value= "value only"
                        },
                        new SerializedAXNode{
                            Role = "textbox",
                            Name = string.Empty,
                            Value= "and a value"
                        },
                        new SerializedAXNode{
                            Role = "textbox",
                            Name = string.Empty,
                            Value= "and a value",
                            Description= "This is a description!"}
                    }
                };
            }
            else if (TestConstants.IsChromium)
            {
                nodeToCheck = new SerializedAXNode
                {
                    Role = "WebArea",
                    Name = "Accessibility Test",
                    Children = new SerializedAXNode[]
                    {
                        new SerializedAXNode
                        {
                            Role = "heading",
                            Name = "Inputs",
                            Level = 1
                        },
                        new SerializedAXNode{
                            Role = "textbox",
                            Name = "Empty input",
                            Focused = true
                        },
                        new SerializedAXNode{
                            Role = "textbox",
                            Name = "readonly input",
                            Readonly = true
                        },
                        new SerializedAXNode{
                            Role = "textbox",
                            Name = "disabled input",
                            Disabled= true
                        },
                        new SerializedAXNode{
                            Role = "textbox",
                            Name = "Input with whitespace",
                            Value= "  "
                        },
                        new SerializedAXNode{
                            Role = "textbox",
                            Name = "",
                            Value= "value only"
                        },
                        new SerializedAXNode{
                            Role = "textbox",
                            Name = "placeholder",
                            Value= "and a value"
                        },
                        new SerializedAXNode{
                            Role = "textbox",
                            Name = "placeholder",
                            Value= "and a value",
                            Description= "This is a description!"}
                    }
                };
            }
            else
            {
                nodeToCheck = new SerializedAXNode
                {
                    Role = "WebArea",
                    Name = "Accessibility Test",
                    Children = new SerializedAXNode[]
                    {
                        new SerializedAXNode
                        {
                            Role = "heading",
                            Name = "Inputs",
                            Level = 1
                        },
                        new SerializedAXNode{
                            Role = "textbox",
                            Name = "Empty input",
                            Focused = true
                        },
                        new SerializedAXNode{
                            Role = "textbox",
                            Name = "readonly input",
                            Readonly = true
                        },
                        new SerializedAXNode{
                            Role = "textbox",
                            Name = "disabled input",
                            Disabled= true
                        },
                        new SerializedAXNode{
                            Role = "textbox",
                            Name = "Input with whitespace",
                            Value= "  "
                        },
                        new SerializedAXNode{
                            Role = "textbox",
                            Name = "",
                            Value= "value only"
                        },
                        new SerializedAXNode{
                            Role = "textbox",
                            Name = "placeholder",
                            Value= "and a value"
                        },
                        new SerializedAXNode{
                            Role = "textbox",
                            Name = "This is a description!",
                            Value= "and a value"}
                    }
                };
            }

            var snapshot = await Page.Accessibility.SnapshotAsync();
            Assert.Equal(nodeToCheck, snapshot);
        }

        ///<playwright-file>accessibility.spec.js</playwright-file>
        ///<playwright-describe>Accessibility</playwright-describe>
        ///<playwright-it>should work with regular text</playwright-it>
        [SkipBrowserAndPlatformFact(skipWebkit: true, skipLinux: true, skipWindows: true)]
        public async Task ShouldWorkWithRegularRext()
        {
            await Page.SetContentAsync("<div>Hello World</div>");
            var snapshot = await Page.Accessibility.SnapshotAsync();
            Assert.Equal(new SerializedAXNode
            {
                Role = TestConstants.IsFirefox ? "text leaf" : "text",
                Name = "Hello World",
            }, snapshot.Children[0]);
        }

        ///<playwright-file>accessibility.spec.js</playwright-file>
        ///<playwright-describe>Accessibility</playwright-describe>
        ///<playwright-it>roledescription</playwright-it>
        [Retry]
        public async Task RoleDescription()
        {
            await Page.SetContentAsync("<div tabIndex=-1 aria-roledescription=\"foo\">Hi</div>");
            var snapshot = await Page.Accessibility.SnapshotAsync();
            Assert.Equal("foo", snapshot.Children[0].RoleDescription);
        }

        ///<playwright-file>accessibility.spec.js</playwright-file>
        ///<playwright-describe>Accessibility</playwright-describe>
        ///<playwright-it>orientation</playwright-it>
        [Retry]
        public async Task Orientation()
        {
            await Page.SetContentAsync("<a href=\"\" role=\"slider\" aria-orientation=\"vertical\">11</a>");
            var snapshot = await Page.Accessibility.SnapshotAsync();
            Assert.Equal("vertical", snapshot.Children[0].Orientation);
        }

        ///<playwright-file>accessibility.spec.js</playwright-file>
        ///<playwright-describe>Accessibility</playwright-describe>
        ///<playwright-it>autocomplete</playwright-it>
        [Retry]
        public async Task Autocomplete()
        {
            await Page.SetContentAsync("<div role=\"textbox\" aria-autocomplete=\"list\">hi</div>");
            var snapshot = await Page.Accessibility.SnapshotAsync();
            Assert.Equal("list", snapshot.Children[0].AutoComplete);
        }

        ///<playwright-file>accessibility.spec.js</playwright-file>
        ///<playwright-describe>Accessibility</playwright-describe>
        ///<playwright-it>multiselectable</playwright-it>
        [Retry]
        public async Task Multiselectable()
        {
            await Page.SetContentAsync("<div role=\"grid\" tabIndex=-1 aria-multiselectable=true>hey</div>");
            var snapshot = await Page.Accessibility.SnapshotAsync();
            Assert.True(snapshot.Children[0].Multiselectable);
        }

        ///<playwright-file>accessibility.spec.js</playwright-file>
        ///<playwright-describe>Accessibility</playwright-describe>
        ///<playwright-it>keyshortcuts</playwright-it>
        [Retry]
        public async Task KeyShortcuts()
        {
            await Page.SetContentAsync("<div role=\"grid\" tabIndex=-1 aria-keyshortcuts=\"foo\">hey</div>");
            var snapshot = await Page.Accessibility.SnapshotAsync();
            Assert.Equal("foo", snapshot.Children[0].KeyShortcuts);
        }

        ///<playwright-file>accessibility.spec.js</playwright-file>
        ///<playwright-describe>Accessibility</playwright-describe>
        ///<playwright-it>filtering children of leaf nodes</playwright-it>
        [SkipBrowserAndPlatformFact(skipWebkit: true, skipWindows: true, skipLinux: true)]
        public async Task FilteringChildrenOfLeafNodes()
        {
            await Page.SetContentAsync(@"
            <div role=""tablist"">
                <div role=""tab"" aria-selected=""true""><b>Tab1</b></div>
                <div role=""tab"">Tab2</div>
            </div>
            ");
            var snapshot = await Page.Accessibility.SnapshotAsync();
            Assert.Equal(
                new SerializedAXNode
                {
                    Role = TestConstants.IsFirefox ? "document" : "WebArea",
                    Name = "",
                    Children = new[]
                    {
                        new SerializedAXNode
                        {
                            Role = "tab",
                            Name = "Tab1",
                            Selected = true
                        },
                        new SerializedAXNode
                        {
                            Role = "tab",
                            Name = "Tab2",
                        }
                    }
                },
                snapshot);
        }

        ///<playwright-file>accessibility.spec.js</playwright-file>
        ///<playwright-describe>Accessibility</playwright-describe>
        ///<playwright-it>rich text editable fields should have children</playwright-it>
        [SkipBrowserAndPlatformFact(skipWebkit: true)]
        public async Task RichTextEditableFieldsShouldHaveChildren()
        {
            await Page.SetContentAsync(@"
            <div contenteditable='true'>
                Edit this image: <img src='fakeimage.png' alt='my fake image'>
            </div>");

            SerializedAXNode node;

            if (TestConstants.IsFirefox)
            {
                node = new SerializedAXNode
                {
                    Role = "section",
                    Name = "",
                    Children = new SerializedAXNode[]
                    {
                        new SerializedAXNode
                        {
                            Role = "text leaf",
                            Name = "Edit this image: "
                        },
                        new SerializedAXNode
                        {
                            Role = "text",
                            Name = "my fake image"
                        }
                    }
                };
            }
            else
            {
                node = new SerializedAXNode
                {
                    Role = "generic",
                    Name = "",
                    Value = "Edit this image: ",
                    Children = new SerializedAXNode[]
                    {
                        new SerializedAXNode
                        {
                            Role = "text",
                            Name = "Edit this image:"
                        },
                        new SerializedAXNode
                        {
                            Role = "img",
                            Name = "my fake image"
                        }
                    }
                };
            }

            var snapshot = await Page.Accessibility.SnapshotAsync();
            Assert.Equal(node, snapshot.Children[0]);
        }

        ///<playwright-file>accessibility.spec.js</playwright-file>
        ///<playwright-describe>Accessibility</playwright-describe>
        ///<playwright-it>rich text editable fields with role should have children</playwright-it>
        [SkipBrowserAndPlatformFact(skipWebkit: true)]
        public async Task RichTextEditableFieldsWithRoleShouldHaveChildren()
        {
            await Page.SetContentAsync(@"
            <div contenteditable='true' role='textbox'>
                Edit this image: <img src='fakeimage.png' alt='my fake image'>
            </div>");

            SerializedAXNode node;

            if (TestConstants.IsFirefox)
            {
                node = new SerializedAXNode
                {
                    Role = "textbox",
                    Name = "",
                    Value = "Edit this image: my fake image",
                    Children = new SerializedAXNode[]
                    {
                        new SerializedAXNode
                        {
                            Role = "text",
                            Name = "my fake image"
                        }
                    }
                };
            }
            else
            {
                node = new SerializedAXNode
                {
                    Role = "textbox",
                    Name = "",
                    Value = "Edit this image: ",
                    Children = new SerializedAXNode[]
                    {
                        new SerializedAXNode
                        {
                            Role = "text",
                            Name = "Edit this image:"
                        },
                        new SerializedAXNode
                        {
                            Role = "img",
                            Name = "my fake image"
                        }
                    }
                };
            }

            Assert.Equal(node, (await Page.Accessibility.SnapshotAsync()).Children[0]);
        }
    }
}
