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

test('(retries 0) should not retry a passed test', async ({ runTest }) => {
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
          [PlaywrightTest]
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
  expect(result.stdout.match(/i-was-running/g).length).toBe(1);
});

test('(retries 0) should not retry a failed test', async ({ runTest }) => {
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
          [PlaywrightTest]
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
  expect(result.rawStdout).toContain("i-was-broken")
  expect(result.rawStdout.match(/i-was-broken/g).length).toBe(1);
});

test('(retries 1) should not retry a passed test', async ({ runTest }) => {
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
          [PlaywrightTest]
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
  expect(result.stdout).toContain("i-was-running")
  expect(result.stdout.match(/i-was-running/g).length).toBe(1);
});

test('(retries 1) should retry a failed test', async ({ runTest }) => {
  test.fixme(true, "there should be two tests registered.")
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
          [PlaywrightTest]
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
  expect(result.stdout).toContain("i-was-running")
  expect(result.stdout.match(/i-was-running/g).length).toBe(1);
  expect(new Set(result.results.TestDefinitions.UnitTest.map(test => test["@_name"]))).toEqual(new Set(["Test", "Test (retry #1)"]));
});
