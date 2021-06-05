using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;


namespace Microsoft.Playwright.Tests
{
    /// <playwright-file>network-post-data.spec.ts</playwright-file>
    public sealed class NetworkPostDataTests : PageTestEx
    {
        /// <playwright-file>network-post-data.spec.ts</playwright-file>
        /// <playwright-it>should return correct postData buffer for utf-8 body</playwright-it>
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldReturnCorrectPostdataBufferForUtf8Body()
        {
            await Page.GotoAsync(Server.EmptyPage);
            string value = "baẞ";

            var task = Page.WaitForRequestAsync("**/*");
            var actualTask = Page.EvaluateAsync(@$"() => {{
                      const request = new Request('{Server.Prefix + "/title.html"}', {{
                        method: 'POST',
                        body: JSON.stringify('{value}'),
                      }});
                      request.headers.set('content-type', 'application/json;charset=UTF-8');
                      return fetch(request);
                    }}");

            await Task.WhenAll(task, actualTask);

            string expectedJsonValue = JsonSerializer.Serialize(value, new()
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                WriteIndented = true
            });

            var request = task.Result;
            Assert.AreEqual(expectedJsonValue, request.PostData);
            Assert.AreEqual(value, request.PostDataJSON()?.GetString());
        }

        /// <playwright-file>network-post-data.spec.ts</playwright-file>
        /// <playwright-it>should return post data w/o content-type</playwright-it>
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldReturnPostDataWOContentType()
        {
            await Page.GotoAsync(Server.EmptyPage);

            var task = Page.WaitForRequestAsync("**/*");
            var actualTask = Page.EvaluateAsync(@"(url) => {
                      const request = new Request(url, {
                        method: 'POST',
                        body: JSON.stringify({ value: 42 }),
                      });
                      request.headers.set('content-type', '');
                      return fetch(request);
                    }", Server.Prefix + "/title.html");

            await Task.WhenAll(task, actualTask);

            var request = task.Result;
            Assert.AreEqual(42, request.PostDataJSON()?.GetProperty("value").GetInt32());
        }

        /// <playwright-file>network-post-data.spec.ts</playwright-file>
        /// <playwright-it>should throw on invalid JSON in post data</playwright-it>
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldThrowOnInvalidJSONInPostData()
        {
            await Page.GotoAsync(Server.EmptyPage);

            var task = Page.WaitForRequestAsync("**/*");
            var actualTask = Page.EvaluateAsync(@"(url) => {
                      const request = new Request(url, {
                        method: 'POST',
                        body: '<not a json>',
                      });
                      return fetch(request);
                    }", Server.Prefix + "/title.html");

            await Task.WhenAll(task, actualTask);

            var request = task.Result;
            Assert.That(() => request.PostDataJSON(), Throws.Exception);
        }

        /// <playwright-file>network-post-data.spec.ts</playwright-file>
        /// <playwright-it>should return post data for PUT requests</playwright-it>
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldReturnPostDataForPUTRequests()
        {
            await Page.GotoAsync(Server.EmptyPage);

            var task = Page.WaitForRequestAsync("**/*");
            var actualTask = Page.EvaluateAsync(@"(url) => {
                      const request = new Request(url, {
                        method: 'PUT',
                        body: JSON.stringify({ value: 42 }),
                      });
                      return fetch(request);
                    }", Server.Prefix + "/title.html");

            await Task.WhenAll(task, actualTask);

            var request = task.Result;
            Assert.AreEqual(42, request.PostDataJSON()?.GetProperty("value").GetInt32());
        }
    }
}
