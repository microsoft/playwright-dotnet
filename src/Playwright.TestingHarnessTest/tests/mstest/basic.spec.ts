/*
 * MIT License
 *
 * Copyright (c) Microsoft Corporation.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
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

import http from 'http';
import { test, expect } from '../baseTest';
import httpProxy from 'http-proxy';

test('should be able to forward DEBUG=pw:api env var', async ({ runTest }) => {
  const result = await runTest({
    'ExampleTests.cs': `
      using System;
      using System.Threading.Tasks;
      using Microsoft.Playwright.MSTest;
      using Microsoft.VisualStudio.TestTools.UnitTesting;

      namespace Playwright.TestingHarnessTest.MSTest;

      [TestClass]  
      public class <class-name> : PageTest
      {
          [TestMethod]
          public async Task Test()
          {
              await Page.GotoAsync("about:blank");
              await Page.SetContentAsync("<button disabled>Click me</button>");
              try
              {
                  await Page.Locator("button").ClickAsync(new() { Timeout = 1_000 });
              }
              catch
              {
              }
          }
      }`,
    '.runsettings': `
      <?xml version="1.0" encoding="utf-8"?>
      <RunSettings>
        <RunConfiguration>
          <EnvironmentVariables>
            <DEBUG>pw:api</DEBUG>
          </EnvironmentVariables>
        </RunConfiguration>
      </RunSettings>`,
  }, 'dotnet test --settings=.runsettings');
  expect(result.passed).toBe(1);
  expect(result.failed).toBe(0);
  expect(result.total).toBe(1);
  expect(result.stderr).toContain("pw:api")
  expect(result.stderr).toContain("element is not enabled")
  expect(result.stderr).toContain("retrying click action")
});

test('should be able to set the browser via the runsettings file', async ({ runTest }) => {
  const result = await runTest({
    'ExampleTests.cs': `
      using System;
      using System.Threading.Tasks;
      using Microsoft.Playwright.MSTest;
      using Microsoft.VisualStudio.TestTools.UnitTesting;

      namespace Playwright.TestingHarnessTest.MSTest;

      [TestClass]  
      public class <class-name> : PageTest
      {
          [TestMethod]
          public async Task Test()
          {
              await Page.GotoAsync("about:blank");
              Console.WriteLine("BrowserName: " + BrowserName);
              Console.WriteLine("BrowserType: " + BrowserType.Name);
              Console.WriteLine("User-Agent: " + await Page.EvaluateAsync<string>("() => navigator.userAgent"));
          }
      }`,
    '.runsettings': `
    <?xml version="1.0" encoding="utf-8"?>
    <RunSettings>
      <Playwright>
        <BrowserName>webkit</BrowserName>
      </Playwright>
    </RunSettings>
    `,
  }, 'dotnet test --settings=.runsettings');
  expect(result.passed).toBe(1);
  expect(result.failed).toBe(0);
  expect(result.total).toBe(1);
  expect(result.stdout).toContain("BrowserName: webkit")
  expect(result.stdout).toContain("BrowserType: webkit")
  expect(/User-Agent: .*WebKit.*/.test(result.stdout)).toBeTruthy()
});

test('should prioritize browser from env over the runsettings file', async ({ runTest }) => {
  const result = await runTest({
    'ExampleTests.cs': `
      using System;
      using System.Threading.Tasks;
      using Microsoft.Playwright.MSTest;
      using Microsoft.VisualStudio.TestTools.UnitTesting;

      namespace Playwright.TestingHarnessTest.MSTest;

      [TestClass]  
      public class <class-name> : PageTest
      {
          [TestMethod]
          public async Task Test()
          {
              await Page.GotoAsync("about:blank");
              Console.WriteLine("BrowserName: " + BrowserName);
              Console.WriteLine("BrowserType: " + BrowserType.Name);
              Console.WriteLine("User-Agent: " + await Page.EvaluateAsync<string>("() => navigator.userAgent"));
          }
      }`,
    '.runsettings': `
    <?xml version="1.0" encoding="utf-8"?>
    <RunSettings>
      <Playwright>
        <BrowserName>webkit</BrowserName>
      </Playwright>
    </RunSettings>
    `,
  }, 'dotnet test --settings=.runsettings', {
    BROWSER: 'firefox'
  });
  expect(result.passed).toBe(1);
  expect(result.failed).toBe(0);
  expect(result.total).toBe(1);
  expect(result.stdout).toContain("BrowserName: firefox")
  expect(result.stdout).toContain("BrowserType: firefox")
  expect(/User-Agent: .*Firefox.*/.test(result.stdout)).toBeTruthy()
});

