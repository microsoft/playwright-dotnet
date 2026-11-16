/**
 * Copyright (c) Microsoft Corporation. All rights reserved.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.Playwright.Tests.Fixtures;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Page;

/// <summary>Port of tests/page/network-post-data.spec.ts.</summary>
public class NetworkPostDataTests
{
    [UpstreamTest("NE-108 should return correct postData buffer for utf-8 body")]
    public async Task ShouldReturnCorrectPostDataBufferForUtf8Body(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        const string value = "baẞ";
        var (request, _) = await PromiseAll(
            page.WaitForRequestAsync("**"),
            page.EvaluateAsync(@"({ url, value }) => {
      const request = new Request(url, {
        method: 'POST',
        body: JSON.stringify(value),
      });
      request.headers.set('content-type', 'application/json;charset=UTF-8');
      return fetch(request);
    }", new { url = server.Prefix + "/title.html", value }));
        // .NET: JSON.stringify does not escape ẞ; JsonSerializer writes ẞ for it unless the encoder is the relaxed one.
        Assert.That(request.PostDataBuffer.SequenceEqual(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value, new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping }))), Is.EqualTo(true));
        ExpectToEqual(request.PostDataJSON(), value);
    }

    [UpstreamTest("NE-109 should return post data w/o content-type @smoke")]
    public async Task ShouldReturnPostDataWOContentType(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        var (request, _) = await PromiseAll(
            page.WaitForRequestAsync("**"),
            page.EvaluateAsync(@"({ url }) => {
      const request = new Request(url, {
        method: 'POST',
        body: JSON.stringify({ value: 42 }),
      });
      request.headers.set('content-type', '');
      return fetch(request);
    }", new { url = server.Prefix + "/title.html" }));
        ExpectToEqual(request.PostDataJSON(), new { value = 42 });
    }

    [UpstreamTest("NE-110 should throw on invalid JSON in post data")]
    [Fixme("dotnet: Request.PostDataJSON() throws the JsonException of JsonDocument.Parse, \"'<' is an invalid start of a value. LineNumber: 0 | BytePositionInLine: 0.\"; upstream expects 'POST data is not a valid JSON object: <not a json>'")]
    public async Task ShouldThrowOnInvalidJSONInPostData(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        var (request, _) = await PromiseAll(
            page.WaitForRequestAsync("**"),
            page.EvaluateAsync(@"({ url }) => {
      const request = new Request(url, {
        method: 'POST',
        body: '<not a json>',
      });
      return fetch(request);
    }", new { url = server.Prefix + "/title.html" }));
        Exception error = null;
        try
        {
            request.PostDataJSON();
        }
        catch (Exception e)
        {
            error = e;
        }
        Assert.That(error.Message, Does.Contain("POST data is not a valid JSON object: <not a json>"));
    }

    [UpstreamTest("NE-111 should return post data for PUT requests")]
    public async Task ShouldReturnPostDataForPUTRequests(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        var (request, _) = await PromiseAll(
            page.WaitForRequestAsync("**"),
            page.EvaluateAsync(@"({ url }) => {
      const request = new Request(url, {
        method: 'PUT',
        body: JSON.stringify({ value: 42 }),
      });
      return fetch(request);
    }", new { url = server.Prefix + "/title.html" }));
        ExpectToEqual(request.PostDataJSON(), new { value = 42 });
    }

    [UpstreamTest("NE-112 should get post data for file/blob")]
    public async Task ShouldGetPostDataForFileBlob(IPage page, Server server, string browserName)
    {
        It.Fail(browserName == "webkit" || browserName == "chromium");
        await page.GotoAsync(server.EmptyPage);
        var (request, _) = await PromiseAll(
            page.WaitForRequestAsync("**/*"),
            page.EvaluateAsync(@"() => {
      const file = new File(['file-contents'], 'filename.txt');

      void fetch('/data', {
        method: 'POST',
        headers: {
          'content-type': 'application/octet-stream'
        },
        body: file
      });
    }"));
        Assert.That(request.PostData, Is.EqualTo("file-contents"));
    }

    [UpstreamTest("NE-113 should get post data for navigator.sendBeacon api calls")]
    [Fixme("dotnet: Request.PostDataJSON() throws a NullReferenceException for a request without a content-type header: it calls Contains() on the missing value; upstream reads it with contentType?.includes()", Browsers = "firefox")]
    public async Task ShouldGetPostDataForNavigatorSendBeaconApiCalls(IPage page, Server server, string browserName)
    {
        It.Fail(browserName == "chromium", "postData is empty");
        It.Fail(browserName == "webkit", "postData is empty");
        await page.GotoAsync(server.EmptyPage);
        var (request, _) = await PromiseAll(
            page.WaitForRequestAsync("**/*"),
            page.EvaluateAsync("() => navigator.sendBeacon(window.location.origin + '/api/foo', new Blob([JSON.stringify({ foo: 'bar' })]))"));
        Assert.That(request.Method, Is.EqualTo("POST"));
        Assert.That(request.Url, Is.EqualTo(server.Prefix + "/api/foo"));
        ExpectToEqual(request.PostDataJSON(), new { foo = "bar" });
    }
}
