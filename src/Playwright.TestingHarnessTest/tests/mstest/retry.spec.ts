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

test('should not retry a passed test with retries: 0', async ({ runTest }) => {
  const result = await runTest({
    'ExampleTests.cs': `
      using System;
      using Microsoft.Playwright.MSTest;
      using Microsoft.VisualStudio.TestTools.UnitTesting;

      namespace Playwright.TestingHarnessTest.MSTest;

      [TestClass]  
      public class <class-name> : PageTest
      {
          [PlaywrightTestMethod]
          public void MyTest()
          {
              Console.WriteLine("i-was-running");
          }
      }`,
  }, 'dotnet test');
  expect(result.exitCode).toBe(0);
  expect(result.passed).toBe(1);
  expect(result.failed).toBe(0);
  expect(result.total).toBe(1);
  expect(result.stdout.match(/i-was-running/g).length).toBe(1);
  expect(result.rawStdout).toMatch(/Passed MyTest \[/);
});

test('should not retry a failed test with retries: 0', async ({ runTest }) => {
  const result = await runTest({
    'ExampleTests.cs': `
      using System;
      using Microsoft.Playwright.MSTest;
      using Microsoft.VisualStudio.TestTools.UnitTesting;

      namespace Playwright.TestingHarnessTest.MSTest;

      [TestClass]  
      public class <class-name> : PageTest
      {
          [PlaywrightTestMethod]
          public void MyTest()
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
  expect(result.stdout.match(/i-was-running/g).length).toBe(1);
  expect(result.rawStdout).toMatch(/Failed MyTest \[/);
});

test('should not retry a passed Mytest with retries: 1', async ({ runTest }) => {
  const result = await runTest({
    'ExampleTests.cs': `
      using System;
      using Microsoft.Playwright.MSTest;
      using Microsoft.VisualStudio.TestTools.UnitTesting;

      namespace Playwright.TestingHarnessTest.MSTest;

      [TestClass]  
      public class <class-name> : PageTest
      {
          [PlaywrightTestMethod]
          public void MyTest()
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
  expect(result.stdout.match(/i-was-running/g).length).toBe(1);
  expect(result.rawStdout).toMatch(/Passed MyTest \[/);
});

test('should retry a failed test with retries: 1', async ({ runTest }) => {
  const result = await runTest({
    'ExampleTests.cs': `
      using System;
      using Microsoft.Playwright.MSTest;
      using Microsoft.VisualStudio.TestTools.UnitTesting;

      namespace Playwright.TestingHarnessTest.MSTest;

      [TestClass]  
      public class <class-name> : PageTest
      {
          [PlaywrightTestMethod]
          public void MyTest()
          {
              Console.Error.WriteLine("i-was-running");
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
  expect(result.failed).toBe(1);
  expect(result.total).toBe(1);
  expect(result.stdout).toContain('Test was retried 1 time.');
  expect(result.rawStdout).toMatch(/Failed MyTest \[/);
});

test('should retry a failed test and stop once it passed', async ({ runTest }) => {
  const result = await runTest({
    'ExampleTests.cs': `
      using System;
      using Microsoft.Playwright.MSTest;
      using Microsoft.VisualStudio.TestTools.UnitTesting;

      namespace Playwright.TestingHarnessTest.MSTest;

      [TestClass]  
      public class <class-name> : PageTest
      {
          static int retries = 0;

          [PlaywrightTestMethod]
          public void MyTest()
          {
              Console.Error.WriteLine("i-was-running" + retries);
              retries++;
              if (retries < 5)
                throw new Exception("i-was-broken");
          }
      }`,
      '.runsettings': `
      <?xml version="1.0" encoding="utf-8"?>
      <RunSettings>
        <Playwright>
          <Retries>10</Retries>
        </Playwright>
      </RunSettings>`,
  }, 'dotnet test --settings=.runsettings');
  expect(result.exitCode).toBe(0);
  expect(result.passed).toBe(1);
  expect(result.failed).toBe(0);
  expect(result.total).toBe(1);
  expect(result.stdout).toContain('Test was retried 4 times.');
  expect(result.rawStdout).toMatch(/Passed MyTest \[/);
});

test('should retry on timeout and print the click api call which was pending', async ({ runTest }) => {
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
          static int retries = 0;

          [PlaywrightTestMethod]
          public async Task MyTest()
          {
              Console.WriteLine("@marker-running-spec");
              retries++;
              if (retries < 3)
                await Page.ClickAsync("button");
          }

          [TestInitialize]
          public void TestInitialize()
          {
            Console.WriteLine("@marker-initialize");
          }

          [TestCleanup]
          public void TestCleanup()
          {
            Console.WriteLine("@marker-cleanup");
          }
      }`,
      '.runsettings': `
      <?xml version="1.0" encoding="utf-8"?>
      <RunSettings>
        <Playwright>
          <TestTimeout>5000</TestTimeout>
          <Retries>5</Retries>
        </Playwright>
      </RunSettings>`,
  }, 'dotnet test --settings=.runsettings');
  expect(result.exitCode).toBe(0);
  expect(result.passed).toBe(1);
  expect(result.failed).toBe(0);
  expect(result.total).toBe(1);
  const markers = result.stdout.split("\n").map(line => line.trim()).filter(line => line.startsWith('@marker'));
  expect(markers).toEqual([
    "@marker-initialize",
    "@marker-running-spec",
    "@marker-cleanup",
    "@marker-initialize",
    "@marker-running-spec",
    "@marker-cleanup",
    "@marker-initialize",
    "@marker-running-spec",
    "@marker-cleanup",
  ]);
  expect(result.stdout.match(/waiting for "Locator\("button"\)"/g).length).toBe(4);
  expect(result.stdout).toContain('Test was retried 2 times.');
  expect(result.rawStdout).toMatch(/Passed MyTest \[/);
});

test('should retry on timeout if the method is not aborting on timeout', async ({ runTest }) => {
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
          static int retries = 0;

          [PlaywrightTestMethod]
          public async Task MyTest()
          {
              Console.WriteLine("@marker-running-spec");
              retries++;
              if (retries < 3)
                await Task.Delay(-1);
          }

          [TestInitialize]
          public void TestInitialize()
          {
            Console.WriteLine("@marker-initialize");
          }

          [TestCleanup]
          public void TestCleanup()
          {
            Console.WriteLine("@marker-cleanup");
          }
      }`,
      '.runsettings': `
      <?xml version="1.0" encoding="utf-8"?>
      <RunSettings>
        <Playwright>
          <TestTimeout>5000</TestTimeout>
          <Retries>5</Retries>
        </Playwright>
      </RunSettings>`,
  }, 'dotnet test --settings=.runsettings');
  expect(result.exitCode).toBe(0);
  expect(result.passed).toBe(1);
  expect(result.failed).toBe(0);
  expect(result.total).toBe(1);
  const markers = result.stdout.split("\n").map(line => line.trim()).filter(line => line.startsWith('@marker'));
  expect(markers).toEqual([
    "@marker-initialize",
    "@marker-running-spec",
    "@marker-initialize",
    "@marker-running-spec",
    "@marker-initialize",
    "@marker-running-spec",
    "@marker-cleanup",
  ]);
  expect(result.stdout).toContain('Test was retried 2 times.');
  expect(result.stdout.match(/Test timed out after 5000ms./g)).toHaveLength(2);
  expect(result.rawStdout).toMatch(/Passed MyTest \[/);
});

test('should throw if the MSTest timeout < Playwright TestTimeout', async ({ runTest }) => {
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
          public void MyTest()
          {
              Console.Error.WriteLine("@marker-running-spec");
          }
      }`,
      '.runsettings': `
      <?xml version="1.0" encoding="utf-8"?>
      <RunSettings>
        <Playwright>
          <TestTimeout>10000</TestTimeout>
          <Retries>1</Retries>
        </Playwright>
        <MSTest>
          <TestTimeout>5000</TestTimeout>
        </MSTest>
      </RunSettings>`,
  }, 'dotnet test --settings=.runsettings');
  expect(result.exitCode).toBe(1);
  expect(result.passed).toBe(0);
  expect(result.failed).toBe(1);
  expect(result.total).toBe(1);
  expect(result.rawStdout).toMatch(/Playwright test timeout \(10000ms\) is larger than MSTest timeout \(5000ms\)\./);
  expect(result.rawStdout).not.toContain('@marker-running-spec');
});
