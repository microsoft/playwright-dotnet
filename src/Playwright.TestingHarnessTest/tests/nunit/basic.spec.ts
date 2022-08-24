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

import { test, expect } from '../baseTest';

test('should be able to forward DEBUG=pw:api env var', async ({ runTest }) => {
  const result = await runTest({
    'ExampleTests.cs': `
      using System;
      using System.Threading.Tasks;
      using Microsoft.Playwright.NUnit;
      using NUnit.Framework;

      namespace Playwright.TestingHarnessTest.NUnit;

      public class <class-name> : PageTest
      {
          [Test]
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
  }, 'dotnet test --settings=.runsettings', {
    // Workaround for https://github.com/nunit/nunit/issues/4144
    PWAPI_TO_STDOUT: '1',
  });
  expect(result.passed).toBe(1);
  expect(result.failed).toBe(0);
  expect(result.total).toBe(1);
  expect(result.stdout).toContain("pw:api")
  expect(result.stdout).toContain("element is not enabled - waiting...")
});

test('should be able to set the browser via the runsettings file', async ({ runTest }) => {
  const result = await runTest({
    'ExampleTests.cs': `
      using System;
      using System.Threading.Tasks;
      using Microsoft.Playwright.NUnit;
      using NUnit.Framework;

      namespace Playwright.TestingHarnessTest.NUnit;

      public class <class-name> : PageTest
      {
          [Test]
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
      using Microsoft.Playwright.NUnit;
      using NUnit.Framework;

      namespace Playwright.TestingHarnessTest.NUnit;

      public class <class-name> : PageTest
      {
          [Test]
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
      using Microsoft.Playwright.NUnit;
      using NUnit.Framework;

      namespace Playwright.TestingHarnessTest.NUnit;

      public class <class-name> : PageTest
      {
          [Test]
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

test('should be able to override context options', async ({ runTest }) => {
  const result = await runTest({
    'ExampleTests.cs': `
      using System;
      using System.Collections.Generic;
      using System.Threading.Tasks;
      using Microsoft.Playwright;
      using Microsoft.Playwright.NUnit;
      using NUnit.Framework;
      
      namespace Playwright.TestingHarnessTest.NUnit;

      public class <class-name> : PageTest
      {
        [Test]
        public async Task Test()
        {
            await Page.GotoAsync("about:blank");

            Assert.False(await Page.EvaluateAsync<bool>("() => matchMedia('(prefers-color-scheme: light)').matches"));
            Assert.True(await Page.EvaluateAsync<bool>("() => matchMedia('(prefers-color-scheme: dark)').matches"));

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
      using Microsoft.Playwright.NUnit;
      using NUnit.Framework;

      namespace Playwright.TestingHarnessTest.NUnit;

      public class <class-name> : PageTest
      {
          [Test]
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
      using System.Collections.Generic;
      using System.Threading.Tasks;
      using Microsoft.Playwright;
      using Microsoft.Playwright.NUnit;
      using NUnit.Framework;
      
      namespace Playwright.TestingHarnessTest.NUnit;

      public class <class-name> : PageTest
      {
          [Test]
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
        using System.Collections.Generic;
        using System.Threading.Tasks;
        using Microsoft.Playwright;
        using Microsoft.Playwright.NUnit;
        using NUnit.Framework;
        
        namespace Playwright.TestingHarnessTest.NUnit;

        public class <class-name> : PageTest
        {
            [Test]
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
  test('should be able to override it via the global config', async ({ runTest }) => {
    const result = await runTest({
      'ExampleTests.cs': `
      using System;
      using System.Collections.Generic;
      using System.Threading.Tasks;
      using Microsoft.Playwright;
      using Microsoft.Playwright.NUnit;
      using NUnit.Framework;
      
      namespace Playwright.TestingHarnessTest.NUnit;

      public class <class-name> : PageTest
      {
          [Test]
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