test('should be able to make the browser headed via the env', async ({ runTest }) => {
  const result = await runTest({
    'ExampleTests.cs': `
      using System;
      using System.Threading.Tasks;
      using Microsoft.Playwright.MSTest;
      using Microsoft.VisualStudio.TestTools.UnitTesting;

      namespace Playwright.TestingHarnessTest.MSTest;

      [TestClass]  
      public class <class-name> : PageTest
      {
          [TestMethod]
          public async Task Test()
          {
              await Page.GotoAsync("about:blank");
              Console.WriteLine("BrowserName: " + BrowserName);
              Console.WriteLine("User-Agent: " + await Page.EvaluateAsync<string>("() => navigator.userAgent"));
          }
      }`,
  }, 'dotnet test', {
    HEADED: '1'
  });
  expect(result.passed).toBe(1);
  expect(result.failed).toBe(0);
  expect(result.total).toBe(1);
  expect(result.stdout).toContain("BrowserName: chromium")
  expect(result.stdout).not.toContain("Headless")
});

test('should be able to to parse BrowserName and LaunchOptions.Headless from runsettings', async ({ runTest }) => {
  const result = await runTest({
    'ExampleTests.cs': `
      using System;
      using System.Threading.Tasks;
      using Microsoft.Playwright.MSTest;
      using Microsoft.VisualStudio.TestTools.UnitTesting;

      namespace Playwright.TestingHarnessTest.MSTest;

      [TestClass]  
      public class <class-name> : PageTest
      {
          [TestMethod]
          public async Task Test()
          {
              await Page.GotoAsync("about:blank");
              Console.WriteLine("BrowserName: " + BrowserName);
              Console.WriteLine("User-Agent: " + await Page.EvaluateAsync<string>("() => navigator.userAgent"));
          }
      }`,
      '.runsettings': `
        <?xml version="1.0" encoding="utf-8"?>
        <RunSettings>
          <Playwright>
            <LaunchOptions>
              <Headless>false</Headless>
            </LaunchOptions>
            <!-- Order here is important, so that LaunchOptions comes first -->
            <BrowserName>firefox</BrowserName>
          </Playwright>
        </RunSettings>
      `,
  }, 'dotnet test --settings=.runsettings');
  expect(result.passed).toBe(1);
  expect(result.failed).toBe(0);
  expect(result.total).toBe(1);
  expect(result.stdout).toContain("BrowserName: firefox")
  expect(result.stdout).not.toContain("Headless")
});

test('should be able to parse LaunchOptions.Proxy from runsettings', async ({ runTest }) => {
  const httpServer = http.createServer((req, res) => {
    res.end('hello world!')
  }).listen(3129);
  const proxyServer = httpProxy.createProxyServer({
    auth: 'user:pwd',
    target: 'http://localhost:3129',
  }).listen(3128);

  const waitForProxyRequest = new Promise<[string, string]>((resolve) => {
    proxyServer.once('proxyReq', (proxyReq, req, res, options) => {
      const authHeader = proxyReq.getHeader('authorization') as string;
      const auth = Buffer.from(authHeader.split(' ')[1], 'base64').toString();
      resolve([req.url, auth]);
    });
  })

  const result = await runTest({
    'ExampleTests.cs': `
      using System;
      using System.Threading.Tasks;
      using Microsoft.Playwright.MSTest;
      using Microsoft.VisualStudio.TestTools.UnitTesting;

      namespace Playwright.TestingHarnessTest.MSTest;

      [TestClass]  
      public class <class-name> : PageTest
      {
          [TestMethod]
          public async Task Test()
          {
              Console.WriteLine("User-Agent: " + await Page.EvaluateAsync<string>("() => navigator.userAgent"));
              await Page.GotoAsync("http://example.com");
          }
      }`,
      '.runsettings': `
        <?xml version="1.0" encoding="utf-8"?>
        <RunSettings>
            <Playwright>
                <BrowserName>chromium</BrowserName>
                <LaunchOptions>
                    <Headless>false</Headless>
                    <Proxy>
                        <Server>http://127.0.0.1:3128</Server>
                        <Username>user</Username>
                        <Password>pwd</Password>
                    </Proxy>
                </LaunchOptions>
            </Playwright>
        </RunSettings>
      `,
  }, 'dotnet test --settings=.runsettings');
  expect(result.passed).toBe(1);
  expect(result.failed).toBe(0);
  expect(result.total).toBe(1);

  expect(result.stdout).not.toContain("Headless");

  const [url, auth] = await waitForProxyRequest;
  expect(url).toBe('http://example.com/');
  expect(auth).toBe('user:pwd');

  proxyServer.close();
  httpServer.close();
});

