using System.Linq;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests
{
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class PageAccessibilityContentEditableTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PageAccessibilityContentEditableTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("page-accessibility.spec.ts", "contenteditable", "plain text field with role should not have children")]
        [SkipBrowserAndPlatformFact(skipWebkit: true, skipFirefox: true)]
        public async Task PlainTextFieldWithRoleShouldNotHaveChildren()
        {
            await Page.SetContentAsync("<div contenteditable='plaintext-only' role='textbox'>Edit this image:<img src='fakeimage.png' alt='my fake image'></div>");
            var snapshot = (await Page.Accessibility.SnapshotAsync());
            Assert.Equal(
                new AccessibilitySnapshotResult
                {
                    Role = "textbox",
                    Name = "",
                    Value = "Edit this image:"
                },
               snapshot.Children.First());
        }

        [PlaywrightTest("page-accessibility.spec.ts", "contenteditable", "plain text field without role should not have content")]
        [SkipBrowserAndPlatformFact(skipWebkit: true, skipFirefox: true)]
        public async Task PlainTextFieldWithoutRoleShouldNotHaveContent()
        {
            await Page.SetContentAsync(
                "<div contenteditable='plaintext-only'>Edit this image:<img src='fakeimage.png' alt='my fake image'></div>");
            var snapshot = (await Page.Accessibility.SnapshotAsync());
            Assert.Equal("generic", snapshot.Children.First().Role);
            Assert.Equal(string.Empty, snapshot.Children.First().Name);
        }

        [PlaywrightTest("page-accessibility.spec.ts", "contenteditable", "plain text field with tabindex and without role should not have content")]
        [SkipBrowserAndPlatformFact(skipWebkit: true, skipFirefox: true)]
        public async Task PlainTextFieldWithTabindexAndWithoutRoleShouldNotHaveContent()
        {
            await Page.SetContentAsync("<div contenteditable=\"plaintext-only\" tabIndex=0>Edit this image:<img src='fakeimage.png' alt='my fake image'></div>");
            var node = (await Page.Accessibility.SnapshotAsync()).Children.First();
            Assert.Equal("generic", node.Role);
            Assert.Empty(node.Name);
        }

        [PlaywrightTest("page-accessibility.spec.ts", "contenteditable", "non editable textbox with role and tabIndex and label should not have children")]
        [SkipBrowserAndPlatformFact(skipWebkit: true, skipFirefox: true)]
        public async Task NonEditableTextboxWithRoleAndTabIndexAndLabelShouldNotHaveChildren()
        {
            await Page.SetContentAsync(@"
            <div role='textbox' tabIndex=0 aria-checked='true' aria-label='my favorite textbox'>
                this is the inner content
                <img alt='yo' src='fakeimg.png'>
            </div>");

            AccessibilitySnapshotResult node = null;

            if (TestConstants.IsFirefox)
            {
                node = new AccessibilitySnapshotResult
                {
                    Role = "textbox",
                    Name = "my favorite textbox",
                    Value = "this is the inner content yo"
                };
            }
            else if (TestConstants.IsChromium)
            {
                node = new AccessibilitySnapshotResult
                {
                    Role = "textbox",
                    Name = "my favorite textbox",
                    Value = "this is the inner content "
                };
            }
            else
            {
                node = new AccessibilitySnapshotResult
                {
                    Role = "textbox",
                    Name = "my favorite textbox",
                    Value = "this is the inner content  "
                };
            }
            Assert.Equal(node, (await Page.Accessibility.SnapshotAsync()).Children.First());
        }

        [PlaywrightTest("page-accessibility.spec.ts", "contenteditable", "checkbox with and tabIndex and label should not have children")]
        [SkipBrowserAndPlatformFact(skipWebkit: true, skipFirefox: true)]
        public async Task CheckboxWithAndTabIndexAndLabelShouldNotHaveChildren()
        {
            await Page.SetContentAsync(@"
            <div role='checkbox' tabIndex=0 aria-checked='true' aria-label='my favorite checkbox'>
                this is the inner content
                <img alt='yo' src='fakeimg.png'>
            </div>");
            Assert.Equal(
                new AccessibilitySnapshotResult
                {
                    Role = "checkbox",
                    Name = "my favorite checkbox",
                    Checked = MixedState.On
                },
                (await Page.Accessibility.SnapshotAsync()).Children.First());
        }

        [PlaywrightTest("page-accessibility.spec.ts", "contenteditable", "checkbox without label should not have children")]
        [SkipBrowserAndPlatformFact(skipWebkit: true, skipFirefox: true)]
        public async Task CheckboxWithoutLabelShouldNotHaveChildren()
        {
            await Page.SetContentAsync(@"
            <div role='checkbox' aria-checked='true'>
                this is the inner content
                <img alt='yo' src='fakeimg.png'>
            </div>");

            AccessibilitySnapshotResult node;
            if (TestConstants.IsFirefox)
            {
                node = new AccessibilitySnapshotResult
                {
                    Role = "checkbox",
                    Name = "this is the inner content yo",
                    Checked = MixedState.On,
                };
            }
            else
            {
                node = new AccessibilitySnapshotResult
                {
                    Role = "checkbox",
                    Name = "this is the inner content yo",
                    Checked = MixedState.On,
                };
            }

            Assert.Equal(node, (await Page.Accessibility.SnapshotAsync()).Children.First());
        }
    }
}
