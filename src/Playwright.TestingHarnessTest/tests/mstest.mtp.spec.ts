import { test, expect } from './baseTest';

test.use({ testMode: 'mstest.mtp' });

const cleanEnvironment = { BROWSER: undefined, HEADED: undefined, PWDEBUG: undefined, PW_INTERNAL_ADAPTER_SETTINGS: undefined };

function pageTest(body: string, overrides = '') {
  return `
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Playwright;
    using Microsoft.Playwright.MSTest;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class <class-name> : PageTest
    {
        [TestMethod]
        public async Task Test()
        {
            ${body}
        }
        ${overrides}
    }`;
}

test('should configure the browser and nested context options', async ({ runTest }) => {
  const result = await runTest({
    'Tests.cs': pageTest(`
      Assert.AreEqual("firefox", BrowserName);
      Assert.AreEqual("fr-FR", await Page.EvaluateAsync<string>("() => navigator.language"));
      Assert.AreEqual("Europe/Madrid", await Page.EvaluateAsync<string>("() => Intl.DateTimeFormat().resolvedOptions().timeZone"));
      Assert.AreEqual(1280, await Page.EvaluateAsync<int>("() => innerWidth"));
      Assert.AreEqual(720, await Page.EvaluateAsync<int>("() => innerHeight"));
      Assert.IsTrue(await Page.EvaluateAsync<bool>("() => matchMedia('(prefers-color-scheme: dark)').matches"));
      Assert.IsTrue(await Page.EvaluateAsync<bool>("() => matchMedia('(prefers-reduced-motion: reduce)').matches"));
      await Page.SetContentAsync("<div data-custom-id='target'>configured</div>");
      await Expect(Page.GetByTestId("target")).ToHaveTextAsync("configured");
    `),
    'testconfig.json': JSON.stringify({
      playwright: {
        browserName: 'firefox',
        testIdAttribute: 'data-custom-id',
        contextOptions: {
          locale: 'fr-FR',
          timezoneId: 'Europe/Madrid',
          viewportSize: { width: 1280, height: 720 },
          colorScheme: 'dark',
          reducedMotion: 'reduce',
        },
      },
    }),
  }, 'dotnet run', cleanEnvironment);
  expect(result.exitCode).toBe(0);
  expect(result.passed).toBe(1);
  expect(result.total).toBe(1);
});

test('should support launch arrays and dictionary options', async ({ runTest, server }) => {
  const result = await runTest({
    'Tests.cs': pageTest(`
      Assert.AreEqual("configured-agent", await Page.EvaluateAsync<string>("() => navigator.userAgent"));
      var response = await Page.GotoAsync("${server.EMPTY_PAGE}");
      Assert.AreEqual("configured-header", await response.Request.HeaderValueAsync("X-Test"));
      CollectionAssert.AreEqual(new[] { "--user-agent=configured-agent" }, new List<string>(Microsoft.Playwright.TestAdapter.PlaywrightSettingsProvider.LaunchOptions.Args));
      Assert.AreEqual("configured-env", new Dictionary<string, string>(Microsoft.Playwright.TestAdapter.PlaywrightSettingsProvider.LaunchOptions.Env)["PW_CONFIG_TEST"]);
    `),
    'testconfig.json': JSON.stringify({
      playwright: {
        launchOptions: { args: ['--user-agent=configured-agent'], env: { PW_CONFIG_TEST: 'configured-env' } },
        contextOptions: { extraHTTPHeaders: { 'X-Test': 'configured-header' } },
      },
    }),
  }, 'dotnet run', cleanEnvironment);
  expect(result.exitCode).toBe(0);
  expect(result.passed).toBe(1);
});

test('should support nested launch proxy options', async ({ runTest, server, proxyServer }) => {
  const result = await runTest({
    'Tests.cs': pageTest(`await Page.GotoAsync("${server.EMPTY_PAGE}");`),
    'testconfig.json': JSON.stringify({
      playwright: {
        launchOptions: {
          proxy: { server: proxyServer.listenAddr(), username: 'user', password: 'pwd' },
        },
      },
    }),
  }, 'dotnet run', cleanEnvironment);
  expect(result.exitCode).toBe(0);
  expect(result.passed).toBe(1);
  expect(proxyServer.requests).toContainEqual({ url: server.EMPTY_PAGE, auth: 'user:pwd' });
});