test('should be able to parse LaunchOptions.Args from runsettings', async ({ runTest }) => {
  const result = await runTest({
    'ExampleTests.cs': `
      using System;
      using System.Threading.Tasks;
      using Microsoft.Playwright.MSTest;
      using Microsoft.VisualStudio.TestTools.UnitTesting;

      namespace Playwright.TestingHarnessTest.MSTest;

      [TestClass]  
      public class <class-name> : PageTest
      {
          [TestMethod]
          public async Task Test()
          {
              Console.WriteLine("User-Agent: " + await Page.EvaluateAsync<string>("() => navigator.userAgent"));
          }
      }`,
      '.runsettings': `
        <?xml version="1.0" encoding="utf-8"?>
        <RunSettings>
            <Playwright>
              <LaunchOptions>
                <Args>['--user-agent=hello']</Args>
              </LaunchOptions>
            </Playwright>
        </RunSettings>
      `,
  }, 'dotnet test --settings=.runsettings');
  expect(result.passed).toBe(1);
  expect(result.failed).toBe(0);
  expect(result.total).toBe(1);
  expect(result.stdout).toContain("User-Agent: hello")
});

test('should be able to override context options', async ({ runTest }) => {
  const result = await runTest({
    'ExampleTests.cs': `
      using System;
      using System.Collections.Generic;
      using System.Threading.Tasks;
      using Microsoft.Playwright;
      using Microsoft.Playwright.MSTest;
      using Microsoft.VisualStudio.TestTools.UnitTesting;
      
      namespace Playwright.TestingHarnessTest.MSTest;

      [TestClass]  
      public class <class-name> : PageTest
      {
        [TestMethod]
        public async Task Test()
        {
            await Page.GotoAsync("about:blank");

            Assert.IsFalse(await Page.EvaluateAsync<bool>("() => matchMedia('(prefers-color-scheme: light)').matches"));
            Assert.IsTrue(await Page.EvaluateAsync<bool>("() => matchMedia('(prefers-color-scheme: dark)').matches"));

            Assert.AreEqual(1920, await Page.EvaluateAsync<int>("() => window.innerWidth"));
            Assert.AreEqual(1080, await Page.EvaluateAsync<int>("() => window.innerHeight"));

            Assert.AreEqual("Foobar", await Page.EvaluateAsync<string>("() => navigator.userAgent"));

            var response = await Page.GotoAsync("https://example.com/");
            Assert.AreEqual(await response.Request.HeaderValueAsync("Kekstar"), "KekStarValue");
        }

        public override BrowserNewContextOptions ContextOptions()
        {
            return new BrowserNewContextOptions()
            {
                ColorScheme = ColorScheme.Dark,
                UserAgent = "Foobar",
                ViewportSize = new()
                {
                    Width = 1920,
                    Height = 1080
                },
                ExtraHTTPHeaders = new Dictionary<string, string> {
                    { "Kekstar", "KekStarValue" }
                }
            };
        }
      }`}, 'dotnet test');
  expect(result.passed).toBe(1);
  expect(result.failed).toBe(0);
  expect(result.total).toBe(1);
});

