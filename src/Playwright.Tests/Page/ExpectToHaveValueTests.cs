/**
 * Copyright (c) Microsoft Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System.Text.RegularExpressions;
using Microsoft.Playwright.Tests.Fixtures;
using static Microsoft.Playwright.Assertions;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Page;

/// <summary>Port of tests/page/expect-to-have-value.spec.ts.</summary>
public class ExpectToHaveValueTests : UpstreamPageTest
{
    [UpstreamTest("EX-302 should work")]
    public async Task ShouldWork()
    {
        await Page.SetContentAsync("<input id=node></input>");
        var locator = Page.Locator("#node");
        await locator.FillAsync("Text content");
        await Expect(locator).ToHaveValueAsync("Text content");
    }

    [UpstreamTest("EX-303 should work with label")]
    public async Task ShouldWorkWithLabel()
    {
        await Page.SetContentAsync("<label><input></input></label>");
        await Page.Locator("label input").FillAsync("Text content");
        await Expect(Page.Locator("label")).ToHaveValueAsync("Text content");
    }

    [UpstreamTest("EX-304 should work with regex")]
    public async Task ShouldWorkWithRegex()
    {
        await Page.SetContentAsync("<input id=node></input>");
        var locator = Page.Locator("#node");
        await locator.FillAsync("Text content");
        await Expect(locator).ToHaveValueAsync(new Regex("Text"));
    }

    [UpstreamTest("EX-305 should support failure")]
    public async Task ShouldSupportFailure()
    {
        await Page.SetContentAsync("<input id=node></input>");
        var locator = Page.Locator("#node");
        await locator.FillAsync("Text content");
        var error = await Catch(() => Expect(locator).ToHaveValueAsync(new Regex("Text2"), new() { Timeout = 1000 }));
        Assert.That(StripAnsi(error.Message), Does.Contain("\"Text content\""));
    }

    public class ToHaveValuesWithMultiSelect : UpstreamPageTest
    {
        [UpstreamTest("EX-306 works with text")]
        public async Task WorksWithText()
        {
            await Page.SetContentAsync(@"
      <select multiple>
        <option value=""R"">Red</option>
        <option value=""G"">Green</option>
        <option value=""B"">Blue</option>
      </select>
    ");
            var locator = Page.Locator("select");
            await locator.SelectOptionAsync(new[] { "R", "G" });
            await Expect(locator).ToHaveValuesAsync(new[] { "R", "G" });
        }

        [UpstreamTest("EX-307 follows labels")]
        public async Task FollowsLabels()
        {
            await Page.SetContentAsync(@"
      <label for=""colors"">Pick a Color</label>
      <select id=""colors"" multiple>
        <option value=""R"">Red</option>
        <option value=""G"">Green</option>
        <option value=""B"">Blue</option>
      </select>
    ");
            var locator = Page.Locator("text=Pick a Color");
            await locator.SelectOptionAsync(new[] { "R", "G" });
            await Expect(locator).ToHaveValuesAsync(new[] { "R", "G" });
        }

        [UpstreamTest("EX-308 exact match with text failure")]
        [LocalFixme("dotnet: the message of ToHaveValuesAsync is \"Locator expected to have values '['R', 'G']'\\nBut was: '['RR', 'GG']'\"; upstream expects the diff lines '-   \"R\"' and '+   \"RR\"'")]
        public async Task ExactMatchWithTextFailure()
        {
            await Page.SetContentAsync(@"
      <select multiple>
        <option value=""RR"">Red</option>
        <option value=""GG"">Green</option>
      </select>
    ");
            var locator = Page.Locator("select");
            await locator.SelectOptionAsync(new[] { "RR", "GG" });
            var error = await Catch(() => Expect(locator).ToHaveValuesAsync(new[] { "R", "G" }, new() { Timeout = 1000 }));
            Assert.That(StripAnsi(error.Message), Does.Contain("-   \"R\""));
            Assert.That(StripAnsi(error.Message), Does.Contain("+   \"RR\""));
        }

        [UpstreamTest("EX-309 works with regex")]
        public async Task WorksWithRegex()
        {
            await Page.SetContentAsync(@"
      <select multiple>
        <option value=""R"">Red</option>
        <option value=""G"">Green</option>
        <option value=""B"">Blue</option>
      </select>
    ");
            var locator = Page.Locator("select");
            await locator.SelectOptionAsync(new[] { "R", "G" });
            await Expect(locator).ToHaveValuesAsync(new[] { new Regex("R"), new Regex("G") });
        }

        [UpstreamTest("EX-310 fails when items not selected")]
        [LocalFixme("dotnet: the message of ToHaveValuesAsync is \"Locator expected to have matching regex '['R', 'G']'\\nBut was: '['B']'\"; upstream expects the diff line '+   \"B\"'")]
        public async Task FailsWhenItemsNotSelected()
        {
            await Page.SetContentAsync(@"
      <select multiple>
        <option value=""R"">Red</option>
        <option value=""G"">Green</option>
        <option value=""B"">Blue</option>
      </select>
    ");
            var locator = Page.Locator("select");
            await locator.SelectOptionAsync(new[] { "B" });
            var error = await Catch(() => Expect(locator).ToHaveValuesAsync(new[] { new Regex("R"), new Regex("G") }, new() { Timeout = 1000 }));
            Assert.That(StripAnsi(error.Message), Does.Contain("+   \"B\""));
        }

        [UpstreamTest("EX-311 fails when multiple not specified")]
        public async Task FailsWhenMultipleNotSpecified()
        {
            await Page.SetContentAsync(@"
      <select>
        <option value=""R"">Red</option>
        <option value=""G"">Green</option>
        <option value=""B"">Blue</option>
      </select>
    ");
            var locator = Page.Locator("select");
            await locator.SelectOptionAsync(new[] { "B" });
            var error = await Catch(() => Expect(locator).ToHaveValuesAsync(new[] { new Regex("R"), new Regex("G") }, new() { Timeout = 1000 }));
            Assert.That(error.Message, Does.Contain("Not a select element with a multiple attribute"));
        }

        [UpstreamTest("EX-312 fails when not a select element")]
        public async Task FailsWhenNotASelectElement()
        {
            await Page.SetContentAsync(@"
      <input value=""foo"" />
    ");
            var locator = Page.Locator("input");
            var error = await Catch(() => Expect(locator).ToHaveValuesAsync(new[] { new Regex("R"), new Regex("G") }, new() { Timeout = 1000 }));
            Assert.That(error.Message, Does.Contain("Not a select element with a multiple attribute"));
        }
    }
}
