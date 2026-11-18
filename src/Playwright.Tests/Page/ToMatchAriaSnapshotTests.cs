/**
 * Copyright (c) Microsoft Corporation. All rights reserved.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Microsoft.Playwright.Tests.Fixtures;
using static Microsoft.Playwright.Assertions;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Page;

/// <summary>Port of tests/page/to-match-aria-snapshot.spec.ts.</summary>
public class ToMatchAriaSnapshotTests : UpstreamPageTest
{
    [UpstreamTest("AR-107 should match page")]
    public async Task ShouldMatchPage()
    {
        await Page.SetContentAsync("<h1>title</h1>");
        await Expect(Page).ToMatchAriaSnapshotAsync(@"
    - heading ""title""
  ");
    }

    [UpstreamTest("AR-108 should match page complex")]
    public async Task ShouldMatchPageComplex()
    {
        await Page.SetContentAsync(@"
    <h1>Microsoft</h1>
    <div>Open source projects and samples from Microsoft</div>
    <ul>
      <li>
        <a href=""about:blank"">Playwright</a>
      </li>
    </ul>");

        await Expect(Page).ToMatchAriaSnapshotAsync(@"
    - heading ""Microsoft""
    - text: Open source projects and samples from Microsoft
    - list:
      - listitem:
        - link ""Playwright""
  ");
    }

    [UpstreamTest("AR-109 should match page with not")]
    public async Task ShouldMatchPageWithNot()
    {
        await Page.SetContentAsync("<h1>title</h1>");
        await Expect(Page).Not.ToMatchAriaSnapshotAsync(@"
    - heading ""wrong""
  ");
    }

    [UpstreamTest("AR-110 should fail page with timeout")]
    [LocalFixme("dotnet: the message of ToMatchAriaSnapshotAsync is \"Page expected to match Aria snapshot '...'\\nBut was: 'System.Dynamic.ExpandoObject' \\nCall log: ...\"")]
    public async Task ShouldFailPageWithTimeout()
    {
        await Page.SetContentAsync("<h1>title</h1>");
        var e = await Catch(() => Expect(Page).ToMatchAriaSnapshotAsync(@"
    - heading ""wrong""
  ", new() { Timeout = 2000 }));
        Assert.That(StripAnsi(e.Message), Does.Contain("expect(page).toMatchAriaSnapshot(expected) failed"));
        Assert.That(StripAnsi(e.Message), Does.Contain("Timeout:  2000ms"));
        Assert.That(StripAnsi(e.Message), Does.Not.Contain("Locator"));
    }

    [UpstreamTest("AR-111 should match")]
    public async Task ShouldMatch()
    {
        await Page.SetContentAsync("<h1>title</h1>");
        await Expect(Page).ToMatchAriaSnapshotAsync(@"
    - heading ""title""
  ");
    }

    [UpstreamTest("AR-112 should match in list")]
    public async Task ShouldMatchInList()
    {
        await Page.SetContentAsync(@"
    <h1>title</h1>
    <h1>title 2</h1>
  ");
        await Expect(Page).ToMatchAriaSnapshotAsync(@"
    - heading ""title""
  ");
    }

    [UpstreamTest("AR-113 should match list with accessible name")]
    public async Task ShouldMatchListWithAccessibleName()
    {
        await Page.SetContentAsync(@"
    <ul aria-label=""my list"">
      <li>one</li>
      <li>two</li>
    </ul>
  ");
        await Expect(Page).ToMatchAriaSnapshotAsync(@"
    - list ""my list"":
      - listitem: ""one""
      - listitem: ""two""
  ");
    }

    [UpstreamTest("AR-114 should match deep item")]
    public async Task ShouldMatchDeepItem()
    {
        await Page.SetContentAsync(@"
    <div>
      <h1>title</h1>
      <h1>title 2</h1>
    </div>
  ");
        await Expect(Page).ToMatchAriaSnapshotAsync(@"
    - heading ""title""
  ");
    }

    [UpstreamTest("AR-115 should match complex")]
    public async Task ShouldMatchComplex()
    {
        await Page.SetContentAsync(@"
    <ul>
      <li>
        <a href='about:blank'>link</a>
      </li>
    </ul>
  ");
        await Expect(Page).ToMatchAriaSnapshotAsync(@"
    - list:
      - listitem:
        - link ""link""
  ");
    }

    [UpstreamTest("AR-116 should match regex")]
    public async Task ShouldMatchRegex()
    {
        {
            await Page.SetContentAsync("<h1>Issues 12</h1>");
            // .NET: ${/Issues \d+/} is the text of the regular expression.
            await Expect(Page).ToMatchAriaSnapshotAsync(@"
      - heading /Issues \d+/
    ");
        }
        {
            await Page.SetContentAsync("<h1>Issues 1/2</h1>");
            // .NET: ${/Issues 1[/]2/} is the text of the regular expression.
            await Expect(Page).ToMatchAriaSnapshotAsync(@"
      - heading /Issues 1[/]2/
    ");
        }
        {
            await Page.SetContentAsync("<h1>Issues 1[</h1>");
            // .NET: ${/Issues 1\[/} is the text of the regular expression.
            await Expect(Page).ToMatchAriaSnapshotAsync(@"
      - heading /Issues 1\[/
    ");
        }
        {
            await Page.SetContentAsync("<h1>Issues 1]]2</h1>");
            // .NET: ${/Issues 1[\]]]2/} is the text of the regular expression.
            await Expect(Page).ToMatchAriaSnapshotAsync(@"
      - heading /Issues 1[\]]]2/
    ");
        }
    }

    [UpstreamTest("AR-117 should allow text nodes")]
    public async Task ShouldAllowTextNodes()
    {
        await Page.SetContentAsync(@"
    <h1>Microsoft</h1>
    <div>Open source projects and samples from Microsoft</div>
  ");

        await Expect(Page).ToMatchAriaSnapshotAsync(@"
    - heading ""Microsoft""
    - text: ""Open source projects and samples from Microsoft""
  ");
    }

    [UpstreamTest("AR-118 details visibility")]
    public async Task DetailsVisibility()
    {
        await Page.SetContentAsync(@"
    <details>
      <summary>Summary</summary>
      <div>Details</div>
    </details>
  ");

        await Expect(Page).ToMatchAriaSnapshotAsync(@"
    - group: ""Summary""
  ");
    }

    [UpstreamTest("AR-119 checked attribute")]
    [LocalFixme("dotnet: the message of ToMatchAriaSnapshotAsync is \"Page expected to match Aria snapshot '...'\\nBut was: 'System.Dynamic.ExpandoObject' \\nCall log: ...\"")]
    public async Task CheckedAttribute()
    {
        await Page.SetContentAsync(@"
    <input type='checkbox' checked />
  ");

        await Expect(Page).ToMatchAriaSnapshotAsync(@"
    - checkbox
  ");

        await Expect(Page).ToMatchAriaSnapshotAsync(@"
    - checkbox [checked]
  ");

        await Expect(Page).ToMatchAriaSnapshotAsync(@"
    - checkbox [checked=true]
  ");

        {
            var e = await Catch(() => Expect(Page).ToMatchAriaSnapshotAsync(@"
      - checkbox [checked=false]
    ", new() { Timeout = 1000 }));
            Assert.That(StripAnsi(e.Message), Does.Contain("expect(page).toMatchAriaSnapshot(expected) failed"));
            Assert.That(StripAnsi(e.Message), Does.Contain("Timeout:  1000ms"));
        }

        {
            var e = await Catch(() => Expect(Page).ToMatchAriaSnapshotAsync(@"
      - checkbox [checked=mixed]
    ", new() { Timeout = 1000 }));
            Assert.That(StripAnsi(e.Message), Does.Contain("expect(page).toMatchAriaSnapshot(expected) failed"));
            Assert.That(StripAnsi(e.Message), Does.Contain("Timeout:  1000ms"));
        }

        {
            var e = await Catch(() => Expect(Page).ToMatchAriaSnapshotAsync(@"
      - checkbox [checked=5]
    ", new() { Timeout = 1000 }));
            Assert.That(StripAnsi(e.Message), Does.Contain(" attribute must be a boolean or \"mixed\""));
        }
    }

    [UpstreamTest("AR-120 disabled attribute")]
    [LocalFixme("dotnet: the message of ToMatchAriaSnapshotAsync is \"Page expected to match Aria snapshot '...'\\nBut was: 'System.Dynamic.ExpandoObject' \\nCall log: ...\"")]
    public async Task DisabledAttribute()
    {
        await Page.SetContentAsync(@"
    <button disabled>Click me</button>
  ");

        await Expect(Page).ToMatchAriaSnapshotAsync(@"
    - button
  ");

        await Expect(Page).ToMatchAriaSnapshotAsync(@"
    - button [disabled]
  ");

        await Expect(Page).ToMatchAriaSnapshotAsync(@"
    - button [disabled=true]
  ");

        {
            var e = await Catch(() => Expect(Page).ToMatchAriaSnapshotAsync(@"
      - button [disabled=false]
    ", new() { Timeout = 1000 }));
            Assert.That(StripAnsi(e.Message), Does.Contain("expect(page).toMatchAriaSnapshot(expected) failed"));
            Assert.That(StripAnsi(e.Message), Does.Contain("Timeout:  1000ms"));
        }

        {
            var e = await Catch(() => Expect(Page).ToMatchAriaSnapshotAsync(@"
      - button [disabled=invalid]
    ", new() { Timeout = 1000 }));
            Assert.That(StripAnsi(e.Message), Does.Contain(" attribute must be a boolean"));
        }
    }

    [UpstreamTest("AR-121 expanded attribute")]
    [LocalFixme("dotnet: the message of ToMatchAriaSnapshotAsync is \"Page expected to match Aria snapshot '...'\\nBut was: 'System.Dynamic.ExpandoObject' \\nCall log: ...\"")]
    public async Task ExpandedAttribute()
    {
        await Page.SetContentAsync(@"
    <button aria-expanded=""true"">Toggle</button>
  ");

        await Expect(Page).ToMatchAriaSnapshotAsync(@"
    - button
  ");

        await Expect(Page).ToMatchAriaSnapshotAsync(@"
    - button [expanded]
  ");

        await Expect(Page).ToMatchAriaSnapshotAsync(@"
    - button [expanded=true]
  ");

        {
            var e = await Catch(() => Expect(Page).ToMatchAriaSnapshotAsync(@"
      - button [expanded=false]
    ", new() { Timeout = 1000 }));
            Assert.That(StripAnsi(e.Message), Does.Contain("expect(page).toMatchAriaSnapshot(expected) failed"));
            Assert.That(StripAnsi(e.Message), Does.Contain("Timeout:  1000ms"));
        }

        {
            var e = await Catch(() => Expect(Page).ToMatchAriaSnapshotAsync(@"
      - button [expanded=invalid]
    ", new() { Timeout = 1000 }));
            Assert.That(StripAnsi(e.Message), Does.Contain(" attribute must be a boolean"));
        }
    }

    [UpstreamTest("AR-122 level attribute")]
    [LocalFixme("dotnet: the message of ToMatchAriaSnapshotAsync is \"Page expected to match Aria snapshot '...'\\nBut was: 'System.Dynamic.ExpandoObject' \\nCall log: ...\"")]
    public async Task LevelAttribute()
    {
        await Page.SetContentAsync(@"
    <h2>Section Title</h2>
  ");

        await Expect(Page).ToMatchAriaSnapshotAsync(@"
    - heading
  ");

        await Expect(Page).ToMatchAriaSnapshotAsync(@"
    - heading [level=2]
  ");

        {
            var e = await Catch(() => Expect(Page).ToMatchAriaSnapshotAsync(@"
      - heading [level=3]
    ", new() { Timeout = 1000 }));
            Assert.That(StripAnsi(e.Message), Does.Contain("expect(page).toMatchAriaSnapshot(expected) failed"));
            Assert.That(StripAnsi(e.Message), Does.Contain("Timeout:  1000ms"));
        }

        {
            var e = await Catch(() => Expect(Page).ToMatchAriaSnapshotAsync(@"
      - heading [level=two]
    ", new() { Timeout = 1000 }));
            Assert.That(StripAnsi(e.Message), Does.Contain(" attribute must be a number"));
        }
    }

    [UpstreamTest("AR-123 pressed attribute")]
    [LocalFixme("dotnet: the message of ToMatchAriaSnapshotAsync is \"Page expected to match Aria snapshot '...'\\nBut was: 'System.Dynamic.ExpandoObject' \\nCall log: ...\"")]
    public async Task PressedAttribute()
    {
        await Page.SetContentAsync(@"
    <button aria-pressed=""true"">Like</button>
  ");

        await Expect(Page).ToMatchAriaSnapshotAsync(@"
    - button
  ");

        await Expect(Page).ToMatchAriaSnapshotAsync(@"
    - button [pressed]
  ");

        await Expect(Page).ToMatchAriaSnapshotAsync(@"
    - button [pressed=true]
  ");

        {
            var e = await Catch(() => Expect(Page).ToMatchAriaSnapshotAsync(@"
      - button [pressed=false]
    ", new() { Timeout = 1000 }));
            Assert.That(StripAnsi(e.Message), Does.Contain("expect(page).toMatchAriaSnapshot(expected) failed"));
            Assert.That(StripAnsi(e.Message), Does.Contain("Timeout:  1000ms"));
        }

        // Test for 'mixed' state
        await Page.SetContentAsync(@"
    <button aria-pressed=""mixed"">Like</button>
  ");

        await Expect(Page).ToMatchAriaSnapshotAsync(@"
    - button [pressed=mixed]
  ");

        {
            var e = await Catch(() => Expect(Page).ToMatchAriaSnapshotAsync(@"
      - button [pressed=true]
    ", new() { Timeout = 1000 }));
            Assert.That(StripAnsi(e.Message), Does.Contain("expect(page).toMatchAriaSnapshot(expected) failed"));
            Assert.That(StripAnsi(e.Message), Does.Contain("Timeout:  1000ms"));
        }

        {
            var e = await Catch(() => Expect(Page).ToMatchAriaSnapshotAsync(@"
      - button [pressed=5]
    ", new() { Timeout = 1000 }));
            Assert.That(StripAnsi(e.Message), Does.Contain(" attribute must be a boolean or \"mixed\""));
        }
    }

    [UpstreamTest("AR-124 selected attribute")]
    [LocalFixme("dotnet: the message of ToMatchAriaSnapshotAsync is \"Page expected to match Aria snapshot '...'\\nBut was: 'System.Dynamic.ExpandoObject' \\nCall log: ...\", without a diff of the two snapshots")]
    public async Task SelectedAttribute()
    {
        await Page.SetContentAsync(@"
    <table>
      <tr aria-selected=""true"">
        <td>Row</td>
      </tr>
      <tr>
        <td>Row 2</td>
      </tr>
    </table>
  ");

        await Expect(Page).ToMatchAriaSnapshotAsync(@"
    - row
    - row
  ");

        await Expect(Page).ToMatchAriaSnapshotAsync(@"
    - row [selected]
    - row
  ");

        await Expect(Page).ToMatchAriaSnapshotAsync(@"
    - row [selected=true]
    - row [selected=false]
  ");

        await Expect(Page.Locator("table")).ToMatchAriaSnapshotAsync(@"
    - table:
      - rowgroup:
        - row [selected=true]
        - row [selected=false]
  ");

        {
            var e = await Catch(() => Expect(Page).ToMatchAriaSnapshotAsync(@"
      - row [selected=false]
      - row [selected=false]
    ", new() { Timeout = 1000 }));
            Assert.That(StripAnsi(e.Message), Does.Contain(@"expect(page).toMatchAriaSnapshot(expected) failed

Timeout: 1000ms
- Expected  - 2
+ Received  + 6

- - row [selected=false]
- - row [selected=false]
+ - table:
+   - rowgroup:
+     - row ""Row"" [selected]:
+       - cell ""Row""
+     - row ""Row 2"":
+       - cell ""Row 2""

Call log:
"));
        }

        {
            var e = await Catch(() => Expect(Page).ToMatchAriaSnapshotAsync(@"
      - row [selected=invalid]
    ", new() { Timeout = 1000 }));
            Assert.That(StripAnsi(e.Message), Does.Contain(" attribute must be a boolean"));
        }
    }

    [UpstreamTest("AR-125 integration test")]
    public async Task IntegrationTest()
    {
        await Page.SetContentAsync(@"
    <h1>Microsoft</h1>
    <div>Open source projects and samples from Microsoft</div>
    <ul>
      <li>
        <details>
          <summary>
            Verified
          </summary>
          <div>
            <div>
              <p>
                We've verified that the organization <strong>microsoft</strong> controls the domain:
              </p>
              <ul>
                <li class=""mb-1"">
                  <strong>opensource.microsoft.com</strong>
                </li>
              </ul>
              <div>
                <a href=""about: blank"">Learn more about verified organizations</a>
              </div>
            </div>
          </div>
        </details>
      </li>
      <li>
        <a href=""about:blank"">
          <summary title=""Label: GitHub Sponsor"">Sponsor</summary>
        </a>
      </li>
    </ul>");

        await Expect(Page).ToMatchAriaSnapshotAsync(@"
    - heading ""Microsoft""
    - text: Open source projects and samples from Microsoft
    - list:
      - listitem:
        - group: Verified
      - listitem:
        - link ""Sponsor""
  ");
    }

    [UpstreamTest("AR-126 integration test 2")]
    public async Task IntegrationTest2()
    {
        await Page.SetContentAsync(@"
    <div>
      <header>
        <h1>todos</h1>
        <input placeholder=""What needs to be done?"">
      </header>
    </div>");
        await Expect(Page).ToMatchAriaSnapshotAsync(@"
    - heading ""todos""
    - textbox ""What needs to be done?""
  ");
    }

    [UpstreamTest("AR-127 expected formatter")]
    [LocalFixme("dotnet: the message of ToMatchAriaSnapshotAsync is \"Page expected to match Aria snapshot '...'\\nBut was: 'System.Dynamic.ExpandoObject' \\nCall log: ...\", without a diff of the two snapshots")]
    public async Task ExpectedFormatter()
    {
        await Page.SetContentAsync(@"
    <div>
      <header>
        <h1>todos</h1>
        <input placeholder=""What needs to be done?"">
        <button>Time 15:30</button>
      </header>
    </div>");
        var error = await Catch(() => Expect(Page).ToMatchAriaSnapshotAsync(@"
    - heading ""todos""
    - textbox ""Wrong text""
  ", new() { Timeout = 1 }));

        // Note that error message should not contain any regular expressions,
        // unlike the baseline generated by --update-snapshots.
        Assert.That(StripAnsi(error.Message), Does.Contain(@"expect(page).toMatchAriaSnapshot(expected) failed

Timeout: 1ms
- Expected  - 2
+ Received  + 4

- - heading ""todos""
- - textbox ""Wrong text""
+ - banner:
+   - heading ""todos"" [level=1]
+   - textbox ""What needs to be done?""
+   - button ""Time 15:30""

Call log:
"));
    }

    [UpstreamTest("AR-128 should unpack escaped names")]
    public async Task ShouldUnpackEscapedNames()
    {
        {
            await Page.SetContentAsync(@"
      <button>Click: me</button>
    ");
            await Expect(Page).ToMatchAriaSnapshotAsync(@"
      - 'button ""Click: me""'
    ");
            await Expect(Page).ToMatchAriaSnapshotAsync(@"
      - 'button /Click: me/'
    ");
        }

        {
            await Page.SetContentAsync(@"
      <button>Click / me</button>
    ");
            await Expect(Page).ToMatchAriaSnapshotAsync(@"
      - button ""Click / me""
    ");
            await Expect(Page).ToMatchAriaSnapshotAsync(@"
      - button /Click \/ me/
    ");
            await Expect(Page).ToMatchAriaSnapshotAsync(@"
      - 'button /Click \/ me/'
    ");
        }

        {
            await Page.SetContentAsync(@"
      <button>Click "" me</button>
    ");
            await Expect(Page).ToMatchAriaSnapshotAsync(@"
      - button ""Click \"" me""
    ");
            await Expect(Page).ToMatchAriaSnapshotAsync(@"
      - button /Click "" me/
    ");
            await Expect(Page).ToMatchAriaSnapshotAsync(@"
      - button /Click \"" me/
    ");
        }

        {
            await Page.SetContentAsync(@"
      <button>Click \ me</button>
    ");
            await Expect(Page).ToMatchAriaSnapshotAsync(@"
      - button ""Click \\ me""
    ");
            await Expect(Page).ToMatchAriaSnapshotAsync(@"
      - button /Click \\ me/
    ");
            await Expect(Page).ToMatchAriaSnapshotAsync(@"
      - 'button /Click \\ me/'
    ");
        }

        {
            await Page.SetContentAsync(@"
      <button>Click ' me</button>
    ");
            await Expect(Page).ToMatchAriaSnapshotAsync(@"
      - 'button ""Click '' me""'
    ");
        }

        {
            await Page.SetContentAsync(@"
      <h1>heading ""name"" [level=1]</h1>
    ");
            await Expect(Page).ToMatchAriaSnapshotAsync(@"
      - heading ""heading \""name\"" [level=1]"" [level=1]
    ");
        }

        {
            await Page.SetContentAsync(@"
      <h1>heading \"" [level=2]</h1>
    ");
            await Expect(Page).ToMatchAriaSnapshotAsync(@"
      - |
          heading    ""heading \\\"" [level=2]"" [
             level  =   1   ]
    ");
        }
    }

    [UpstreamTest("AR-129 should report error in YAML")]
    [LocalFixme("dotnet: the message of ToMatchAriaSnapshotAsync is \"Page expected to match Aria snapshot\\nError: Aria snapshot must be a YAML sequence, elements starting with \" -\" '...'\\nBut was: 'null' \\nCall log: ...\"")]
    public async Task ShouldReportErrorInYAML()
    {
        await Page.SetContentAsync("<h1>title</h1>");

        {
            var error = await Catch(() => Expect(Page).ToMatchAriaSnapshotAsync(@"
      heading ""title""
    "));
            Assert.That(StripAnsi(error.Message), Is.EqualTo(@"expect(page).toMatchAriaSnapshot(expected) failed

Expected: ""heading \""title\""""
Error: Aria snapshot must be a YAML sequence, elements starting with "" -""

Call log:
  - Expect ""toMatchAriaSnapshot"" with timeout 10000ms
"));
        }

        {
            var error = await Catch(() => Expect(Page).ToMatchAriaSnapshotAsync(@"
      - heading: a:
    "));
            Assert.That(StripAnsi(error.Message), Is.EqualTo(@"expect(page).toMatchAriaSnapshot(expected) failed

Expected: ""- heading: a:""
Error: Nested mappings are not allowed in compact mappings at line 1, column 12:

- heading: a:
           ^

Call log:
  - Expect ""toMatchAriaSnapshot"" with timeout 10000ms
"));
        }
    }

    [UpstreamTest("AR-130 should report error in YAML keys")]
    [LocalFixme("dotnet: the message of ToMatchAriaSnapshotAsync is \"Page expected to match Aria snapshot\\nError: Unterminated string: ... '...'\\nBut was: 'null' \\nCall log: ...\"")]
    public async Task ShouldReportErrorInYAMLKeys()
    {
        await Page.SetContentAsync("<h1>title</h1>");

        {
            var error = await Catch(() => Expect(Page).ToMatchAriaSnapshotAsync(@"
      - heading ""title
    "));
            Assert.That(StripAnsi(error.Message), Is.EqualTo(@"expect(page).toMatchAriaSnapshot(expected) failed

Expected: ""- heading \""title""
Error: Unterminated string:

heading ""title
              ^

Call log:
  - Expect ""toMatchAriaSnapshot"" with timeout 10000ms
"));
        }

        {
            var error = await Catch(() => Expect(Page).ToMatchAriaSnapshotAsync(@"
      - heading /title
    "));
            Assert.That(StripAnsi(error.Message), Is.EqualTo(@"expect(page).toMatchAriaSnapshot(expected) failed

Expected: ""- heading /title""
Error: Unterminated regex:

heading /title
              ^

Call log:
  - Expect ""toMatchAriaSnapshot"" with timeout 10000ms
"));
        }

        {
            var error = await Catch(() => Expect(Page).ToMatchAriaSnapshotAsync(@"
      - heading [level=a]
    "));
            Assert.That(StripAnsi(error.Message), Is.EqualTo(@"expect(page).toMatchAriaSnapshot(expected) failed

Expected: ""- heading [level=a]""
Error: Value of ""level"" attribute must be a number:

heading [level=a]
               ^

Call log:
  - Expect ""toMatchAriaSnapshot"" with timeout 10000ms
"));
        }

        {
            var error = await Catch(() => Expect(Page).ToMatchAriaSnapshotAsync(@"
      - heading [expanded=FALSE]
    "));
            Assert.That(StripAnsi(error.Message), Is.EqualTo(@"expect(page).toMatchAriaSnapshot(expected) failed

Expected: ""- heading [expanded=FALSE]""
Error: Value of ""expanded"" attribute must be a boolean:

heading [expanded=FALSE]
                  ^

Call log:
  - Expect ""toMatchAriaSnapshot"" with timeout 10000ms
"));
        }

        {
            var error = await Catch(() => Expect(Page).ToMatchAriaSnapshotAsync(@"
      - heading [checked=foo]
    "));
            Assert.That(StripAnsi(error.Message), Is.EqualTo(@"expect(page).toMatchAriaSnapshot(expected) failed

Expected: ""- heading [checked=foo]""
Error: Value of ""checked"" attribute must be a boolean or ""mixed"":

heading [checked=foo]
                 ^

Call log:
  - Expect ""toMatchAriaSnapshot"" with timeout 10000ms
"));
        }

        {
            var error = await Catch(() => Expect(Page).ToMatchAriaSnapshotAsync(@"
      - heading [level=]
    "));
            Assert.That(StripAnsi(error.Message), Is.EqualTo(@"expect(page).toMatchAriaSnapshot(expected) failed

Expected: ""- heading [level=]""
Error: Value of ""level"" attribute must be a number:

heading [level=]
               ^

Call log:
  - Expect ""toMatchAriaSnapshot"" with timeout 10000ms
"));
        }

        {
            var error = await Catch(() => Expect(Page).ToMatchAriaSnapshotAsync(@"
      - heading [bogus]
    "));
            Assert.That(StripAnsi(error.Message), Is.EqualTo(@"expect(page).toMatchAriaSnapshot(expected) failed

Expected: ""- heading [bogus]""
Error: Unsupported attribute [bogus]:

heading [bogus]
         ^

Call log:
  - Expect ""toMatchAriaSnapshot"" with timeout 10000ms
"));
        }

        {
            var error = await Catch(() => Expect(Page).ToMatchAriaSnapshotAsync(@"
      - heading invalid
    "));
            Assert.That(StripAnsi(error.Message), Is.EqualTo(@"expect(page).toMatchAriaSnapshot(expected) failed

Expected: ""- heading invalid""
Error: Unexpected input:

heading invalid
        ^

Call log:
  - Expect ""toMatchAriaSnapshot"" with timeout 10000ms
"));
        }
    }

    [UpstreamTest("AR-131 call log should contain actual snapshot")]
    public async Task CallLogShouldContainActualSnapshot()
    {
        await Page.SetContentAsync("<h1>todos</h1>");
        var error = await Catch(() => Expect(Page).ToMatchAriaSnapshotAsync(@"
    - heading ""wrong""
  ", new() { Timeout = 3000 }));

        Assert.That(StripAnsi(error.Message), Does.Contain("unexpected value \"- heading \"todos\" [level=1]\""));
    }

    [UpstreamTest("AR-132 should parse attributes")]
    public async Task ShouldParseAttributes()
    {
        {
            await Page.SetContentAsync(@"
      <button aria-pressed=""mixed"">hello world</button>
    ");
            await Expect(Page).ToMatchAriaSnapshotAsync(@"
      - button [pressed=mixed ]
    ");
        }

        {
            await Page.SetContentAsync(@"
      <h2>hello world</h2>
    ");
            await Expect(Page.Locator("body")).Not.ToMatchAriaSnapshotAsync(@"
      - heading [level =  -3 ]
    ");
        }
    }

    [UpstreamTest("AR-133 should not unshift actual template text")]
    [LocalFixme("dotnet: ToMatchAriaSnapshotAsync sends the expected snapshot as written, upstream removes the common indentation of its lines first: the message has \"        - heading \"title\" [level=1]\\n    - heading \"title 2\" [level=1]\"")]
    public async Task ShouldNotUnshiftActualTemplateText()
    {
        await Page.SetContentAsync(@"
    <h1>title</h1>
    <h1>title 2</h1>
  ");
        var error = await Catch(() => Expect(Page).ToMatchAriaSnapshotAsync(@"
        - heading ""title"" [level=1]
    - heading ""title 2"" [level=1]
  ", new() { Timeout = 1000 }));
        Assert.That(StripAnsi(error.Message), Does.Contain(@"
    - heading ""title"" [level=1]
- heading ""title 2"" [level=1]"));
    }

    [UpstreamTest("AR-134 should not match what is not matched")]
    [LocalFixme("dotnet: the message of ToMatchAriaSnapshotAsync is \"Page expected to match Aria snapshot '...'\\nBut was: 'System.Dynamic.ExpandoObject' \\nCall log: ...\", without a diff of the two snapshots")]
    public async Task ShouldNotMatchWhatIsNotMatched()
    {
        await Page.SetContentAsync("<p>Text</p>");
        var error = await Catch(() => Expect(Page).ToMatchAriaSnapshotAsync(@"
    - paragraph:
      - button ""bogus""
  "));
        Assert.That(StripAnsi(error.Message), Does.Contain(@"
- - paragraph:
-   - button ""bogus""
+ - paragraph: Text"));
    }

    [UpstreamTest("AR-135 should match url")]
    public async Task ShouldMatchUrl()
    {
        await Page.SetContentAsync(@"
    <a href='https://example.com'>Link</a>
  ");

        await Expect(Page).ToMatchAriaSnapshotAsync(@"
    - link:
      - /url: /.*example.com/
  ");
    }

    [UpstreamTest("AR-136 should detect unexpected children: equal")]
    [LocalFixme("dotnet: the message of ToMatchAriaSnapshotAsync is \"Page expected to match Aria snapshot '...'\\nBut was: 'System.Dynamic.ExpandoObject' \\nCall log: ...\", without a diff of the two snapshots")]
    public async Task ShouldDetectUnexpectedChildrenEqual()
    {
        await Page.SetContentAsync(@"
    <ul>
      <li>One</li>
      <li>Two</li>
      <li>Three</li>
    </ul>
  ");

        await Expect(Page).ToMatchAriaSnapshotAsync(@"
    - list:
      - listitem: ""One""
      - listitem: ""Three""
  ");

        var e = await Catch(() => Expect(Page).ToMatchAriaSnapshotAsync(@"
    - list:
      - /children: equal
      - listitem: ""One""
      - listitem: ""Three""
  ", new() { Timeout = 1000 }));

        Assert.That(StripAnsi(e.Message), Does.Contain("expect(page).toMatchAriaSnapshot(expected) failed"));
        Assert.That(StripAnsi(e.Message), Does.Contain("+   - listitem: Two"));
    }

    [UpstreamTest("AR-137 should detect unexpected children: deep-equal")]
    [LocalFixme("dotnet: the message of ToMatchAriaSnapshotAsync is \"Page expected to match Aria snapshot '...'\\nBut was: 'System.Dynamic.ExpandoObject' \\nCall log: ...\", without a diff of the two snapshots")]
    public async Task ShouldDetectUnexpectedChildrenDeepEqual()
    {
        await Page.SetContentAsync(@"
    <ul>
      <li>
        <ul>
          <li>1.1</li>
          <li>1.2</li>
        </ul>
      </li>
    </ul>
  ");

        await Expect(Page).ToMatchAriaSnapshotAsync(@"
    - list:
      - listitem:
        - list:
          - listitem: 1.1
  ");

        await Expect(Page).ToMatchAriaSnapshotAsync(@"
    - list:
      - /children: equal
      - listitem:
        - list:
          - listitem: 1.1
  ");

        var e = await Catch(() => Expect(Page).ToMatchAriaSnapshotAsync(@"
    - list:
      - /children: deep-equal
      - listitem:
        - list:
          - listitem: 1.1
  ", new() { Timeout = 1000 }));

        Assert.That(StripAnsi(e.Message), Does.Contain("expect(page).toMatchAriaSnapshot(expected) failed"));
        Assert.That(StripAnsi(e.Message), Does.Contain("+       - listitem: \"1.2\""));
    }

    [UpstreamTest("AR-138 should allow restoring contain mode inside deep-equal")]
    [LocalFixme("dotnet: the message of ToMatchAriaSnapshotAsync is \"Page expected to match Aria snapshot '...'\\nBut was: 'System.Dynamic.ExpandoObject' \\nCall log: ...\", without a diff of the two snapshots")]
    public async Task ShouldAllowRestoringContainModeInsideDeepEqual()
    {
        await Page.SetContentAsync(@"
    <ul>
      <li>
        <ul>
          <li>1.1</li>
          <li>1.2</li>
        </ul>
      </li>
    </ul>
  ");

        var e = await Catch(() => Expect(Page).ToMatchAriaSnapshotAsync(@"
    - list:
      - /children: deep-equal
      - listitem:
        - list:
          - listitem: 1.1
  ", new() { Timeout = 1000 }));

        Assert.That(StripAnsi(e.Message), Does.Contain("expect(page).toMatchAriaSnapshot(expected) failed"));
        Assert.That(StripAnsi(e.Message), Does.Contain("+       - listitem: \"1.2\""));

        await Expect(Page).ToMatchAriaSnapshotAsync(@"
    - list:
      - /children: deep-equal
      - listitem:
        - list:
          - /children: contain
          - listitem: 1.1
  ");
    }

    [UpstreamTest("AR-139 top-level deep-equal")]
    [LocalFixme("dotnet: the message of ToMatchAriaSnapshotAsync is \"Page expected to match Aria snapshot '...'\\nBut was: 'System.Dynamic.ExpandoObject' \\nCall log: ...\", without a diff of the two snapshots")]
    public async Task TopLevelDeepEqual()
    {
        await Page.SetContentAsync(@"
    <ul>
      <li>
        <ul>
          <li>1.1</li>
          <li>1.2</li>
        </ul>
      </li>
    </ul>
  ");

        var error = await Catch(() => Expect(Page).ToMatchAriaSnapshotAsync(@"
    - /children: deep-equal
    - list
  ", new() { Timeout = 1000 }));

        Assert.That(StripAnsi(error.Message), Does.Contain(@"
- - /children: deep-equal
- - list
+ - list:
+   - listitem:
+     - list:
+       - listitem: ""1.1""
+       - listitem: ""1.2""
  ".Trim()));
    }

    [UpstreamTest("AR-140 generated snapshot includes text children when name is longer than text")]
    public async Task GeneratedSnapshotIncludesTextChildrenWhenNameIsLongerThanText()
    {
        await Page.SetContentAsync(@"
    <section>
      <div role=""progressbar"" aria-label=""Alpha Beta"" aria-valuenow=""7"" aria-valuemin=""0"" aria-valuemax=""10"">
        <span>Alpha</span>
        <span>7</span>
      </div>
    </section>
  ");
        await Expect(Page.Locator("section")).ToMatchAriaSnapshotAsync(@"
    - /children: deep-equal
    - progressbar ""Alpha Beta"": Alpha 7
  ");
    }

    [UpstreamTest("AR-141 treat bad regex as a string")]
    [LocalFixme("dotnet: the message of ToMatchAriaSnapshotAsync is \"Page expected to match Aria snapshot '...'\\nBut was: 'System.Dynamic.ExpandoObject' \\nCall log: ...\", without a diff of the two snapshots")]
    public async Task TreatBadRegexAsAString()
    {
        await Page.SetContentAsync("<a href=\"/foo\">Log in</a>");
        var error = await Catch(() => Expect(Page).ToMatchAriaSnapshotAsync(@"
    - link ""Log in"":
      - /url: /[a/
  ", new() { Timeout = 1 }));
        Assert.That(StripAnsi(error.Message), Does.Contain("expect(page).toMatchAriaSnapshot(expected) failed"));
        Assert.That(StripAnsi(error.Message), Does.Contain("-   - /url: /[a/"));
        Assert.That(StripAnsi(error.Message), Does.Contain("+   - /url: /foo"));
    }

    [UpstreamTest("AR-142 invalid attribute")]
    [LocalFixme("dotnet: the message of ToMatchAriaSnapshotAsync is \"Page expected to match Aria snapshot '...'\\nBut was: 'System.Dynamic.ExpandoObject' \\nCall log: ...\", without the received snapshot")]
    public async Task InvalidAttribute()
    {
        await Page.SetContentAsync(@"
    <input type=""text"" aria-label=""Email"" aria-invalid=""true"" value=""not-an-email"">
    <input type=""text"" aria-label=""Name"" value=""Alice"">
  ");

        await Expect(Page).ToMatchAriaSnapshotAsync(@"
    - textbox ""Email"" [invalid]: not-an-email
    - textbox ""Name"": Alice
  ");

        await Expect(Page).ToMatchAriaSnapshotAsync(@"
    - textbox ""Email"" [invalid=true]: not-an-email
    - textbox ""Name"" [invalid=false]: Alice
  ");

        // `aria-invalid="grammar"` and `aria-invalid="spelling"` surface their underlying value.
        await Page.SetContentAsync(@"
    <input type=""text"" aria-label=""Bio"" aria-invalid=""grammar"">
    <input type=""text"" aria-label=""Note"" aria-invalid=""spelling"">
    <input type=""text"" aria-label=""Mixed-case grammar"" aria-invalid=""Grammar"">
    <input type=""text"" aria-label=""Mixed-case spelling"" aria-invalid=""Spelling"">
  ");
        await Expect(Page).ToMatchAriaSnapshotAsync(@"
    - textbox ""Bio"" [invalid=grammar]
    - textbox ""Note"" [invalid=spelling]
    - textbox ""Mixed-case grammar"" [invalid=grammar]
    - textbox ""Mixed-case spelling"" [invalid=spelling]
  ");

        // A `grammar` value is distinct from a plain `true` invalid state.
        var error = await Catch(() => Expect(Page).ToMatchAriaSnapshotAsync(@"
    - textbox ""Bio"" [invalid]
  ", new() { Timeout = 1 }));
        Assert.That(StripAnsi(error.Message), Does.Contain("[invalid=grammar]"));

        // Any other non-false value falls back to `true`.
        await Page.SetContentAsync("<input type=\"text\" aria-label=\"Zip\" aria-invalid=\"garbage\">");
        await Expect(Page).ToMatchAriaSnapshotAsync(@"
    - textbox ""Zip"" [invalid]
  ");
    }
}
