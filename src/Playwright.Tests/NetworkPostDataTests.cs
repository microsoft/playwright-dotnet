/*
 * MIT License
 *
 * Copyright (c) Microsoft Corporation.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

using System.Text.Encodings.Web;
using System.Text.Json;


namespace Microsoft.Playwright.Tests;

/// <playwright-file>network-post-data.spec.ts</playwright-file>
public sealed class NetworkPostDataTests : PageTestEx
{
    /// <playwright-file>network-post-data.spec.ts</playwright-file>
    /// <playwright-it>should return correct postData buffer for utf-8 body</playwright-it>
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

        string expectedJsonValue = JsonSerializer.Serialize(value, new JsonSerializerOptions()
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
