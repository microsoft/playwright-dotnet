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

import { test, expect } from './baseTest';

test.use({ testMode: 'nunit.mtp' });

test('should configure NUnit fixtures through MTP', async ({ runTest }) => {
  const result = await runTest({
    'Tests.cs': `
      using System.Threading.Tasks;
      using Microsoft.Playwright;
      using Microsoft.Playwright.NUnit;
      using NUnit.Framework;

      public class <class-name> : PageTest
      {
          [Test]
          public async Task Test()
          {
              Assert.That(BrowserName, Is.EqualTo("firefox"));
              Assert.That(ContextOptions().ColorScheme, Is.EqualTo(ColorScheme.NoPreference));
              Assert.That(await Page.EvaluateAsync<string>("() => navigator.language"), Is.EqualTo("fr-FR"));
              await Page.SetContentAsync("<div data-mtp-id='target'>configured</div>");
              await Expect(Page.GetByTestId("target")).ToHaveTextAsync("configured");
          }
      }`,
    'testconfig.json': JSON.stringify({
      playwright: {
        browserName: 'firefox',
        testIdAttribute: 'data-mtp-id',
        contextOptions: { locale: 'fr-FR', colorScheme: 'no-preference' },
      },
    }),
  }, 'dotnet run', { BROWSER: undefined, HEADED: undefined, PWDEBUG: undefined, PW_INTERNAL_ADAPTER_SETTINGS: undefined });
  expect(result.exitCode).toBe(0);
  expect(result.passed).toBe(1);
  expect(result.total).toBe(1);
});