test('should apply the configured expect timeout', async ({ runTest }) => {
  const result = await runTest({
    'Tests.cs': pageTest(`
      await Page.SetContentAsync("<div>actual</div>");
      await Expect(Page.Locator("div")).ToHaveTextAsync("wrong");
    `),
    'testconfig.json': JSON.stringify({ playwright: { expectTimeout: 123 } }),
  }, 'dotnet run', cleanEnvironment);
  expect(result.exitCode).toBe(2);
  expect(result.failed).toBe(1);
  expect(result.rawStdout).toContain('with timeout 123ms');
});

test('should preserve defaults without configuration and ignore stale VSTest forwarding', async ({ runTest }) => {
  const result = await runTest({
    'Tests.cs': pageTest(`
      Assert.AreEqual("chromium", BrowserName);
      Assert.AreEqual("en-US", await Page.EvaluateAsync<string>("() => navigator.language"));
      Assert.IsTrue(await Page.EvaluateAsync<bool>("() => matchMedia('(prefers-color-scheme: light)').matches"));
      await Page.SetContentAsync("<div data-testid='target'>default</div>");
      await Expect(Page.GetByTestId("target")).ToHaveTextAsync("default");
    `),
  }, 'dotnet run', { ...cleanEnvironment, PW_INTERNAL_ADAPTER_SETTINGS: '{"BrowserName":"firefox"}' });
  expect(result.exitCode).toBe(0);
  expect(result.passed).toBe(1);
});

test('should select configuration using the MTP config-file option', async ({ runTest }) => {
  const result = await runTest({
    'Tests.cs': pageTest(`Assert.AreEqual("firefox", BrowserName); await Page.GotoAsync("about:blank");`),
    'testconfig.json': JSON.stringify({ playwright: { browserName: 'chromium' } }),
    'alternative.json': JSON.stringify({ playwright: { browserName: 'firefox' } }),
  }, 'dotnet run -- --config-file alternative.json', cleanEnvironment);
  expect(result.exitCode).toBe(0);
  expect(result.passed).toBe(1);
});

test('should preserve environment precedence over JSON configuration', async ({ runTest }) => {
  const result = await runTest({
    'Tests.cs': pageTest(`
      Assert.AreEqual("chromium", BrowserName);
      Assert.IsFalse(Microsoft.Playwright.TestAdapter.PlaywrightSettingsProvider.LaunchOptions.Headless);
      Assert.AreEqual("HeadedChrome", await Page.EvaluateAsync<string>("() => navigator.userAgent.includes('Headless') ? 'HeadlessChrome' : 'HeadedChrome'"));
    `),
    'testconfig.json': JSON.stringify({ playwright: { browserName: 'firefox', launchOptions: { headless: true } } }),
  }, 'dotnet run', { ...cleanEnvironment, BROWSER: 'CHROMIUM', HEADED: '1' });
  expect(result.exitCode).toBe(0);
  expect(result.passed).toBe(1);
});

test('should preserve code overrides and allow customizing configured defaults', async ({ runTest }) => {
  const result = await runTest({
    'Tests.cs': pageTest(`
      Assert.AreEqual("code-agent", await Page.EvaluateAsync<string>("() => navigator.userAgent"));
      Assert.AreEqual("fr-FR", await Page.EvaluateAsync<string>("() => navigator.language"));
      Assert.AreEqual("configured-timezone", ContextOptions().TimezoneId == "Europe/Madrid" ? "configured-timezone" : "wrong");
      Assert.AreEqual(800, await Page.EvaluateAsync<int>("() => innerWidth"));
    `, `
      public override Task<BrowserTypeLaunchOptions> LaunchOptionsAsync() => Task.FromResult(new BrowserTypeLaunchOptions { Args = new[] { "--user-agent=code-agent" } });
      public override BrowserNewContextOptions ContextOptions()
      {
          var options = base.ContextOptions();
          options.ViewportSize = new() { Width = 800, Height = 600 };
          return options;
      }
    `),
    'testconfig.json': JSON.stringify({
      playwright: {
        launchOptions: { args: ['--user-agent=file-agent'] },
        contextOptions: { locale: 'fr-FR', timezoneId: 'Europe/Madrid', viewportSize: { width: 1280, height: 720 } },
      },
    }),
  }, 'dotnet run', cleanEnvironment);
  expect(result.exitCode).toBe(0);
  expect(result.passed).toBe(1);
});

