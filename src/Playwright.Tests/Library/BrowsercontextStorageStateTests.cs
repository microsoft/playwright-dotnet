/**
 * Copyright 2018 Google Inc. All rights reserved.
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

using System.Text.Json;
using Microsoft.Playwright.Tests.Fixtures;
using static Microsoft.Playwright.Assertions;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Library;

/// <summary>Port of tests/library/browsercontext-storage-state.spec.ts.</summary>
public class BrowsercontextStorageStateTests : UpstreamBrowserTest
{
    [UpstreamTest("ST-001 should capture local storage")]
    public async Task ShouldCaptureLocalStorage()
    {
        var context = await ContextFactory();
        var page1 = await context.NewPageAsync();
        await page1.RouteAsync("**/*", route =>
        {
            IgnoreErrors(route.FulfillAsync(new() { Body = "<html></html>" }));
        });
        await page1.GotoAsync("https://www.example.com");
        await page1.EvaluateAsync(@"() => {
    localStorage['name1'] = 'value1';
  }");
        await page1.GotoAsync("https://www.domain.com");
        await page1.EvaluateAsync(@"() => {
    localStorage['name2'] = 'value2';
  }");
        // .NET: a storage state is a JSON string.
        var origins = JsonSerializer.Deserialize<JsonElement>(await context.StorageStateAsync()).GetProperty("origins");
        ExpectToEqual(origins, new[]
        {
            new
            {
                origin = "https://www.domain.com",
                localStorage = new[]
                {
                    new
                    {
                        name = "name2",
                        value = "value2",
                    },
                },
            },
            new
            {
                origin = "https://www.example.com",
                localStorage = new[]
                {
                    new
                    {
                        name = "name1",
                        value = "value1",
                    },
                },
            },
        });
    }

    [UpstreamTest("ST-002 should set local storage")]
    [LocalFixme("dotnet: SetStorageStateAsync takes the path of a file only. A storage state, which is a JSON string in .NET, fails with 'The specified storage state file does not exist: {\"cookies\":[],...'")]
    public async Task ShouldSetLocalStorage()
    {
        Info.Fixme(Channel?.StartsWith("msedge", StringComparison.Ordinal) == true, "Network.clearBrowserCache sometimes stalls");

        var context = await ContextFactory(new()
        {
            // .NET: a storage state is a JSON string.
            StorageState = JsonStringify(new
            {
                cookies = Array.Empty<object>(),
                origins = new[]
                {
                    new
                    {
                        origin = "https://www.example.com",
                        localStorage = new[]
                        {
                            new
                            {
                                name = "name1",
                                value = "value1",
                            },
                        },
                    },
                },
            }),
        });
        var page = await context.NewPageAsync();
        await page.RouteAsync("**/*", route =>
        {
            IgnoreErrors(route.FulfillAsync(new() { Body = "<html></html>" }));
        });
        await page.GotoAsync("https://www.example.com");
        var localStorage = await page.EvaluateAsync<object>("window.localStorage");
        ExpectToEqual(localStorage, new { name1 = "value1" });

        // Now use setStorageState to replace the storage
        // .NET: a storage state is a JSON string.
        await context.SetStorageStateAsync(JsonStringify(new
        {
            cookies = Array.Empty<object>(),
            origins = new[]
            {
                new
                {
                    origin = "https://www.example.com",
                    localStorage = new[]
                    {
                        new
                        {
                            name = "name2",
                            value = "value2",
                        },
                    },
                },
            },
        }));
        Assert.That(context.Pages, Has.Count.EqualTo(1));
        await page.GotoAsync("https://www.example.com");
        var localStorage2 = await page.EvaluateAsync<object>("window.localStorage");
        ExpectToEqual(localStorage2, new { name2 = "value2" });

        await context.CloseAsync();
    }

    [UpstreamTest("ST-003 should report good error if the url is not valid")]
    [LocalFixme("dotnet: the message is 'Error setting storage state:\\n...', without the 'browser.newContext: ' prefix of the call")]
    public async Task ShouldReportGoodErrorIfTheUrlIsNotValid()
    {
        var error = await Catch(() => ContextFactory(new()
        {
            // .NET: a storage state is a JSON string.
            StorageState = JsonStringify(new
            {
                cookies = Array.Empty<object>(),
                origins = new[]
                {
                    new
                    {
                        origin = "foo",
                        localStorage = new[]
                        {
                            new
                            {
                                name = "name1",
                                value = "value1",
                            },
                        },
                    },
                },
            }),
        }));
        Assert.That(error.Message, Does.Contain("browser.newContext: Error setting storage state:"));
        Assert.That(error.Message, Does.Contain("foo"));
    }

    [UpstreamTest("ST-004 should round-trip through the file")]
    public async Task ShouldRoundTripThroughTheFile()
    {
        Info.Fixme(Channel?.StartsWith("msedge", StringComparison.Ordinal) == true, "Network.clearBrowserCache sometimes stalls");

        var context = await ContextFactory();
        var page1 = await context.NewPageAsync();
        await page1.RouteAsync("**/*", route =>
        {
            IgnoreErrors(route.FulfillAsync(new() { Body = "<html></html>" }));
        });
        await page1.GotoAsync("https://www.example.com");
        await page1.EvaluateAsync(@"async () => {
    localStorage['name1'] = 'value1';
    document.cookie = 'username=John Doe';

    await new Promise((resolve, reject) => {
      const openRequest = indexedDB.open('db', 42);
      openRequest.onupgradeneeded = () => {
        openRequest.result.createObjectStore('store', { keyPath: 'name' });
        openRequest.result.createObjectStore('store2');
      };
      openRequest.onsuccess = () => {
        const transaction = openRequest.result.transaction(['store', 'store2'], 'readwrite');
        transaction
            .objectStore('store')
            .put({ name: 'foo', date: new Date(0), null: null });
        transaction
            .objectStore('store2')
            .put(new TextEncoder().encode('bar'), 'foo');
        transaction.addEventListener('complete', resolve);
        transaction.addEventListener('error', reject);
      };
    });

    return document.cookie;
  }");

        var path = Info.OutputPath("storage-state.json");
        var state = await context.StorageStateAsync(new() { Path = path, IndexedDB = true });
        var written = await File.ReadAllTextAsync(path);
        // .NET: a storage state is a JSON string, JSON.stringify(state, undefined, 2) is the state.
        Assert.That(state, Is.EqualTo(written));

        var checkContext = async (IBrowserContext context) =>
        {
            var page = await context.NewPageAsync();
            await page.RouteAsync("**/*", route =>
            {
                IgnoreErrors(route.FulfillAsync(new() { Body = "<html></html>" }));
            });
            await page.GotoAsync("https://www.example.com");
            var localStorage = await page.EvaluateAsync<object>("window.localStorage");
            ExpectToEqual(localStorage, new { name1 = "value1" });
            var cookie = await page.EvaluateAsync<string>("document.cookie");
            Assert.That(cookie, Is.EqualTo("username=John Doe"));
            var idbValues = await page.EvaluateAsync<object>(@"() => new Promise((resolve, reject) => {
      const openRequest = indexedDB.open('db', 42);
      openRequest.addEventListener('success', async () => {
        const db = openRequest.result;
        const transaction = db.transaction(['store', 'store2'], 'readonly');
        const request1 = transaction.objectStore('store').get('foo');
        const request2 = transaction.objectStore('store2').get('foo');

        const [result1, result2] = await Promise.all([request1, request2].map(request => new Promise((resolve, reject) => {
          request.addEventListener('success', () => resolve(request.result));
          request.addEventListener('error', () => reject(request.error));
        })));

        resolve([result1, new TextDecoder().decode(result2)]);
      });
      openRequest.addEventListener('error', () => reject(openRequest.error));
    })");
            ExpectToEqual(idbValues, new object[]
            {
                new Dictionary<string, object> { ["name"] = "foo", ["date"] = DateTime.UnixEpoch.AddMilliseconds(0), ["null"] = null },
                "bar",
            });
        };

        var context2 = await ContextFactory(new() { StorageStatePath = path });
        await checkContext(context2);
        await context2.CloseAsync();

        var context3 = await ContextFactory();
        await context3.SetStorageStateAsync(path);
        Assert.That(context3.Pages, Has.Count.EqualTo(0));
        await checkContext(context3);
        await context3.CloseAsync();
    }

    [UpstreamTest("ST-005 should round-trip OPFS")]
    [LocalFixme("dotnet: SetStorageStateAsync takes the path of a file only. A storage state, which is a JSON string in .NET, fails with 'The specified storage state file does not exist: {\"cookies\":[],...'")]
    public async Task ShouldRoundTripOPFS()
    {
        var page = await PageFixture();
        Info.Skip(BrowserName == "webkit", "OPFS is unavailable in non-persistent WebKit contexts");

        await page.GotoAsync(Server.EmptyPage);
        await page.EvaluateAsync(@"async () => {
    const root = await navigator.storage.getDirectory();
    const nested = await root.getDirectoryHandle('nested', { create: true });
    await nested.getDirectoryHandle('empty', { create: true });

    const binary = await nested.getFileHandle('data.bin', { create: true });
    const binaryWritable = await binary.createWritable();
    await binaryWritable.write(new Uint8Array([0, 1, 2, 255]));
    await binaryWritable.close();

    const text = await root.getFileHandle('hello.txt', { create: true });
    const textWritable = await text.createWritable();
    await textWritable.write('Hello, world!');
    await textWritable.close();
  }");

        // .NET: a storage state is a JSON string.
        ExpectToEqual(JsonSerializer.Deserialize<JsonElement>(await page.Context.StorageStateAsync()), new { cookies = Array.Empty<object>(), origins = Array.Empty<object>() });

        var path = Info.OutputPath("storage-state.json");
        var storageState = await page.Context.StorageStateAsync(new() { Path = path, Opfs = true });
        ExpectToEqual(JsonSerializer.Deserialize<JsonElement>(storageState).GetProperty("origins"), new[]
        {
            new
            {
                origin = Server.Prefix,
                localStorage = Array.Empty<object>(),
                opfs = new object[]
                {
                    new { path = "hello.txt", type = "file", base64 = "SGVsbG8sIHdvcmxkIQ==" },
                    new { path = "nested", type = "directory" },
                    new { path = "nested/data.bin", type = "file", base64 = "AAEC/w==" },
                    new { path = "nested/empty", type = "directory" },
                },
            },
        });
        ExpectToEqual(JsonSerializer.Deserialize<JsonElement>(await File.ReadAllTextAsync(path)), JsonSerializer.Deserialize<JsonElement>(storageState));
        ExpectToEqual(JsonSerializer.Deserialize<JsonElement>(await page.Context.APIRequest.StorageStateAsync(new() { Opfs = true })), JsonSerializer.Deserialize<JsonElement>(storageState));

        var checkContext = async (IBrowserContext context) =>
        {
            ExpectToEqual(JsonSerializer.Deserialize<JsonElement>(await context.StorageStateAsync(new() { Opfs = true })), JsonSerializer.Deserialize<JsonElement>(storageState));
            var checkPage = await context.NewPageAsync();
            await checkPage.GotoAsync(Server.EmptyPage);
            ExpectToEqual(await checkPage.EvaluateAsync<object>(@"async () => {
      const root = await navigator.storage.getDirectory();
      const hello = await (await root.getFileHandle('hello.txt')).getFile();
      const nested = await root.getDirectoryHandle('nested');
      const data = await (await nested.getFileHandle('data.bin')).getFile();
      const empty = await nested.getDirectoryHandle('empty');
      const emptyEntries = [];
      for await (const name of empty.keys())
        emptyEntries.push(name);
      return {
        text: await hello.text(),
        bytes: [...new Uint8Array(await data.arrayBuffer())],
        empty: emptyEntries,
      };
    }"), new
            {
                text = "Hello, world!",
                bytes = new[] { 0, 1, 2, 255 },
                empty = Array.Empty<object>(),
            });
        };

        var context2 = await ContextFactory(new() { StorageStatePath = path });
        await checkContext(context2);
        await context2.CloseAsync();

        var context3 = await ContextFactory();
        var page3 = await context3.NewPageAsync();
        await page3.GotoAsync(Server.EmptyPage);
        await page3.EvaluateAsync(@"async () => {
    const root = await navigator.storage.getDirectory();
    await root.getFileHandle('stale.txt', { create: true });
  }");
        await context3.SetStorageStateAsync(storageState);
        await checkContext(context3);
        await context3.CloseAsync();
    }

    [UpstreamTest("ST-006 should capture cookies")]
    public async Task ShouldCaptureCookies()
    {
        var context = await ContextFixture();
        var page = await PageFixture();
        Server.SetRoute("/setcookie.html", (req, res) =>
        {
            res.SetHeader("Set-Cookie", new[] { "a=b", "empty=" });
            res.End();
        });

        await page.GotoAsync(Server.Prefix + "/setcookie.html");
        Assert.That(await page.EvaluateAsync<string[]>(@"() => {
    const cookies = document.cookie.split(';');
    return cookies.map(cookie => cookie.trim()).sort();
  }"), Is.EqualTo(new[]
        {
            "a=b",
            "empty=",
        }));

        var storageState = await context.StorageStateAsync();
        ExpectToEqual(new HashSet<JsonElement>(JsonSerializer.Deserialize<JsonElement>(storageState).GetProperty("cookies").EnumerateArray()), new HashSet<object>
        {
            ObjectContaining(new
            {
                name = "a",
                value = "b",
            }),
            ObjectContaining(new
            {
                name = "empty",
                value = "",
            }),
        });
        var context2 = await ContextFactory(new() { StorageState = storageState });
        var page2 = await context2.NewPageAsync();
        await page2.GotoAsync(Server.EmptyPage);
        Assert.That(await page2.EvaluateAsync<string[]>(@"() => {
    const cookies = document.cookie.split(';');
    return cookies.map(cookie => cookie.trim()).sort();
  }"), Is.EqualTo(new[]
        {
            "a=b",
            "empty=",
        }));
    }

    [UpstreamTest("ST-007 should not emit events about internal page")]
    public async Task ShouldNotEmitEventsAboutInternalPage()
    {
        var context = await ContextFactory();
        var page = await context.NewPageAsync();
        await page.RouteAsync("**/*", route =>
        {
            IgnoreErrors(route.FulfillAsync(new() { Body = "<html></html>" }));
        });
        await page.GotoAsync("https://www.example.com");
        await page.EvaluateAsync("() => localStorage['name1'] = 'value1'");
        await page.GotoAsync("https://www.domain.com");
        await page.EvaluateAsync("() => localStorage['name2'] = 'value2'");

        var events = new List<object>();
        context.Page += (_, e) => events.Add(e);
        context.Request += (_, e) => events.Add(e);
        context.RequestFailed += (_, e) => events.Add(e);
        context.RequestFinished += (_, e) => events.Add(e);
        context.Response += (_, e) => events.Add(e);
        await context.StorageStateAsync();
        Assert.That(events, Has.Count.EqualTo(0));
    }

    [UpstreamTest("ST-008 should not restore localStorage twice")]
    public async Task ShouldNotRestoreLocalStorageTwice()
    {
        var context = await ContextFactory(new()
        {
            // .NET: a storage state is a JSON string.
            StorageState = JsonStringify(new
            {
                cookies = Array.Empty<object>(),
                origins = new[]
                {
                    new
                    {
                        origin = "https://www.example.com",
                        localStorage = new[]
                        {
                            new
                            {
                                name = "name1",
                                value = "value1",
                            },
                        },
                    },
                },
            }),
        });
        var page = await context.NewPageAsync();
        await page.RouteAsync("**/*", route =>
        {
            IgnoreErrors(route.FulfillAsync(new() { Body = "<html></html>" }));
        });
        await page.GotoAsync("https://www.example.com");
        var localStorage1 = await page.EvaluateAsync<object>("window.localStorage");
        ExpectToEqual(localStorage1, new { name1 = "value1" });

        await page.EvaluateAsync("() => window.localStorage['name1'] = 'value2'");

        await page.GotoAsync("https://www.example.com");
        var localStorage2 = await page.EvaluateAsync<object>("window.localStorage");
        ExpectToEqual(localStorage2, new { name1 = "value2" });

        await context.CloseAsync();
    }

    [UpstreamTest("ST-009 should handle missing file")]
    [LocalFixme("dotnet: the client reads the file itself, and the message is 'The specified storage state file does not exist: <file>'. Upstream expects 'Error reading storage state from <file>:\\nENOENT'")]
    public async Task ShouldHandleMissingFile()
    {
        var file = Info.OutputPath("does-not-exist.json");
        var error = await Catch(() => ContextFactory(new()
        {
            StorageStatePath = file,
        }));
        Assert.That(error.Message, Does.Contain($"Error reading storage state from {file}:\nENOENT"));
    }

    [UpstreamTest("ST-010 should handle malformed file")]
    [LocalFixme("dotnet: the client parses the file itself, and the message is ''not-json' is an invalid JSON literal. Expected the literal 'null'. Path: $ | LineNumber: 0 | BytePositionInLine: 1.'. Upstream expects 'Error reading storage state from <file>:\\nUnexpected token 'o', \"not-json\" is not valid JSON'")]
    public async Task ShouldHandleMalformedFile()
    {
        var file = Info.OutputPath("state.json");
        File.WriteAllText(file, "not-json");
        var error = await Catch(() => ContextFactory(new()
        {
            StorageStatePath = file,
        }));
        if (NodeVersion.Major > 18)
        {
            Assert.That(error.Message, Does.Contain($"Error reading storage state from {file}:\nUnexpected token 'o', \"not-json\" is not valid JSON"));
        }
        else
        {
            Assert.That(error.Message, Does.Contain($"Error reading storage state from {file}:\nUnexpected token o in JSON at position 1"));
        }
    }

    [UpstreamTest("ST-011 should serialize storageState with lone surrogates")]
    [LocalFixme("dotnet: the storage state has U+FFFD, the replacement character, in place of the lone surrogate U+DA7E")]
    public async Task ShouldSerializeStorageStateWithLoneSurrogates()
    {
        var page = await PageFixture();
        var context = await ContextFixture();
        await page.GotoAsync(Server.EmptyPage);
        await page.EvaluateAsync("() => window.localStorage.setItem('foo', String.fromCharCode(55934))");
        // .NET: a storage state is a JSON string.
        var storageState = JsonSerializer.Deserialize<JsonElement>(await context.StorageStateAsync());
        Assert.That(storageState.GetProperty("origins")[0].GetProperty("localStorage")[0].GetProperty("value").GetString(), Is.EqualTo(((char)55934).ToString()));
    }

    [UpstreamTest("ST-012 should work when service worker is intefering")]
    public async Task ShouldWorkWhenServiceWorkerIsIntefering()
    {
        var page = await PageFixture();
        var context = await ContextFixture();
        Info.Skip(IsAndroid);
        Info.Skip(IsElectron && ElectronMajorVersion < 30, "error: Browser context management is not supported.");

        Server.SetRoute("/", (req, res) =>
        {
            res.WriteHead(200, new() { ["content-type"] = "text/html" });
            res.End(@"
      <script>
        console.log('from page');
        window.localStorage.foo = 'bar';
        window.registrationPromise = navigator.serviceWorker.register('sw.js');
        window.activationPromise = new Promise(resolve => navigator.serviceWorker.oncontrollerchange = resolve);
      </script>
    ");
        });

        Server.SetRoute("/sw.js", (req, res) =>
        {
            res.WriteHead(200, new() { ["content-type"] = "application/javascript" });
            res.End(@"
      const kHtmlPage = `
        <script>
          console.log('from sw page');
          let counter = window.localStorage.counter || 0;
          ++counter;
          window.localStorage.counter = counter;
          setTimeout(() => {
            window.location.href = counter + '.html';
          }, 0);
        </script>
      `;

      console.log('from sw 1');
      self.addEventListener('fetch', event => {
        console.log('fetching ' + event.request.url);
        const blob = new Blob([kHtmlPage], { type: 'text/html' });
        const response = new Response(blob, { status: 200 , statusText: 'OK' });
        event.respondWith(response);
      });

      self.addEventListener('activate', event => {
        console.log('from sw 2');
        event.waitUntil(clients.claim());
      });
    ");
        });

        await page.GotoAsync(Server.Prefix);
        await page.EvaluateAsync("() => window['activationPromise']");

        // .NET: a storage state is a JSON string.
        var storageState = JsonSerializer.Deserialize<JsonElement>(await context.StorageStateAsync());
        ExpectToEqual(storageState.GetProperty("origins")[0].GetProperty("localStorage")[0], new { name = "foo", value = "bar" });
    }

    [UpstreamTest("ST-013 should work when service worker is intefering and the origin is not open")]
    public async Task ShouldWorkWhenServiceWorkerIsInteferingAndTheOriginIsNotOpen()
    {
        var page = await PageFixture();
        var context = await ContextFixture();
        Info.Skip(IsAndroid);
        Info.Skip(IsElectron && ElectronMajorVersion < 30, "error: Browser context management is not supported.");

        Server.SetRoute("/", (req, res) =>
        {
            res.WriteHead(200, new() { ["content-type"] = "text/html" });
            res.End(@"
      <script>
        window.localStorage.foo = 'bar';
        window.registrationPromise = navigator.serviceWorker.register('sw.js');
        window.activationPromise = new Promise(resolve => navigator.serviceWorker.oncontrollerchange = resolve);
      </script>
    ");
        });

        Server.SetRoute("/sw.js", (req, res) =>
        {
            res.WriteHead(200, new() { ["content-type"] = "application/javascript" });
            res.End(@"
      const kHtmlPage = `
        <script>
          window.localStorage.fromServiceWorker = 'yes';
          window.location.href = 'redirected.html';
        </script>
      `;

      self.addEventListener('fetch', event => {
        if (new URL(event.request.url).pathname !== '/')
          return;
        const blob = new Blob([kHtmlPage], { type: 'text/html' });
        event.respondWith(new Response(blob, { status: 200, statusText: 'OK' }));
      });

      self.addEventListener('activate', event => {
        event.waitUntil(clients.claim());
      });
    ");
        });

        Server.SetRoute("/redirected.html", (req, res) =>
        {
            res.WriteHead(200, new() { ["content-type"] = "text/html" });
            res.End("<html></html>");
        });

        await page.GotoAsync(Server.Prefix);
        await page.EvaluateAsync("() => window['activationPromise']");
        await page.GotoAsync("about:blank");

        // .NET: a storage state is a JSON string.
        var storageState = JsonSerializer.Deserialize<JsonElement>(await context.StorageStateAsync());
        ExpectToEqual(storageState.GetProperty("origins")[0].GetProperty("localStorage"), new[] { new { name = "foo", value = "bar" } });
    }

    [UpstreamTest("ST-014 should set local storage in third-party context")]
    public async Task ShouldSetLocalStorageInThirdPartyContext()
    {
        var context = await ContextFactory(new()
        {
            // .NET: a storage state is a JSON string.
            StorageState = JsonStringify(new
            {
                cookies = Array.Empty<object>(),
                origins = new[]
                {
                    new
                    {
                        origin = Server.CrossProcessPrefix,
                        localStorage = new[]
                        {
                            new
                            {
                                name = "name1",
                                value = "value1",
                            },
                        },
                    },
                },
            }),
        });
        var page = await context.NewPageAsync();
        await page.GotoAsync(Server.EmptyPage);
        var frame = await AttachFrame(page, "frame1", Server.CrossProcessPrefix + "/empty.html");

        var localStorage = await frame.EvaluateAsync<object>("window.localStorage");
        ExpectToEqual(localStorage, new { name1 = "value1" });
        await context.CloseAsync();
    }

    [UpstreamTest("ST-015 should roundtrip local storage in third-party context")]
    public async Task ShouldRoundtripLocalStorageInThirdPartyContext()
    {
        var page = await PageFixture();
        await page.GotoAsync(Server.EmptyPage);
        var frame = await AttachFrame(page, "frame1", Server.CrossProcessPrefix + "/empty.html");
        await frame.EvaluateAsync("() => window.localStorage.setItem('name1', 'value1')");
        var storageState = await page.Context.StorageStateAsync();

        var context2 = await ContextFactory(new() { StorageState = storageState });
        var page2 = await context2.NewPageAsync();
        await page2.GotoAsync(Server.EmptyPage);
        var frame2 = await AttachFrame(page2, "frame1", Server.CrossProcessPrefix + "/empty.html");

        var localStorage = await frame2.EvaluateAsync<object>("window.localStorage");
        ExpectToEqual(localStorage, new { name1 = "value1" });
        await context2.CloseAsync();
    }

    [UpstreamTest("ST-016 should support IndexedDB")]
    public async Task ShouldSupportIndexedDB()
    {
        var page = await PageFixture();
        await page.GotoAsync(Server.Prefix + "/to-do-notifications/index.html");

        await Expect(page.Locator("#notifications")).ToMatchAriaSnapshotAsync(@"
    - list:
      - listitem: Database initialised.
  ");
        await page.GetByLabel("Task title").FillAsync("Pet the cat");
        await page.GetByLabel("Hours").FillAsync("1");
        await page.GetByLabel("Mins").FillAsync("1");
        await page.GetByText("Add Task").ClickAsync();
        await Expect(page.Locator("#notifications")).ToMatchAriaSnapshotAsync(@"
    - list:
      - listitem: ""Transaction completed: database modification finished.""
  ");

        var storageState = await page.Context.StorageStateAsync(new() { IndexedDB = true });
        // .NET: a storage state is a JSON string.
        ExpectToEqual(JsonSerializer.Deserialize<JsonElement>(storageState).GetProperty("origins"), new[]
        {
            new
            {
                origin = Server.Prefix,
                localStorage = Array.Empty<object>(),
                indexedDB = new[]
                {
                    new
                    {
                        name = "toDoList",
                        version = 4,
                        stores = new[]
                        {
                            new
                            {
                                name = "toDoList",
                                autoIncrement = false,
                                keyPath = "taskTitle",
                                records = new[]
                                {
                                    new
                                    {
                                        valueEncoded = new
                                        {
                                            id = 1,
                                            o = new object[]
                                            {
                                                new { k = "taskTitle", v = "Pet the cat" },
                                                new { k = "hours", v = "1" },
                                                new { k = "minutes", v = "1" },
                                                new { k = "day", v = "01" },
                                                new { k = "month", v = "January" },
                                                new { k = "year", v = "2025" },
                                                new { k = "notified", v = "no" },
                                                new { k = "binaryTitle", v = new { ab = new { b = "UGV0IHRoZSBjYXQ=" } } },
                                            },
                                        },
                                    },
                                },
                                indexes = new[]
                                {
                                    new
                                    {
                                        name = "day",
                                        keyPath = "day",
                                        multiEntry = false,
                                        unique = false,
                                    },
                                    new
                                    {
                                        name = "hours",
                                        keyPath = "hours",
                                        multiEntry = false,
                                        unique = false,
                                    },
                                    new
                                    {
                                        name = "minutes",
                                        keyPath = "minutes",
                                        multiEntry = false,
                                        unique = false,
                                    },
                                    new
                                    {
                                        name = "month",
                                        keyPath = "month",
                                        multiEntry = false,
                                        unique = false,
                                    },
                                    new
                                    {
                                        name = "notified",
                                        keyPath = "notified",
                                        multiEntry = false,
                                        unique = false,
                                    },
                                    new
                                    {
                                        name = "year",
                                        keyPath = "year",
                                        multiEntry = false,
                                        unique = false,
                                    },
                                },
                            },
                        },
                    },
                },
            },
        });

        var context = await ContextFactory(new() { StorageState = storageState });
        ExpectToEqual(JsonSerializer.Deserialize<JsonElement>(await context.StorageStateAsync(new() { IndexedDB = true })), JsonSerializer.Deserialize<JsonElement>(storageState));

        var recreatedPage = await context.NewPageAsync();
        await recreatedPage.GotoAsync(Server.Prefix + "/to-do-notifications/index.html");
        await Expect(recreatedPage.Locator("#task-list")).ToMatchAriaSnapshotAsync(@"
    - list:
      - listitem:
        - text: /Pet the cat \[Pet the cat\]/
  ");

        ExpectToEqual(JsonSerializer.Deserialize<JsonElement>(await context.StorageStateAsync()), new { cookies = Array.Empty<object>(), origins = Array.Empty<object>() });
    }

    [UpstreamTest("ST-017 should roundtrip IndexedDB values and keys with ${restore}")]
    [TestCase("newContext")]
    [TestCase("setStorageState")]
    [LocalFixme("dotnet: SetStorageStateAsync takes the path of a file only. A storage state, which is a JSON string in .NET, fails with 'The specified storage state file does not exist: {\"cookies\":[],...' (only for setStorageState)")]
    public async Task ShouldRoundtripIndexedDBValuesAndKeysWith(string restore)
    {
        var page = await PageFixture();
        await page.GotoAsync(Server.EmptyPage);
        await page.EvaluateAsync(@"() => new Promise((resolve, reject) => {
      const request = indexedDB.open('collections', 1);
      request.onupgradeneeded = () => request.result.createObjectStore('store');
      request.onerror = () => reject(request.error);
      request.onsuccess = () => {
        const db = request.result;
        const transaction = db.transaction('store', 'readwrite');
        const store = transaction.objectStore('store');
        store.put(new Map([['mk', 'mv']]), 'map');
        store.put(new Set([1, 2]), 'set');
        store.put(0, 'zero');
        store.put(false, 'false');
        store.put('', 'empty');
        store.put(null, 'null');
        store.put(Infinity, 'infinity');
        store.put('first', 0);
        store.put('second', '');
        transaction.oncomplete = () => {
          db.close();
          resolve();
        };
        transaction.onerror = () => reject(transaction.error);
      };
    })");

        var path = Info.OutputPath("storage-state.json");
        var storageState = await page.Context.StorageStateAsync(new() { IndexedDB = true, Path = path });
        var context = await ContextFactory(restore == "newContext" ? new() { StorageStatePath = path } : new());
        if (restore == "setStorageState")
        {
            await context.SetStorageStateAsync(storageState);
        }
        // .NET: a storage state is a JSON string.
        ExpectToEqual(JsonSerializer.Deserialize<JsonElement>(await context.StorageStateAsync(new() { IndexedDB = true })), JsonSerializer.Deserialize<JsonElement>(storageState));

        var restoredPage = await context.NewPageAsync();
        await restoredPage.GotoAsync(Server.EmptyPage);
        var entries = await restoredPage.EvaluateAsync<object>(@"async () => {
      const db = await new Promise((resolve, reject) => {
        const request = indexedDB.open('collections', 1);
        request.onerror = () => reject(request.error);
        request.onsuccess = () => resolve(request.result);
      });
      const transaction = db.transaction('store', 'readonly');
      const store = transaction.objectStore('store');
      transaction.oncomplete = () => db.close();
      const [keys, values] = await Promise.all([store.getAllKeys(), store.getAll()].map(request => new Promise((resolve, reject) => {
        request.onsuccess = () => resolve(request.result);
        request.onerror = () => reject(request.error);
      })));
      return keys.map((key, i) => {
        const value = values[i];
        if (value instanceof Map)
          return [key, { map: [...value] }];
        if (value instanceof Set)
          return [key, { set: [...value] }];
        return [key, value];
      });
    }");
        ExpectToEqual(entries, new object[]
        {
            new object[] { 0, "first" },
            new object[] { "", "second" },
            new object[] { "empty", "" },
            new object[] { "false", false },
            new object[] { "infinity", double.PositiveInfinity },
            new object[] { "map", new { map = new[] { new[] { "mk", "mv" } } } },
            new object[] { "null", null },
            new object[] { "set", new { set = new[] { 1, 2 } } },
            new object[] { "zero", 0 },
        });
    }

    [UpstreamTest("ST-018 should support empty indexedDB")]
    public async Task ShouldSupportEmptyIndexedDB()
    {
        var page = await PageFixture();
        await page.GotoAsync(Server.EmptyPage);
        await page.EvaluateAsync(@"() => new Promise(resolve => {
    const openRequest = indexedDB.open('unused-db');
    openRequest.onsuccess = () => resolve();
    openRequest.onerror = () => resolve();
  })");
        var storageState = await page.Context.StorageStateAsync(new() { IndexedDB = true });
        // .NET: a storage state is a JSON string.
        ExpectToEqual(JsonSerializer.Deserialize<JsonElement>(storageState).GetProperty("origins"), new[]
        {
            new
            {
                origin = Server.Prefix,
                localStorage = Array.Empty<object>(),
                indexedDB = new[]
                {
                    new
                    {
                        name = "unused-db",
                        version = 1,
                        stores = Array.Empty<object>(),
                    },
                },
            },
        });

        var context = await ContextFactory(new() { StorageState = storageState });
        ExpectToEqual(JsonSerializer.Deserialize<JsonElement>(await context.StorageStateAsync(new() { IndexedDB = true })), JsonSerializer.Deserialize<JsonElement>(storageState));
    }

    [UpstreamTest("ST-019 should not leave IndexedDB connections open")]
    [LocalFixme("dotnet: SetStorageStateAsync takes the path of a file only. A storage state, which is a JSON string in .NET, fails with 'The specified storage state file does not exist: {\"cookies\":[],...'")]
    public async Task ShouldNotLeaveIndexedDBConnectionsOpen()
    {
        var context = await ContextFactory();
        var page = await context.NewPageAsync();
        await page.GotoAsync(Server.EmptyPage);
        await page.EvaluateAsync(@"async () => {
    const openRequest = indexedDB.open('db', 1);
    openRequest.onupgradeneeded = () => openRequest.result.createObjectStore('store');
    await new Promise((resolve, reject) => {
      openRequest.onsuccess = () => {
        const db = openRequest.result;
        const transaction = db.transaction('store', 'readwrite');
        transaction.objectStore('store').put('value', 'key');
        transaction.oncomplete = () => {
          db.close();
          resolve();
        };
        transaction.onerror = () => reject(transaction.error);
      };
      openRequest.onerror = () => reject(openRequest.error);
    });
  }");

        var state = await context.StorageStateAsync(new() { IndexedDB = true });

        await page.EvaluateAsync(@"() => new Promise((resolve, reject) => {
    const request = indexedDB.deleteDatabase('db');
    request.onsuccess = () => resolve();
    request.onerror = () => reject(request.error);
    request.onblocked = () => reject(new Error('deleteDatabase was blocked'));
  })");

        await context.SetStorageStateAsync(state);
        // .NET: a storage state is a JSON string.
        ExpectToEqual(JsonSerializer.Deserialize<JsonElement>(await context.StorageStateAsync(new() { IndexedDB = true })), JsonSerializer.Deserialize<JsonElement>(state));
    }

    [UpstreamTest("ST-020 should round-trip WebAuthn credentials with storageState")]
    public async Task ShouldRoundTripWebAuthnCredentialsWithStorageState()
    {
        var context = await ContextFactory();
        var credential = await context.Credentials.CreateAsync(Server.Hostname);

        // Credentials are opt-in, omitted by default.
        // .NET: a storage state is a JSON string.
        ExpectToEqual(JsonSerializer.Deserialize<JsonElement>(await context.StorageStateAsync()), new { cookies = Array.Empty<object>(), origins = Array.Empty<object>() });

        var storageState = await context.StorageStateAsync(new() { Credentials = true });
        ExpectToEqual(JsonSerializer.Deserialize<JsonElement>(storageState), new { cookies = Array.Empty<object>(), origins = Array.Empty<object>(), credentials = new[] { credential } });

        // A fresh context seeded from the storage state holds the same credential and round-trips equal.
        var context2 = await ContextFactory(new() { StorageState = storageState });
        ExpectToEqual(await context2.Credentials.GetAsync(), new[] { credential });
        ExpectToEqual(JsonSerializer.Deserialize<JsonElement>(await context2.StorageStateAsync(new() { Credentials = true })), JsonSerializer.Deserialize<JsonElement>(storageState));
    }

    [UpstreamTest("ST-021 setStorageState should replace credentials")]
    [LocalFixme("dotnet: SetStorageStateAsync takes the path of a file only. A storage state, which is a JSON string in .NET, fails with 'The specified storage state file does not exist: {\"cookies\":[],...'")]
    public async Task SetStorageStateShouldReplaceCredentials()
    {
        var ctxA = await ContextFactory();
        var credA = await ctxA.Credentials.CreateAsync("a.example.com");
        var stateA = await ctxA.StorageStateAsync(new() { Credentials = true });

        var ctxB = await ContextFactory();
        var credB = await ctxB.Credentials.CreateAsync("b.example.com");
        var stateB = await ctxB.StorageStateAsync(new() { Credentials = true });

        var context = await ContextFactory(new() { StorageState = stateA });
        ExpectToEqual(await context.Credentials.GetAsync(), new[] { credA });

        // Replacing the storage state swaps in the new credentials.
        await context.SetStorageStateAsync(stateB);
        ExpectToEqual(await context.Credentials.GetAsync(), new[] { credB });

        // A storage state without credentials clears them.
        // .NET: a storage state is a JSON string.
        await context.SetStorageStateAsync(JsonStringify(new { cookies = Array.Empty<object>(), origins = Array.Empty<object>() }));
        ExpectToEqual(await context.Credentials.GetAsync(), Array.Empty<object>());

        // Credentials can be installed again afterwards.
        await context.SetStorageStateAsync(stateA);
        ExpectToEqual(await context.Credentials.GetAsync(), new[] { credA });
    }

    [UpstreamTest("ST-022 setStorageState should handle missing file")]
    [LocalFixme("dotnet: the message is 'The specified storage state file does not exist: <file>'. Upstream expects 'Error reading storage state from <file>'")]
    public async Task SetStorageStateShouldHandleMissingFile()
    {
        var context = await ContextFactory();
        var file = Info.OutputPath("does-not-exist.json");
        var error = await Catch(() => context.SetStorageStateAsync(file));
        Assert.That(error.Message, Does.Contain($"Error reading storage state from {file}"));
    }
}
