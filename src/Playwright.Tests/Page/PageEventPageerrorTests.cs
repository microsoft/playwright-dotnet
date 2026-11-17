/**
 * Copyright 2017 Google Inc. All rights reserved.
 * Modifications copyright (c) Microsoft Corporation.
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

using System.Globalization;
using Microsoft.Playwright.Tests.Fixtures;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Page;

/// <summary>Port of tests/page/page-event-pageerror.spec.ts.</summary>
public class PageEventPageerrorTests
{
    [UpstreamTest("ER-001 should fire")]
    [LocalFixme("dotnet: the page error is a string, the stack of the error, which starts with 'Error: Fancy error!\\n    at c (': upstream expects error.message 'Fancy error!'")]
    public async Task ShouldFire(IPage page, Server server, string browserName, int browserMajorVersion, bool isBidi)
    {
        var url = server.Prefix + "/error.html";
        var (error, _) = await PromiseAll(
            Once<string>(h => page.PageError += h, h => page.PageError -= h),
            page.GotoAsync(url));
        // .NET: expect(error.name).toBe('Error') is left out: the page error is a string, error.name is not in the API.
        // .NET: error.message and error.stack are the page error itself, a string.
        Assert.That(error, Is.EqualTo("Fancy error!"));
        if (browserName == "chromium" || (browserName == "firefox" && isBidi))
        {
            Assert.That(error, Is.EqualTo(@"Error: Fancy error!
    at c (myscript.js:14:11)
    at b (myscript.js:10:5)
    at a (myscript.js:6:5)
    at myscript.js:3:1"));
        }
        else if (browserName == "webkit")
        {
            Assert.That(error, Is.EqualTo($@"Error: Fancy error!
    at c ({url}:14:36)
    at b ({url}:10:6)
    at a ({url}:6:6)
    at global code ({url}:3:2)"));
        }
        else if (browserName == "firefox")
        {
            Assert.That(error, Is.EqualTo(@"Error: Fancy error!
    at c (myscript.js:14:11)
    at b (myscript.js:10:5)
    at a (myscript.js:6:5)
    at  (myscript.js:3:1)"));
        }

        // The same script from another origin: Chromium reports it without an exception object,
        // so the stack is built from the CDP call frames, which are zero-based.
        var crossOriginUrl = server.CrossProcessPrefix + "/error.js";
        server.SetRoute("/error.js", (req, res) =>
        {
            res.SetHeader("Content-Type", "text/javascript");
            // Same lines as error.html, so the stack has the same locations.
            res.End(@"
console.error('Not a JS error');
a();

function a() {
    b();
}

function b() {
    c();
}

function c() {
    throw new Error('Fancy error!');
}
");
        });
        server.SetRoute("/cross-origin-error.html", (req, res) =>
        {
            res.SetHeader("Content-Type", "text/html");
            res.End($"<script src=\"{crossOriginUrl}\"></script>");
        });
        var (crossOriginError, _) = await PromiseAll(
            Once<string>(h => page.PageError += h, h => page.PageError -= h),
            page.GotoAsync(server.Prefix + "/cross-origin-error.html"));
        Assert.That(crossOriginError, Is.EqualTo("Fancy error!"));
        if (browserName == "chromium" && browserMajorVersion >= 155)
        {
            Assert.That(crossOriginError, Is.EqualTo($@"Uncaught Error: Fancy error!
    at c ({crossOriginUrl}:14:11)
    at b ({crossOriginUrl}:10:5)
    at a ({crossOriginUrl}:6:5)
    at <anonymous> ({crossOriginUrl}:3:1)"));
        }
        else if (browserName == "webkit")
        {
            Assert.That(crossOriginError, Is.EqualTo($@"Error: Fancy error!
    at c ({crossOriginUrl}:14:36)
    at b ({crossOriginUrl}:10:6)
    at a ({crossOriginUrl}:6:6)
    at global code ({crossOriginUrl}:3:2)"));
        }
        else if (browserName == "firefox" && !isBidi)
        {
            Assert.That(crossOriginError, Is.EqualTo($@"Error: Fancy error!
    at c ({crossOriginUrl}:14:11)
    at b ({crossOriginUrl}:10:5)
    at a ({crossOriginUrl}:6:5)
    at  ({crossOriginUrl}:3:1)"));
        }
    }

    [UpstreamTest("ER-002 should not receive console message for pageError")]
    public async Task ShouldNotReceiveConsoleMessageForPageError(IPage page, Server server)
    {
        var messages = new List<IConsoleMessage>();
        page.Console += (_, e) => messages.Add(e);
        await PromiseAll(
            Once<string>(h => page.PageError += h, h => page.PageError -= h),
            page.GotoAsync(server.Prefix + "/error.html"));
        Assert.That(messages.Count, Is.EqualTo(1));
    }

    [UpstreamTest("ER-003 should contain sourceURL")]
    public async Task ShouldContainSourceURL(IPage page, Server server, string browserName)
    {
        Info.Fixme(browserName == "webkit");

        var (error, _) = await PromiseAll(
            Once<string>(h => page.PageError += h, h => page.PageError -= h),
            page.GotoAsync(server.Prefix + "/error.html"));
        // .NET: error.stack is the page error itself, a string.
        Assert.That(error, Does.Contain("myscript.js"));
    }

    [UpstreamTest("ER-004 should contain the Error.name property")]
    [LocalFixme("dotnet: the page error is a string, the stack of the error, which starts with 'my-name: my-message\\n    at ': upstream expects error.message 'my-message'")]
    public async Task ShouldContainTheErrorNameProperty(IPage page)
    {
        var (error, _) = await PromiseAll(
            Once<string>(h => page.PageError += h, h => page.PageError -= h),
            page.EvaluateAsync(@"() => {
      window.builtins.setTimeout(() => {
        const error = new Error('my-message');
        error.name = 'my-name';
        throw error;
      }, 0);
    }"));
        // .NET: expect(error.name).toBe('my-name') is left out: the page error is a string, error.name is not in the API.
        // .NET: error.message is the page error itself, a string.
        Assert.That(error, Is.EqualTo("my-message"));
    }

    [UpstreamTest("ER-005 should support an empty Error.name property")]
    [LocalFixme("dotnet: the page error is a string, the stack of the error, which starts with 'Error: my-message\\n    at eval (': upstream expects error.message 'my-message' (only in chromium); dotnet: the page error is a string, the stack of the error, which starts with ': my-message\\n    at  (': upstream expects error.message 'my-message' (only in firefox); dotnet: the page error is a string, the stack of the error, 'my-message\\n    at unknown (undefined:3:32)': upstream expects error.message 'my-message' (only in webkit)")]
    public async Task ShouldSupportAnEmptyErrorNameProperty(IPage page)
    {
        var (error, _) = await PromiseAll(
            Once<string>(h => page.PageError += h, h => page.PageError -= h),
            page.EvaluateAsync(@"() => {
      window.builtins.setTimeout(() => {
        const error = new Error('my-message');
        error.name = '';
        throw error;
      }, 0);
    }"));
        // .NET: expect(error.name).toBe('') is left out: the page error is a string, error.name is not in the API.
        // .NET: error.message is the page error itself, a string.
        Assert.That(error, Is.EqualTo("my-message"));
    }

    [UpstreamTest("ER-006 should handle odd values")]
    [LocalFixme("dotnet: the page error is a string, ': null' for a value without a stack (name: message, with an empty name): upstream expects error.message 'null' (only in chromium); dotnet: the page error is a string, the stack of the error, which starts with 'uncaught exception: null\\n    at  (': upstream expects error.message 'null' (only in firefox); dotnet: the page error is a string, the stack of the error, 'null\\n    at unknown (:2:55)': upstream expects error.message 'null' (only in webkit)")]
    public async Task ShouldHandleOddValues(IPage page)
    {
        // .NET: the case [undefined, 'undefined'] is left out: a null argument of EvaluateAsync is null in the page, and .NET has no undefined.
        var cases = new (object, string)[]
        {
            (null, "null"),
            (0, "0"),
            ("", ""),
        };
        foreach (var (value, message) in cases)
        {
            var (error, _) = await PromiseAll(
                Once<string>(h => page.PageError += h, h => page.PageError -= h),
                page.EvaluateAsync(@"value => {
        window.builtins.setTimeout(() => { throw value; }, 0);
      }", value));
            // .NET: error.message is the page error itself, a string.
            Assert.That(error, Is.EqualTo(message));
        }
    }

    [UpstreamTest("ER-007 should handle object")]
    [LocalFixme("dotnet: the page error is a string, ': Object' for a value without a stack (name: message, with an empty name): upstream expects error.message 'Object' (only in chromium); dotnet: the page error is a string, the stack of the error, which starts with 'uncaught exception: [object Object]\\n    at  (': upstream expects error.message '[object Object]' (only in firefox); dotnet: the page error is a string, the stack of the error, '[object Object]\\n    at unknown (:2:50)': upstream expects error.message '[object Object]' (only in webkit)")]
    public async Task ShouldHandleObject(IPage page, string browserName)
    {
        var (error, _) = await PromiseAll(
            Once<string>(h => page.PageError += h, h => page.PageError -= h),
            page.EvaluateAsync(@"() => {
      window.builtins.setTimeout(() => { throw {}; }, 0);
    }"));
        // .NET: error.message is the page error itself, a string.
        Assert.That(error, Is.EqualTo(browserName == "chromium" ? "Object" : "[object Object]"));
    }

    [UpstreamTest("ER-008 should handle window")]
    [LocalFixme("dotnet: the page error is a string, ': Window' for a value without a stack (name: message, with an empty name): upstream expects error.message 'Window' (only in chromium); dotnet: the page error is a string, the stack of the error, which starts with 'uncaught exception: [object Window]\\n    at  (': upstream expects error.message '[object Window]' (only in firefox); dotnet: the page error is a string, the stack of the error, '[object Window]\\n    at unknown (:2:54)': upstream expects error.message '[object Window]' (only in webkit)")]
    public async Task ShouldHandleWindow(IPage page, string browserName)
    {
        var (error, _) = await PromiseAll(
            Once<string>(h => page.PageError += h, h => page.PageError -= h),
            page.EvaluateAsync(@"() => {
      window.builtins.setTimeout(() => { throw window; }, 0);
    }"));
        // .NET: error.message is the page error itself, a string.
        Assert.That(error, Is.EqualTo(browserName == "chromium" ? "Window" : "[object Window]"));
    }

    [UpstreamTest("ER-009 should remove a listener of a non-existing event handler")]
    public async Task ShouldRemoveAListenerOfANonExistingEventHandler(IPage page)
    {
        page.PageError -= (_, _) => { };
    }

    [UpstreamTest("ER-010 should emit error from unhandled rejects")]
    public async Task ShouldEmitErrorFromUnhandledRejects(IPage page, string browserName)
    {
        var error = await PromiseAll(
            Once<string>(h => page.PageError += h, h => page.PageError -= h),
            page.SetContentAsync(@"
        <script>
          Promise.reject(new Error('sad :('));
        </script>
    "));
        // .NET: error.message is the page error itself, a string.
        Assert.That(error, Does.Contain("sad :("));
    }

    [UpstreamTest("ER-011 pageErrors should work")]
    [LocalFixme("dotnet: a page error is a string, the stack of the error, which starts with 'Error: error201\\n    at ': upstream expects error.message 'error201'")]
    public async Task PageErrorsShouldWork(IPage page)
    {
        await page.EvaluateAsync(@"async () => {
    for (let i = 0; i < 301; i++)
      window.builtins.setTimeout(() => { throw new Error('error' + i); }, 0);
    await new Promise(f => window.builtins.setTimeout(f, 2000));
  }");

        var errors = await page.PageErrorsAsync();
        // .NET: error.message is the page error itself, a string.
        var messages = errors.Select(e => e).ToArray();

        var expected = new List<string>();
        for (var i = 201; i < 301; i++)
        {
            expected.Add("error" + i.ToString(CultureInfo.InvariantCulture));
        }

        Assert.That(messages.Length, Is.GreaterThanOrEqualTo(100), "should be at least 100 errors");
        Assert.That(messages[(messages.Length - expected.Count)..], Is.EqualTo(expected), "should return last errors");
    }

    [UpstreamTest("ER-012 clearPageErrors should work")]
    [LocalFixme("dotnet: a page error is a string, the stack of the error, which starts with 'Error: error1\\n    at ': upstream expects error.message 'error1'")]
    public async Task ClearPageErrorsShouldWork(IPage page)
    {
        await page.EvaluateAsync(@"() => {
    window.builtins.setTimeout(() => { throw new Error('error1'); }, 0);
    window.builtins.setTimeout(() => { throw new Error('error2'); }, 0);
  }");
        await page.WaitForTimeoutAsync(1000);

        var errors = await page.PageErrorsAsync();
        // .NET: error.message is the page error itself, a string.
        Assert.That(errors.Select(e => e), Does.Contain("error1"));
        Assert.That(errors.Select(e => e), Does.Contain("error2"));

        await page.ClearPageErrorsAsync();

        errors = await page.PageErrorsAsync();
        Assert.That(errors, Is.EqualTo(Array.Empty<string>()));

        await page.EvaluateAsync(@"() => {
    window.builtins.setTimeout(() => { throw new Error('error3'); }, 0);
  }");
        await page.WaitForTimeoutAsync(1000);

        errors = await page.PageErrorsAsync();
        Assert.That(errors.Count, Is.EqualTo(1));
        Assert.That(errors[0], Does.Contain("error3"));
    }

    [UpstreamTest("ER-013 should fire illegal character error")]
    public async Task ShouldFireIllegalCharacterError(IPage page, Server server, string browserName, bool isWindows, string channel)
    {
        server.SetRoute("/error.html", (req, res) =>
        {
            res.End(@"
      <!doctype html>
      <html lang=""en"">
        <head>
          <meta charset=""UTF-8"" />
          <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"" />
          <title>vite-project</title>
        </head>
        <body>
          <div id=""app""></div>
          <script >
            let a=10；//   !!!note: ； not ;
          </script>
        </body>
      </html>
    ");
        });
        var url = server.Prefix + "/error.html";
        var (error, _) = await PromiseAll(
            Once<string>(h => page.PageError += h, h => page.PageError -= h),
            page.GotoAsync(url));
        // .NET: error.message is the page error itself, a string.
        if (browserName == "chromium")
        {
            Assert.That(error, Does.Contain("Invalid or unexpected token"));
        }
        else if (browserName == "webkit" && isWindows && channel != "webkit-wsl")
        {
            Assert.That(error, Does.Contain("No identifiers allowed directly after numeric literal"));
        }
        else if (browserName == "webkit")
        {
            Assert.That(error, Does.Contain("Invalid character"));
        }
        else
        {
            Assert.That(error, Does.Contain("illegal character"));
        }
    }

    [UpstreamTest("ER-022 should not fire for a caught fetch rejected by a pending navigation")]
    [LocalFixme("dotnet: the page error is a string, the stack of the error, which starts with 'Error: genuine error\\n    at ': upstream expects error.message 'genuine error'")]
    public async Task ShouldNotFireForACaughtFetchRejectedByAPendingNavigation(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        var errors = new List<string>();
        // .NET: error.message is the page error itself, a string.
        page.PageError += (_, error) => errors.Add(error);
        var waitResponse = new TaskCompletionSource<ServerResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        server.SetRoute("/wait", (req, res) => waitResponse.TrySetResult(res));
        var replacementResponse = new TaskCompletionSource<ServerResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        server.SetRoute("/replacement", (req, res) => replacementResponse.TrySetResult(res));
        var fetchDone = page.WaitForConsoleMessageAsync(new() { Predicate = message => message.Text == "fetch done" });
        // Chromium does not run evaluate during a pending navigation, so the page starts the second fetch.
        await page.EvaluateAsync(@"() => {
    void fetch('/wait').catch(() => {}).then(() => fetch('/empty.html')).catch(() => {}).then(() => console.log('fetch done'));
  }");
        var navigation = page.GotoAsync(server.Prefix + "/replacement");
        var response = await replacementResponse.Task;
        // WebKit rejects the second fetch, because the document is about to go away.
        (await waitResponse.Task).End();
        await fetchDone;
        response.End("replacement");
        await navigation;
        var (error, _) = await PromiseAll(
            Once<string>(h => page.PageError += h, h => page.PageError -= h),
            page.EvaluateAsync("() => setTimeout(() => { throw new Error('genuine error'); }, 0)"));
        Assert.That(error, Is.EqualTo("genuine error"));
        Assert.That(errors, Is.EqualTo(new[] { "genuine error" }));
    }
}