test('should be able to override launch options', async ({ runTest }) => {
  const result = await runTest({
    'ExampleTests.cs': `
      using System;
      using System.Threading.Tasks;
      using Microsoft.Playwright.MSTest;
      using Microsoft.VisualStudio.TestTools.UnitTesting;

      namespace Playwright.TestingHarnessTest.MSTest;

      [TestClass]  
      public class <class-name> : PageTest
      {
          [TestMethod]
          public async Task Test()
          {
              await Page.GotoAsync("about:blank");
              Console.WriteLine("User-Agent: " + await Page.EvaluateAsync<string>("() => navigator.userAgent"));
          }
      }`,
    '.runsettings': `
    <?xml version="1.0" encoding="utf-8"?>
    <RunSettings>
      <Playwright>
        <LaunchOptions>
          <Headless>false</Headless>
        </LaunchOptions>
      </Playwright>
    </RunSettings>
    `,
  }, 'dotnet test --settings=.runsettings');
  expect(result.passed).toBe(1);
  expect(result.failed).toBe(0);
  expect(result.total).toBe(1);
  expect(result.stdout).not.toContain("Headless");
});

test.describe('Expect() timeout', () => {
  test('should have 5 seconds by default', async ({ runTest }) => {
    const result = await runTest({
      'ExampleTests.cs': `
      using System;
      using System.Threading.Tasks;
      using Microsoft.Playwright.MSTest;
      using Microsoft.VisualStudio.TestTools.UnitTesting;

      namespace Playwright.TestingHarnessTest.MSTest;

      [TestClass]  
      public class <class-name> : PageTest
      {
          [TestMethod]
          public async Task Test()
          {
              await Page.GotoAsync("about:blank");
              await Page.SetContentAsync("<button>Click me</button>");
              await Expect(Page.Locator("button")).ToHaveTextAsync("noooo-wrong-text");
          }
      }`,
    }, 'dotnet test');
    expect(result.passed).toBe(0);
    expect(result.failed).toBe(1);
    expect(result.total).toBe(1);
    expect(result.rawStdout).toContain("LocatorAssertions.ToHaveTextAsync with timeout 5000ms")
  });
  test('should be able to override it via each Expect() call', async ({ runTest }) => {
    const result = await runTest({
      'ExampleTests.cs': `
        using System;
        using System.Threading.Tasks;
        using Microsoft.Playwright.MSTest;
        using Microsoft.VisualStudio.TestTools.UnitTesting;
  
        namespace Playwright.TestingHarnessTest.MSTest;
  
        [TestClass]  
        public class <class-name> : PageTest
        {
            [TestMethod]
            public async Task Test()
            {
                await Page.GotoAsync("about:blank");
                await Page.SetContentAsync("<button>Click me</button>");
                await Expect(Page.Locator("button")).ToHaveTextAsync("noooo-wrong-text", new() { Timeout = 100 });
            }
        }`,
    }, 'dotnet test');
    expect(result.passed).toBe(0);
    expect(result.failed).toBe(1);
    expect(result.total).toBe(1);
    expect(result.rawStdout).toContain("LocatorAssertions.ToHaveTextAsync with timeout 100ms")
  });
  test('should be able to override it via the global settings', async ({ runTest }) => {
    const result = await runTest({
      'ExampleTests.cs': `
      using System;
      using System.Threading.Tasks;
      using Microsoft.Playwright.MSTest;
      using Microsoft.VisualStudio.TestTools.UnitTesting;

      namespace Playwright.TestingHarnessTest.MSTest;

      [TestClass]  
      public class <class-name> : PageTest
      {
          [TestMethod]
          public async Task Test()
          {
              await Page.GotoAsync("about:blank");
              await Page.SetContentAsync("<button>Click me</button>");
              await Expect(Page.Locator("button")).ToHaveTextAsync("noooo-wrong-text");
          }
      }`,
      '.runsettings': `
      <?xml version="1.0" encoding="utf-8"?>
      <RunSettings>
        <Playwright>
          <ExpectTimeout>123</ExpectTimeout>
        </Playwright>
      </RunSettings>
      `,
    }, 'dotnet test --settings=.runsettings');
    expect(result.passed).toBe(0);
    expect(result.failed).toBe(1);
    expect(result.total).toBe(1);
    expect(result.rawStdout).toContain("LocatorAssertions.ToHaveTextAsync with timeout 123ms")
  });
});
