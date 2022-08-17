import { test, expect } from '../baseTest';

test('(retries 0) should not retry a passed test', async ({ runTest }) => {
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
          [PlaywrightTestMethod]
          public void Test()
          {
              Console.WriteLine("i-was-running");
          }
      }`,
  }, 'dotnet test');
  expect(result.exitCode).toBe(0);
  expect(result.passed).toBe(1);
  expect(result.failed).toBe(0);
  expect(result.total).toBe(1);
  expect(result.stdoutMessages.match(/i-was-running/g).length).toBe(1);
});

test('(retries 0) should not retry a failed test', async ({ runTest }) => {
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
          [PlaywrightTestMethod]
          public void Test()
          {
              Console.WriteLine("i-was-running");
              throw new Exception("i-was-broken");
          }
      }`,
  }, 'dotnet test');
  expect(result.exitCode).toBe(1);
  expect(result.passed).toBe(0);
  expect(result.failed).toBe(1);
  expect(result.total).toBe(1);
  expect(result.stdoutMessages.match(/i-was-running/g).length).toBe(1);
});

test('(retries 1) should not retry a passed test', async ({ runTest }) => {
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
          [PlaywrightTestMethod]
          public void Test()
          {
              Console.WriteLine("i-was-running");
          }
      }`,
      '.runsettings': `
      <?xml version="1.0" encoding="utf-8"?>
      <RunSettings>
        <Playwright>
          <Retries>1</Retries>
        </Playwright>
      </RunSettings>`,
  }, 'dotnet test --settings=.runsettings');
  expect(result.exitCode).toBe(0);
  expect(result.passed).toBe(1);
  expect(result.failed).toBe(0);
  expect(result.total).toBe(1);
  expect(result.stdoutMessages.match(/i-was-running/g).length).toBe(1);
});

test('(retries 1) should retry a failed test', async ({ runTest }) => {
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
          [PlaywrightTestMethod]
          public void Test()
          {
              Console.WriteLine("i-was-running");
              throw new Exception("i-was-broken");
          }
      }`,
      '.runsettings': `
      <?xml version="1.0" encoding="utf-8"?>
      <RunSettings>
        <Playwright>
          <Retries>1</Retries>
        </Playwright>
      </RunSettings>`,
  }, 'dotnet test --settings=.runsettings');
  expect(result.exitCode).toBe(1);
  expect(result.passed).toBe(0);
  expect(result.failed).toBe(2);
  expect(result.total).toBe(2);
  expect(result.stdoutMessages.match(/i-was-running/g).length).toBe(2);
  expect(new Set(result.results.TestDefinitions.UnitTest.map(test => test["@_name"]))).toEqual(new Set(["Test", "Test (retry #1)"]));
});
