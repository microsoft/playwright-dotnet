using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using Mono.Unix;
using PlaywrightSharp.Helpers;
using PlaywrightSharp.Helpers.Linux;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Chromium
{
    ///<playwright-file>chromium/tracing.spec.js</playwright-file>
    ///<playwright-describe>Chromium.startTracing</playwright-describe>
    public class TracingTests : PlaywrightSharpPageBaseTest
    {
        private readonly string _file;

        /// <inheritdoc/>
        public TracingTests(ITestOutputHelper output) : base(output)
        {
            _file = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        }

        /// <inheritdoc/>
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

        ///<playwright-file>chromium/tracing.spec.js</playwright-file>
        ///<playwright-describe>Chromium.startTracing</playwright-describe>
        ///<playwright-it>should output a trace</playwright-it>
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldOutputATrace()
        {
            await Browser.StartTracingAsync(new TracingOptions
            {
                Screenshots = true,
                Path = _file
            });
            await Page.GoToAsync(TestConstants.ServerUrl + "/grid.html");
            await Browser.StopTracingAsync();

            Assert.True(File.Exists(_file));
        }

        ///<playwright-file>chromium/tracing.spec.js</playwright-file>
        ///<playwright-describe>Chromium.startTracing</playwright-describe>
        ///<playwright-it>should run with custom categories if provided</playwright-it>
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldRunWithCustomCategoriesProvided()
        {
            await Browser.StartTracingAsync(new TracingOptions
            {
                Screenshots = true,
                Path = _file,
                Categories = new List<string>
                {
                    "disabled-by-default-v8.cpu_profiler.hires"
                }
            });

            await Browser.StopTracingAsync();

            string jsonString = File.ReadAllText(_file);
            var traceJson = JsonDocument.Parse(jsonString);
            Assert.Contains("disabled-by-default-v8.cpu_profiler.hires", traceJson.RootElement.GetProperty("metadata").GetProperty("trace-config").ToString());

        }

        ///<playwright-file>chromium/tracing.spec.js</playwright-file>
        ///<playwright-describe>Chromium.startTracing</playwright-describe>
        ///<playwright-it>should throw if tracing on two pages</playwright-it>
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldThrowIfTracingOnTwoPages()
        {
            await Browser.StartTracingAsync(new TracingOptions
            {
                Path = _file
            });
            var newPage = await Browser.DefaultContext.NewPageAsync();
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await Browser.StartTracingAsync(new TracingOptions
                {
                    Path = _file
                });
            });

            await newPage.CloseAsync();
            await Browser.StopTracingAsync();
        }

        ///<playwright-file>chromium/tracing.spec.js</playwright-file>
        ///<playwright-describe>Chromium.startTracing</playwright-describe>
        ///<playwright-it>should return a buffer</playwright-it>
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldReturnABuffer()
        {
            await Browser.StartTracingAsync(new TracingOptions
            {
                Screenshots = true,
                Path = _file
            });
            await Page.GoToAsync(TestConstants.ServerUrl + "/grid.html");
            var trace = await Browser.StopTracingAsync();
            var buf = File.ReadAllText(_file);
            Assert.Equal(trace, buf);
        }

        ///<playwright-file>chromium/tracing.spec.js</playwright-file>
        ///<playwright-describe>Chromium.startTracing</playwright-describe>
        ///<playwright-it>should work without options</playwright-it>
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldWorkWithoutOptions()
        {
            await Browser.StartTracingAsync();
            await Page.GoToAsync(TestConstants.ServerUrl + "/grid.html");
            var trace = await Browser.StopTracingAsync();
            Assert.NotNull(trace);
        }

        ///<playwright-file>chromium/tracing.spec.js</playwright-file>
        ///<playwright-describe>Chromium.startTracing</playwright-describe>
        ///<playwright-it>should return null in case of Buffer error</playwright-it>
        [Fact(Skip = "We don't need this test")]
        public void ShouldReturnNullInCaseOfBufferError() { }

        ///<playwright-file>chromium/tracing.spec.js</playwright-file>
        ///<playwright-describe>Chromium.startTracing</playwright-describe>
        ///<playwright-it>should support a buffer without a path</playwright-it>
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldSupportABufferWithoutAPath()
        {
            await Browser.StartTracingAsync(new TracingOptions
            {
                Screenshots = true
            });
            await Page.GoToAsync(TestConstants.ServerUrl + "/grid.html");
            var trace = await Browser.StopTracingAsync();
            Assert.Contains("screenshot", trace);
        }
    }
}
