using System.Threading.Tasks;
using PlaywrightSharp.Accessibility;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Accessibility
{
    ///<playwright-file>accessibility.spec.js</playwright-file>
    ///<playwright-describe>plaintext contenteditable</playwright-describe>
    public class PlainTextContentEditableTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PlainTextContentEditableTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>accessibility.spec.js</playwright-file>
        ///<playwright-describe>plaintext contenteditable</playwright-describe>
        ///<playwright-it>plain text field with role should not have children</playwright-it>
        [SkipBrowserAndPlayformFact(skipWebkit: true, skipFirefox: true)]
        public async Task PlainTextFieldWithRoleShouldNotHaveChildren()
        {
            await Page.SetContentAsync("<div contenteditable='plaintext-only' role='textbox'>Edit this image:<img src='fakeimage.png' alt='my fake image'></div>");
            Assert.Equal(
                new SerializedAXNode
                {
                    Role = "textbox",
                    Name = "",
                    Value = "Edit this image:"
                },
                (await Page.Accessibility.SnapshotAsync()).Children[0]);
        }

        ///<playwright-file>accessibility.spec.js</playwright-file>
        ///<playwright-describe>plaintext contenteditable</playwright-describe>
        ///<playwright-it>plain text field without role should not have content</playwright-it>
        [SkipBrowserAndPlayformFact(skipWebkit: true, skipFirefox: true)]
        public async Task PlainTextFieldWithoutRoleShouldNotHaveContent()
        {
            await Page.SetContentAsync(
                "<div contenteditable='plaintext-only'>Edit this image:<img src='fakeimage.png' alt='my fake image'></div>");
            var snapshot = await Page.Accessibility.SnapshotAsync();
            Assert.Equal("GenericContainer", snapshot.Children[0].Role);
            Assert.Equal(string.Empty, snapshot.Children[0].Name);
        }

        ///<playwright-file>accessibility.spec.js</playwright-file>
        ///<playwright-describe>plaintext contenteditable</playwright-describe>
        ///<playwright-it>plain text field with tabindex and without role should not have content</playwright-it>
        [SkipBrowserAndPlayformFact(skipWebkit: true, skipFirefox: true)]
        public async Task PlainTextFieldWithTabindexAndWithoutRoleShouldNotHaveContent()
        {
            await Page.SetContentAsync("div contenteditable=\"plaintext-only\" tabIndex=0>Edit this image:<img src='fakeimage.png' alt='my fake image'></div>");
            Assert.Equal(
                new SerializedAXNode
                {
                    Role = "textbox",
                    Name = "",
                    Value = "Edit this image:"
                },
                (await Page.Accessibility.SnapshotAsync()).Children[0]);
        }

        ///<playwright-file>accessibility.spec.js</playwright-file>
        ///<playwright-describe>plaintext contenteditable</playwright-describe>
        ///<playwright-it>non editable textbox with role and tabIndex and label should not have children</playwright-it>
        [SkipBrowserAndPlayformFact(skipWebkit: true, skipFirefox: true)]
        public async Task NonEditableTextboxWithRoleAndTabIndexAndLabelShouldNotHaveChildren()
        {
            await Page.SetContentAsync(@"
            <div role='textbox' tabIndex=0 aria-checked='true' aria-label='my favorite textbox'>
                this is the inner content
                <img alt='yo' src='fakeimg.png'>
            </div>");

            SerializedAXNode node = null;

            if (TestConstants.IsFirefox)
            {
                node = new SerializedAXNode
                {
                    Role = "textbox",
                    Name = "my favorite textbox",
                    Value = "this is the inner content yo"
                };
            }
            else if (TestConstants.IsChromium)
            {
                node = new SerializedAXNode
                {
                    Role = "textbox",
                    Name = "my favorite textbox",
                    Value = "this is the inner content "
                };
            }
            else
            {
                node = new SerializedAXNode
                {
                    Role = "textbox",
                    Name = "my favorite textbox",
                    Value = "this is the inner content  "
                };
            }
            Assert.Equal(node, (await Page.Accessibility.SnapshotAsync()).Children[0]);
        }

        ///<playwright-file>accessibility.spec.js</playwright-file>
        ///<playwright-describe>plaintext contenteditable</playwright-describe>
        ///<playwright-it>checkbox with and tabIndex and label should not have children</playwright-it>
        [SkipBrowserAndPlayformFact(skipWebkit: true, skipFirefox: true)]
        public async Task CheckboxWithAndTabIndexAndLabelShouldNotHaveChildren()
        {
            await Page.SetContentAsync(@"
            <div role='checkbox' tabIndex=0 aria-checked='true' aria-label='my favorite checkbox'>
                this is the inner content
                <img alt='yo' src='fakeimg.png'>
            </div>");
            Assert.Equal(
                new SerializedAXNode
                {
                    Role = "checkbox",
                    Name = "my favorite checkbox",
                    Checked = CheckedState.True
                },
                (await Page.Accessibility.SnapshotAsync()).Children[0]);
        }

        ///<playwright-file>accessibility.spec.js</playwright-file>
        ///<playwright-describe>plaintext contenteditable</playwright-describe>
        ///<playwright-it>checkbox without label should not have children</playwright-it>
        [SkipBrowserAndPlayformFact(skipWebkit: true, skipFirefox: true)]
        public async Task CheckboxWithoutLabelShouldNotHaveChildren()
        {
            await Page.SetContentAsync(@"
            <div role='checkbox' aria-checked='true'>
                this is the inner content
                <img alt='yo' src='fakeimg.png'>
            </div>");

            SerializedAXNode node;
            if (TestConstants.IsFirefox)
            {
                node = new SerializedAXNode
                {
                    Role = "checkbox",
                    Name = "this is the inner content yo",
                    Checked = CheckedState.True
                };
            }
            else
            {
                node = new SerializedAXNode
                {
                    Role = "checkbox",
                    Name = "this is the inner content yo",
                    Checked = CheckedState.True
                };
            }

            Assert.Equal(node, (await Page.Accessibility.SnapshotAsync()).Children[0]);
        }
    }
}
