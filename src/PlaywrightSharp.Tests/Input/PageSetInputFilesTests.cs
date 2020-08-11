using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Input
{
    ///<playwright-file>input.spec.js</playwright-file>
    ///<playwright-describe>Page.setInputFiles</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class PageSetInputFilesTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PageSetInputFilesTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>input.spec.js</playwright-file>
        ///<playwright-describe>Page.setInputFiles</playwright-describe>
        ///<playwright-it>should work</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldWork()
        {
            await Page.SetContentAsync("<input type=file>");
            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "Assets", TestConstants.FileToUpload);
            await Page.SetInputFilesAsync("input", filePath);

            Assert.Equal(1, await Page.QuerySelectorEvaluateAsync<int>("input", "e => e.files.length"));
            Assert.Equal("file-to-upload.txt", await Page.QuerySelectorEvaluateAsync<string>("input", "e => e.files[0].name"));
        }

        ///<playwright-file>input.spec.js</playwright-file>
        ///<playwright-describe>Page.setInputFiles</playwright-describe>
        ///<playwright-it>should set from memory</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldSetFromMemory()
        {
            await Page.SetContentAsync("<input type=file>");

            await Page.SetInputFilesAsync("input", new FilePayload
            {
                Name = "test.txt",
                MimeType = "text/plain",
                Buffer = Convert.ToBase64String(Encoding.UTF8.GetBytes("this is a test"))
            });

            Assert.Equal(1, await Page.QuerySelectorEvaluateAsync<int>("input", "e => e.files.length"));
            Assert.Equal("test.txt", await Page.QuerySelectorEvaluateAsync<string>("input", "e => e.files[0].name"));
        }
    }
}