for (const [pwDebug, environment, expected] of [
  ['0', undefined, '0'],
  ['1', undefined, '1'],
  ['console', undefined, 'console'],
  ['1', '0', '0'],
] as const) {
  test(`should initialize PWDEBUG=${pwDebug} before fixtures, with environment ${environment}`, async ({ runTest }) => {
    const result = await runTest({
      'Tests.cs': `
        using System;
        using Microsoft.VisualStudio.TestTools.UnitTesting;
        [TestClass]
        public class <class-name>
        {
            private static readonly string DebugAtInitialization = Environment.GetEnvironmentVariable("PWDEBUG");
            [TestMethod]
            public void Test() => Assert.AreEqual("${expected}", DebugAtInitialization);
        }`,
      'testconfig.json': JSON.stringify({ playwright: { pwDebug } }),
    }, 'dotnet run', { ...cleanEnvironment, PWDEBUG: environment });
    expect(result.exitCode).toBe(0);
    expect(result.passed).toBe(1);
  });
}

for (const [name, config, diagnostic] of [
  ['invalid browser', { browserName: 'unknown' }, 'Invalid browser name'],
  ['unknown setting', { browserNmae: 'firefox' }, 'browserNmae'],
  ['unknown launch option', { launchOptions: { headles: false } }, 'headles'],
  ['unknown context option', { contextOptions: { local: 'fr-FR' } }, 'local'],
  ['invalid timeout type', { expectTimeout: 'wrong' }, 'expectTimeout'],
  ['negative timeout', { expectTimeout: -1 }, 'non-negative'],
  ['invalid enum', { contextOptions: { colorScheme: 'unknown' } }, 'unknown'],
  ['invalid debug mode', { pwDebug: 'wrong' }, 'pwDebug'],
  ['invalid root shape', ['firefox'], 'PlaywrightConfiguration'],
] as const) {
  test(`should fail explicitly for ${name}`, async ({ runTest }) => {
    const result = await runTest({
      'Tests.cs': pageTest(`Assert.Fail("The test should not run with invalid configuration.");`),
      'testconfig.json': JSON.stringify({ playwright: config }),
    }, 'dotnet run', cleanEnvironment);
    expect(result.exitCode).not.toBe(0);
    expect(result.total).toBe(0);
    expect(result.rawStdout + result.stderr).toContain(diagnostic);
  });
}

test('should leave Playwright runsettings to VSTest and use MTP configuration', async ({ runTest }) => {
  const result = await runTest({
    'Tests.cs': pageTest(`Assert.AreEqual("chromium", BrowserName); await Page.GotoAsync("about:blank");`),
    'ignored.runsettings': `<RunSettings><Playwright><BrowserName>firefox</BrowserName></Playwright><RunConfiguration><MaxCpuCount>2</MaxCpuCount></RunConfiguration></RunSettings>`,
    'testconfig.json': JSON.stringify({ playwright: { browserName: 'chromium' } }),
  }, 'dotnet run -- --settings ignored.runsettings', cleanEnvironment);
  expect(result.exitCode).toBe(0);
  expect(result.passed).toBe(1);
  expect(result.rawStdout).toContain("Runsettings attribute 'MaxCpuCount'");
});

test('should support discovery without executing tests', async ({ runTest }) => {
  const result = await runTest({
    'Tests.cs': pageTest(`Assert.Fail("Discovery should not execute tests.");`),
    'testconfig.json': JSON.stringify({ playwright: { browserName: 'firefox' } }),
  }, 'dotnet run -- --list-tests', cleanEnvironment);
  expect(result.exitCode).toBe(0);
  expect(result.total).toBe(0);
  expect(result.rawStdout).toContain('Test');
});
