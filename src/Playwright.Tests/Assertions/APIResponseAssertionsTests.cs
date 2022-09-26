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

using Microsoft.AspNetCore.Http;

namespace Microsoft.Playwright.Tests.Assertions;

public class APIResponseAssertionsTests : PageTestEx
{
    [SetUp]
    public void Setup()
    {
        Server.SetRoute("/text-content-type", async context =>
        {
            context.Response.StatusCode = 404;
            context.Response.Headers["Content-Type"] = "text/plain";
            await context.Response.WriteAsync("Text error");
        });
        Server.SetRoute("/no-content-type", async context =>
        {
            context.Response.StatusCode = 404;
            await context.Response.WriteAsync("no content type error");
        });
        Server.SetRoute("/binary-content-type", async context =>
        {
            context.Response.StatusCode = 404;
            context.Response.Headers["Content-Type"] = "image/bmp";
            await context.Response.WriteAsync("Image content type error");
        });
    }


    [PlaywrightTest("tests/page/expect-boolean.spec.ts", "toBeOK")]
    public async Task ToBeOK()
    {
        var response = await Page.APIRequest.GetAsync(Server.EmptyPage);
        await Expect(response).ToBeOKAsync();
    }

    [PlaywrightTest("tests/page/expect-boolean.spec.ts", "not.toBeOK")]
    public async Task NotToBeOK()
    {
        var response = await Page.APIRequest.GetAsync($"{Server.Prefix}/unkown");
        await Expect(response).Not.ToBeOKAsync();
    }

    [PlaywrightTest("tests/page/expect-boolean.spec.ts", "toBeOK fail with invalid argument")]
    public async Task ToBeFailWithInvalidArgument()
    {
        IAPIResponse response = null;
        await PlaywrightAssert.ThrowsAsync<ArgumentNullException>(() => Expect(response).ToBeOKAsync());
    }

    [PlaywrightTest("tests/page/expect-boolean.spec.ts", "toBeOK should print response with text content type when fails > text content type")]
    public async Task ToBeOKShouldPrintResponseWithTextContentTypeWhenFailsWithTextContentType()
    {
        var response = await Page.APIRequest.GetAsync($"{Server.Prefix}/text-content-type");
        var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => Expect(response).ToBeOKAsync());
        StringAssert.Contains($"→ GET {Server.Prefix}/text-content-type", exception.Message);
        StringAssert.Contains("← 404 Not Found", exception.Message);
        StringAssert.Contains("Text error", exception.Message);
    }

    [PlaywrightTest("tests/page/expect-boolean.spec.ts", "toBeOK should print response with text content type when fails > no content type")]
    public async Task ToBeOKShouldPrintResponseWithTextContentTypeWhenFailsWithNoContentType()
    {
        var response = await Page.APIRequest.GetAsync($"{Server.Prefix}/no-content-type");
        var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => Expect(response).ToBeOKAsync());
        StringAssert.Contains($"→ GET {Server.Prefix}/no-content-type", exception.Message);
        StringAssert.Contains("← 404 Not Found", exception.Message);
        StringAssert.DoesNotContain("No content type error", exception.Message);
    }

    [PlaywrightTest("tests/page/expect-boolean.spec.ts", "toBeOK should print response with text content type when fails > image content type")]
    public async Task ToBeOKShouldPrintResponseWithTextContentTypeWhenFailsWithImageContentType()
    {
        var response = await Page.APIRequest.GetAsync($"{Server.Prefix}/image-content-type");
        var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => Expect(response).ToBeOKAsync());
        StringAssert.Contains($"→ GET {Server.Prefix}/image-content-type", exception.Message);
        StringAssert.Contains("← 404 Not Found", exception.Message);
        StringAssert.DoesNotContain("Image content type error", exception.Message);
    }
}
