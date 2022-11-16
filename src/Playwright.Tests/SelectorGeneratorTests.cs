/*
 * MIT License
 *
 * Copyright (c) Microsoft Corporation.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
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

namespace Microsoft.Playwright.Tests;

public class SelectorGeneratorTests : PageTestEx
{
    [PlaywrightTest("selector-generator.spec.ts", "should use data-testid in strict errors")]
    public async Task ShouldUseDataTestIdInStrictErrors()
    {
        Playwright.Selectors.SetTestIdAttribute("data-custom-id");
        await Page.SetContentAsync(@"
      <div>
        <div></div>
        <div>
          <div></div>
          <div></div>
        </div>
      </div>
      <div>
        <div class='foo bar:0' data-custom-id='One'>
        </div>
        <div class='foo bar:1' data-custom-id='Two'>
        </div>
      </div>
    ");
        var error = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => Page.Locator(".foo").HoverAsync());
        StringAssert.Contains("strict mode violation", error.Message);
        StringAssert.Contains("<div class=\"foo bar:0", error.Message);
        StringAssert.Contains("<div class=\"foo bar:1", error.Message);
        StringAssert.Contains("aka GetByTestId(\"One\")", error.Message);
        StringAssert.Contains("aka GetByTestId(\"Two\")", error.Message);
    }
}
