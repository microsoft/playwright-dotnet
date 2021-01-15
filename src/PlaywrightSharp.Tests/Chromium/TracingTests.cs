using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using PlaywrightSharp.Chromium;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Chromium
{
    ///<playwright-file>chromium/tracing.spec.js</playwright-file>
    ///<playwright-describe>Chromium.startTracing</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class TracingTests : PlaywrightSharpPageBaseTest
    {
        private readonly string _file;

        /// <inheritdoc/>
        public TracingTests(ITestOutputHelper output) : base(output)
        {
            _file = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        }

        /// <inheritdoc cref="IAsyncLifetime.DisposeAsync"/>
        public override async Task DisposeAsync()
        {
            await base.DisposeAsync();

            int attempts = 0;
            const int maxAttempts = 5;

            while (true)
            {
                try
                {
                    attempts++;
                    if (File.Exists(_file))
                    {
                        File.Delete(_file);
                    }
                    break;
                }
                catch (UnauthorizedAccessException)
                {
                    if (attempts == maxAttempts)
                    {
                        break;
                    }

                    await Task.Delay(1000);
                }
            }
        }

        [PlaywrightTest("chromium/tracing.spec.js", "Chromium.startTracing", "should output a trace")]
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldOutputATrace()
        {
            await ((IChromiumBrowser)Browser).StartTracingAsync(Page, screenshots: true, path: _file);
            await Page.GoToAsync(TestConstants.ServerUrl + "/grid.html");
            await ((IChromiumBrowser)Browser).StopTracingAsync();

            Assert.True(File.Exists(_file));
        }

        [PlaywrightTest("chromium/tracing.spec.js", "Chromium.startTracing", "should run with custom categories if provided")]
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldRunWithCustomCategoriesProvided()
        {
            await ((IChromiumBrowser)Browser).StartTracingAsync(Page, true, _file, new[] { "disabled-by-default-v8.cpu_profiler.hires" });

            await ((IChromiumBrowser)Browser).StopTracingAsync();

            string jsonString = File.ReadAllText(_file);
            var traceJson = JsonDocument.Parse(jsonString);
            Assert.Contains("disabled-by-default-v8.cpu_profiler.hires", traceJson.RootElement.GetProperty("metadata").GetProperty("trace-config").ToString());
        }

        [PlaywrightTest("chromium/tracing.spec.js", "Chromium.startTracing", "should throw if tracing on two pages")]
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldThrowIfTracingOnTwoPages()
        {
            await ((IChromiumBrowser)Browser).StartTracingAsync(Page, path: _file);
            var newPage = await Browser.NewPageAsync();
            await Assert.ThrowsAsync<PlaywrightSharpException>(async () =>
            {
                await ((IChromiumBrowser)Browser).StartTracingAsync(newPage, path: _file);
            });

            await newPage.CloseAsync();
            await ((IChromiumBrowser)Browser).StopTracingAsync();
        }

        [PlaywrightTest("chromium/tracing.spec.js", "Chromium.startTracing", "should return a buffer")]
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldReturnABuffer()
        {
            await ((IChromiumBrowser)Browser).StartTracingAsync(Page, true, _file);
            await Page.GoToAsync(TestConstants.ServerUrl + "/grid.html");
            string trace = await ((IChromiumBrowser)Browser).StopTracingAsync();
            string buf = File.ReadAllText(_file);
            Assert.Equal(trace, buf);
        }

        [PlaywrightTest("chromium/tracing.spec.js", "Chromium.startTracing", "should work without options")]
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldWorkWithoutOptions()
        {
            await ((IChromiumBrowser)Browser).StartTracingAsync(Page);
            await Page.GoToAsync(TestConstants.ServerUrl + "/grid.html");
            string trace = await ((IChromiumBrowser)Browser).StopTracingAsync();
            Assert.NotNull(trace);
        }

        [PlaywrightTest("chromium/tracing.spec.js", "Chromium.startTracing", "should support a buffer without a path")]
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldSupportABufferWithoutAPath()
        {
            await ((IChromiumBrowser)Browser).StartTracingAsync(Page, true);
            await Page.GoToAsync(TestConstants.ServerUrl + "/grid.html");
            string trace = await ((IChromiumBrowser)Browser).StopTracingAsync();
            Assert.Contains("screenshot", trace);
        }
    }
}
