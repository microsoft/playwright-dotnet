import { createServer } from 'http';
import type { AddressInfo } from 'net';
import { test, expect } from '../baseTest';

test('should be able to set a custom navigation timeout', async ({ runTest }) => {
  const server = createServer(async (request, response) => {
    await new Promise(resolve => setTimeout(resolve, 2000));
    response.end('Hello World');
  })
  await new Promise(resolve => server.listen(resolve));

  const result = await runTest({
    'ExampleTests.cs': `
      using System;
      using System.Threading.Tasks;
      using Microsoft.Playwright.NUnit;
      using NUnit.Framework;

      namespace Playwright.TestingHarnessTest.NUnit;

      [TestFixture]  
      public class <class-name> : PageTest
      {
          [Test]
          public async Task Test()
          {
              await Page.GotoAsync("http://127.0.0.1:${(server.address() as AddressInfo).port}");
          }
      }`,
      '.runsettings': `
      <?xml version="1.0" encoding="utf-8"?>
      <RunSettings>
        <Playwright>
          <NavigationTimeout>1000</NavigationTimeout>
        </Playwright>
      </RunSettings>`,
  }, 'dotnet test --settings=.runsettings');
  expect(result.exitCode).toBe(1);
  expect(result.passed).toBe(0);
  expect(result.failed).toBe(1);
  expect(result.total).toBe(1);
  expect(result.rawStdout).toContain("System.TimeoutException : Timeout 1000ms exceeded.");
  expect(result.rawStdout).toContain(`navigating to "http://`);
  await new Promise(resolve => server.close(resolve));
});

test('should inherit NavigationTimeout from ActionTimeout', async ({ runTest }) => {
  const server = createServer(async (request, response) => {
    await new Promise(resolve => setTimeout(resolve, 2000));
    response.end('Hello World');
  })
  await new Promise(resolve => server.listen(resolve));

  const result = await runTest({
    'ExampleTests.cs': `
      using System;
      using System.Threading.Tasks;
      using Microsoft.Playwright.NUnit;
      using NUnit.Framework;

      namespace Playwright.TestingHarnessTest.NUnit;

      [TestFixture]  
      public class <class-name> : PageTest
      {
          [Test]
          public async Task Test()
          {
              await Page.GotoAsync("http://127.0.0.1:${(server.address() as AddressInfo).port}");
          }
      }`,
      '.runsettings': `
      <?xml version="1.0" encoding="utf-8"?>
      <RunSettings>
        <Playwright>
          <ActionTimeout>1000</ActionTimeout>
        </Playwright>
      </RunSettings>`,
  }, 'dotnet test --settings=.runsettings');
  expect(result.exitCode).toBe(1);
  expect(result.passed).toBe(0);
  expect(result.failed).toBe(1);
  expect(result.total).toBe(1);
  expect(result.rawStdout).toContain("System.TimeoutException : Timeout 1000ms exceeded.");
  expect(result.rawStdout).toContain(`navigating to "http://`);
  await new Promise(resolve => server.close(resolve));
});

test('should be able to set a custom ActionTimeout', async ({ runTest }) => {
  const result = await runTest({
    'ExampleTests.cs': `
      using System;
      using System.Threading.Tasks;
      using Microsoft.Playwright.NUnit;
      using NUnit.Framework;

      namespace Playwright.TestingHarnessTest.NUnit;

      [TestFixture]  
      public class <class-name> : PageTest
      {
          [Test]
          public async Task Test()
          {
              await Page.GotoAsync("data:text/html,");
              await Page.ClickAsync("my-element");
          }
      }`,
      '.runsettings': `
      <?xml version="1.0" encoding="utf-8"?>
      <RunSettings>
        <Playwright>
          <ActionTimeout>1000</ActionTimeout>
        </Playwright>
      </RunSettings>`,
  }, 'dotnet test --settings=.runsettings');
  expect(result.exitCode).toBe(1);
  expect(result.passed).toBe(0);
  expect(result.failed).toBe(1);
  expect(result.total).toBe(1);
  expect(result.rawStdout).toContain("System.TimeoutException : Timeout 1000ms exceeded.");
  expect(result.rawStdout).toContain("waiting for selector \"my-element\"");
});
