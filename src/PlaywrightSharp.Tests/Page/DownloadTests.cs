using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Page
{
    ///<playwright-file>download.spec.js</playwright-file>
    ///<playwright-describe>Download</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class DownloadTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public DownloadTests(ITestOutputHelper output) : base(output)
        {
            Server.SetRoute("/download", context =>
            {
                context.Response.Headers["Content-Type"] = "application/octet-stream";
                context.Response.Headers["Content-Disposition"] = "attachment";
                return context.Response.WriteAsync("Hello World");
            });

            Server.SetRoute("/download", context =>
            {
                context.Response.Headers["Content-Type"] = "application/octet-stream";
                context.Response.Headers["Content-Disposition"] = "attachment; filename=file.txt";
                return context.Response.WriteAsync("Hello World");
            });
        }

        ///<playwright-file>download.spec.js</playwright-file>
        ///<playwright-describe>Download</playwright-describe>
        ///<playwright-it>should report downloads with acceptDownloads: false</playwright-it>
        [Retry]
        public async Task ShouldReportDownloadsWithAcceptDownloadsFalse()
        {
            await Page.SetContentAsync($"<a href=\"{TestConstants.ServerUrl}/downloadWithFilename\">download</a>");
            var downloadTask = Page.WaitForEvent<DownloadEventArgs>(PageEvent.Download);

            await Task.WhenAll(
                downloadTask,
                Page.ClickAsync("a"));

            var download = downloadTask.Result.Download;
            Assert.Equal($"{TestConstants.ServerUrl}/downloadWithFilename", download.Url);
            Assert.Equal("file.txt", dowload.SuggestedFilename);
            await download.path().catch (e => error = e);
            expect(await download.failure()).toContain('acceptDownloads');
            expect(error.message).toContain('acceptDownloads: true');
            }

        }
    }
