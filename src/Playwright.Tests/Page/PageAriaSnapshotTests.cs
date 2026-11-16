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

using Microsoft.Playwright.Tests.Fixtures;
using static Microsoft.Playwright.Assertions;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Page;

/// <summary>Port of tests/page/page-aria-snapshot.spec.ts.</summary>
public class PageAriaSnapshotTests
{
    private static async Task CheckAndMatchSnapshot(ILocator locator, string snapshot)
    {
        Assert.That(await locator.AriaSnapshotAsync(), Is.EqualTo(Unshift(snapshot)));
        await Expect(locator).ToMatchAriaSnapshotAsync(snapshot);
    }

    [UpstreamTest("AR-001 should snapshot")]
    public async Task ShouldSnapshot(IPage page)
    {
        await page.SetContentAsync("<h1>title</h1>");
        await CheckAndMatchSnapshot(page.Locator("body"), @"
    - heading ""title"" [level=1]
  ");
    }

    [UpstreamTest("AR-002 should snapshot list")]
    public async Task ShouldSnapshotList(IPage page)
    {
        await page.SetContentAsync(@"
    <h1>title</h1>
    <h1>title 2</h1>
  ");
        await CheckAndMatchSnapshot(page.Locator("body"), @"
    - heading ""title"" [level=1]
    - heading ""title 2"" [level=1]
  ");
    }

    [UpstreamTest("AR-003 should snapshot list with accessible name")]
    public async Task ShouldSnapshotListWithAccessibleName(IPage page)
    {
        await page.SetContentAsync(@"
    <ul aria-label=""my list"">
      <li>one</li>
      <li>two</li>
    </ul>
  ");
        await CheckAndMatchSnapshot(page.Locator("body"), @"
    - list ""my list"":
      - listitem: one
      - listitem: two
  ");
    }

    [UpstreamTest("AR-004 should quote names that look like regexes")]
    public async Task ShouldQuoteNamesThatLookLikeRegexes(IPage page)
    {
        await page.SetContentAsync(@"
    <nav>
      <a href=""/"">/</a>
      <a href=""/docs/"">/docs/</a>
      <a href=""/a(b/"">/a(b/</a>
    </nav>
  ");
        await CheckAndMatchSnapshot(page.Locator("nav"), @"
    - navigation:
      - link ""/"":
        - /url: /
      - link ""/docs/"":
        - /url: /docs/
      - link ""/a(b/"":
        - /url: /a(b/
  ");
    }

    [UpstreamTest("AR-005 should snapshot complex")]
    public async Task ShouldSnapshotComplex(IPage page)
    {
        await page.SetContentAsync(@"
    <ul>
      <li>
        <a href='about:blank'>link</a>
      </li>
    </ul>
  ");
        await CheckAndMatchSnapshot(page.Locator("body"), @"
    - list:
      - listitem:
        - link ""link"":
          - /url: about:blank
  ");
    }

    [UpstreamTest("AR-006 should allow text nodes")]
    public async Task ShouldAllowTextNodes(IPage page)
    {
        await page.SetContentAsync(@"
    <h1>Microsoft</h1>
    <div>Open source projects and samples from Microsoft</div>
  ");

        await CheckAndMatchSnapshot(page.Locator("body"), @"
    - heading ""Microsoft"" [level=1]
    - text: Open source projects and samples from Microsoft
  ");
    }

    [UpstreamTest("AR-007 should snapshot details visibility")]
    public async Task ShouldSnapshotDetailsVisibility(IPage page)
    {
        await page.SetContentAsync(@"
    <details>
      <summary>Summary</summary>
      <div>Details</div>
    </details>
  ");

        await CheckAndMatchSnapshot(page.Locator("body"), @"
    - group: Summary
  ");
    }

    [UpstreamTest("AR-008 should not snapshot text directly inside closed details")]
    public async Task ShouldNotSnapshotTextDirectlyInsideClosedDetails(IPage page)
    {
        await page.SetContentAsync(@"
    <details>
      <summary>Summary</summary>
      Details
    </details>
  ");

        await CheckAndMatchSnapshot(page.Locator("body"), @"
    - group: Summary
  ");

        await page.EvalOnSelectorAsync("details", "details => details.open = true");
        await CheckAndMatchSnapshot(page.Locator("body"), @"
    - group: Summary Details
  ");
    }

    [UpstreamTest("AR-009 should not snapshot text that is not assigned to a slot")]
    public async Task ShouldNotSnapshotTextThatIsNotAssignedToASlot(IPage page)
    {
        await page.SetContentAsync(@"
    <div role=button>Light</div>
    <script>
      document.querySelector('div').attachShadow({ mode: 'open' }).innerHTML = '<p>Shadow</p>';
    </script>
  ");

        await CheckAndMatchSnapshot(page.Locator("body"), @"
    - button ""Shadow"":
      - paragraph: Shadow
  ");
        await Expect(page.GetByRole(AriaRole.Button, new() { Name = "Shadow", Exact = true })).ToBeVisibleAsync();
    }

    [UpstreamTest("AR-010 should snapshot integration")]
    public async Task ShouldSnapshotIntegration(IPage page)
    {
        await page.SetContentAsync(@"
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

        await CheckAndMatchSnapshot(page.Locator("body"), @"
    - heading ""Microsoft"" [level=1]
    - text: Open source projects and samples from Microsoft
    - list:
      - listitem:
        - group: Verified
      - listitem:
        - link ""Sponsor"":
          - /url: about:blank
  ");
    }

    [UpstreamTest("AR-011 should support multiline text")]
    public async Task ShouldSupportMultilineText(IPage page)
    {
        await page.SetContentAsync(@"
    <p>
      Line 1
      Line 2
      Line 3
    </p>
  ");

        await CheckAndMatchSnapshot(page.Locator("body"), @"
    - paragraph: Line 1 Line 2 Line 3
  ");
        await Expect(page.Locator("body")).ToMatchAriaSnapshotAsync(@"
    - paragraph: |
          Line 1
          Line 2
          Line 3
  ");
    }

    [UpstreamTest("AR-012 should concatenate span text")]
    public async Task ShouldConcatenateSpanText(IPage page)
    {
        await page.SetContentAsync(@"
    <span>One</span> <span>Two</span> <span>Three</span>
  ");

        await CheckAndMatchSnapshot(page.Locator("body"), @"
    - text: One Two Three
  ");
    }

    [UpstreamTest("AR-013 should concatenate span text 2")]
    public async Task ShouldConcatenateSpanText2(IPage page)
    {
        await page.SetContentAsync(@"
    <span>One </span><span>Two </span><span>Three</span>
  ");

        await CheckAndMatchSnapshot(page.Locator("body"), @"
    - text: One Two Three
  ");
    }

    [UpstreamTest("AR-014 should concatenate div text with spaces")]
    public async Task ShouldConcatenateDivTextWithSpaces(IPage page)
    {
        await page.SetContentAsync(@"
    <div>One</div><div>Two</div><div>Three</div>
  ");

        await CheckAndMatchSnapshot(page.Locator("body"), @"
    - text: One Two Three
  ");
    }

    [UpstreamTest("AR-015 should include pseudo in text")]
    public async Task ShouldIncludePseudoInText(IPage page)
    {
        await page.SetContentAsync(@"
    <style>
      span:before {
        content: 'world';
      }
      div:after {
        content: 'bye';
      }
    </style>
    <a href=""about:blank"">
      <span>hello</span>
      <div>hello</div>
    </a>
  ");

        await CheckAndMatchSnapshot(page.Locator("body"), @"
    - link ""worldhello hellobye"":
      - /url: about:blank
  ");
    }

    [UpstreamTest("AR-016 should not include hidden pseudo in text")]
    public async Task ShouldNotIncludeHiddenPseudoInText(IPage page)
    {
        await page.SetContentAsync(@"
    <style>
      span:before {
        content: 'world';
        display: none;
      }
      div:after {
        content: 'bye';
        visibility: hidden;
      }
    </style>
    <a href=""about:blank"">
      <span>hello</span>
      <div>hello</div>
    </a>
  ");

        await CheckAndMatchSnapshot(page.Locator("body"), @"
    - link ""hello hello"":
      - /url: about:blank
  ");
    }

    [UpstreamTest("AR-017 should include new line for block pseudo")]
    public async Task ShouldIncludeNewLineForBlockPseudo(IPage page)
    {
        await page.SetContentAsync(@"
    <style>
      span:before {
        content: 'world';
        display: block;
      }
      div:after {
        content: 'bye';
        display: block;
      }
    </style>
    <a href=""about:blank"">
      <span>hello</span>
      <div>hello</div>
    </a>
  ");

        await CheckAndMatchSnapshot(page.Locator("body"), @"
    - link ""world hello hello bye"":
      - /url: about:blank
  ");
    }

    [UpstreamTest("AR-018 should work with slots")]
    public async Task ShouldWorkWithSlots(IPage page)
    {
        // Text "foo" is assigned to the slot, should not be used twice.
        await page.SetContentAsync(@"
    <button><div>foo</div></button>
    <script>
      (() => {
        const container = document.querySelector('div');
        const shadow = container.attachShadow({ mode: 'open' });
        const slot = document.createElement('slot');
        shadow.appendChild(slot);
      })();
    </script>
  ");
        await CheckAndMatchSnapshot(page.Locator("body"), @"
    - button ""foo""
  ");

        // Text "foo" is assigned to the slot, should be used instead of slot content.
        await page.SetContentAsync(@"
    <div>foo</div>
    <script>
      (() => {
        const container = document.querySelector('div');
        const shadow = container.attachShadow({ mode: 'open' });
        const button = document.createElement('button');
        shadow.appendChild(button);
        const slot = document.createElement('slot');
        button.appendChild(slot);
        const span = document.createElement('span');
        span.textContent = 'pre';
        slot.appendChild(span);
      })();
    </script>
  ");
        await CheckAndMatchSnapshot(page.Locator("body"), @"
    - button ""foo""
  ");

        // Nothing is assigned to the slot, should use slot content.
        await page.SetContentAsync(@"
    <div></div>
    <script>
      (() => {
        const container = document.querySelector('div');
        const shadow = container.attachShadow({ mode: 'open' });
        const button = document.createElement('button');
        shadow.appendChild(button);
        const slot = document.createElement('slot');
        button.appendChild(slot);
        const span = document.createElement('span');
        span.textContent = 'pre';
        slot.appendChild(span);
      })();
    </script>
  ");
        await CheckAndMatchSnapshot(page.Locator("body"), @"
    - button ""pre""
  ");
    }

    [UpstreamTest("AR-019 should snapshot inner text")]
    public async Task ShouldSnapshotInnerText(IPage page)
    {
        await page.SetContentAsync(@"
    <div role=""listitem"">
      <div>
        <div>
          <span title=""a.test.ts"">a.test.ts</span>
        </div>
        <div>
          <button title=""Run""></button>
          <button title=""Show source""></button>
          <button title=""Watch""></button>
        </div>
      </div>
    </div>
    <div role=""listitem"">
      <div>
        <div>
          <span title=""snapshot"">snapshot</span>
        </div>
        <div class=""ui-mode-list-item-time"">30ms</div>
        <div>
          <button title=""Run""></button>
          <button title=""Show source""></button>
          <button title=""Watch""></button>
        </div>
      </div>
    </div>
  ");

        await CheckAndMatchSnapshot(page.Locator("body"), @"
    - listitem:
      - text: a.test.ts
      - button ""Run""
      - button ""Show source""
      - button ""Watch""
    - listitem:
      - text: snapshot 30ms
      - button ""Run""
      - button ""Show source""
      - button ""Watch""
  ");
    }

    [UpstreamTest("AR-020 should include pseudo codepoints")]
    public async Task ShouldIncludePseudoCodepoints(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        await page.SetContentAsync(@"
    <link href=""codicon.css"" rel=""stylesheet"" />
    <p class='codicon codicon-check'>hello</p>
  ");

        await CheckAndMatchSnapshot(page.Locator("body"), @"
    - paragraph: " + "" + @"hello
  ");
    }

    [UpstreamTest("AR-021 check aria-hidden text")]
    public async Task CheckAriaHiddenText(IPage page)
    {
        await page.SetContentAsync(@"
    <p>
      <span>hello</span>
      <span aria-hidden=""true"">world</span>
    </p>
  ");

        await CheckAndMatchSnapshot(page.Locator("body"), @"
    - paragraph: hello
  ");
    }

    [UpstreamTest("AR-022 should ignore presentation and none roles")]
    public async Task ShouldIgnorePresentationAndNoneRoles(IPage page)
    {
        await page.SetContentAsync(@"
    <ul>
      <li role='presentation'>hello</li>
      <li role='none'>world</li>
    </ul>
  ");

        await CheckAndMatchSnapshot(page.Locator("body"), @"
    - list: hello world
  ");
    }

    [UpstreamTest("AR-023 should treat input value as text in templates, but not for checkbox/radio/file")]
    public async Task ShouldTreatInputValueAsTextInTemplatesButNotForCheckboxRadioFile(IPage page)
    {
        await page.SetContentAsync(@"
    <input value='hello world'>
    <input type=file>
    <input type=checkbox checked>
    <input type=radio checked>
  ");

        await CheckAndMatchSnapshot(page.Locator("body"), @"
    - textbox: hello world
    - button ""Choose File""
    - checkbox [checked]
    - radio [checked]
  ");
    }

    [UpstreamTest("AR-024 should not use on as checkbox value")]
    public async Task ShouldNotUseOnAsCheckboxValue(IPage page)
    {
        await page.SetContentAsync(@"
    <input type='checkbox'>
    <input type='radio'>
  ");

        await CheckAndMatchSnapshot(page.Locator("body"), @"
    - checkbox
    - radio
  ");
    }

    [UpstreamTest("AR-025 should respect aria-owns")]
    public async Task ShouldRespectAriaOwns(IPage page)
    {
        await page.SetContentAsync(@"
    <a href='about:blank' aria-owns='input p'>
      <div role='region'>Link 1</div>
    </a>
    <a href='about:blank' aria-owns='input p'>
      <div role='region'>Link 2</div>
    </a>
    <input id='input' value='Value'>
    <p id='p'>Paragraph</p>
  ");

        // - Different from Chrome DevTools which attributes ownership to the last element.
        // - CDT also does not include non-owned children in accessible name.
        // - Disregarding these as aria-owns can't suggest multiple parts by spec.
        await CheckAndMatchSnapshot(page.Locator("body"), @"
    - link ""Link 1 Value Paragraph"":
      - /url: about:blank
      - region: Link 1
      - textbox: Value
      - paragraph: Paragraph
    - link ""Link 2 Value Paragraph"":
      - /url: about:blank
      - region: Link 2
  ");
    }

    [UpstreamTest("AR-026 should be ok with circular ownership")]
    public async Task ShouldBeOkWithCircularOwnership(IPage page)
    {
        await page.SetContentAsync(@"
    <a href='about:blank' id='parent'>
      <div role='region' aria-owns='parent'>Hello</div>
    </a>
  ");

        await CheckAndMatchSnapshot(page.Locator("body"), @"
    - link ""Hello"":
      - /url: about:blank
      - region: Hello
  ");
    }

    [UpstreamTest("AR-027 should escape yaml text in text nodes")]
    public async Task ShouldEscapeYamlTextInTextNodes(IPage page)
    {
        await page.SetContentAsync(@"
    <details>
      <summary>one: <a href=""#"">link1</a> ""two <a href=""#"">link2</a> 'three <a href=""#"">link3</a> `four</summary>
    </details>
    <ul>
      <a href=""#"">one</a>,<a href=""#"">two</a>
      (<a href=""#"">three</a>)
      {<a href=""#"">four</a>}
      [<a href=""#"">five</a>]
    </ul>
    <div>[Select all]</div>
  ");

        await CheckAndMatchSnapshot(page.Locator("body"), @"
    - group:
      - text: ""one:""
      - link ""link1"":
        - /url: ""#""
      - text: ""\""two""
      - link ""link2"":
        - /url: ""#""
      - text: ""'three""
      - link ""link3"":
        - /url: ""#""
      - text: ""`four""
    - list:
      - link ""one"":
        - /url: ""#""
      - text: "",""
      - link ""two"":
        - /url: ""#""
      - text: (
      - link ""three"":
        - /url: ""#""
      - text: "") {""
      - link ""four"":
        - /url: ""#""
      - text: ""} [""
      - link ""five"":
        - /url: ""#""
      - text: ""]""
    - text: ""[Select all]""
  ");
    }

    [UpstreamTest("AR-028 should normalize whitespace")]
    public async Task ShouldNormalizeWhitespace(IPage page)
    {
        await page.SetContentAsync(@"
    <details>
      <summary> one  " + "\n" + @" two <a href=""#""> link &nbsp;" + "\n" + @"  1 </a> </summary>
    </details>
    <input value='  hello   &nbsp; world '>
    <button>hello" + "­​" + @"world</button>
  ");

        await CheckAndMatchSnapshot(page.Locator("body"), @"
    - group:
      - text: one two
      - link ""link 1"":
        - /url: ""#""
    - textbox: hello world
    - button ""helloworld""
  ");

        // Weird whitespace in the template should be normalized.
        await Expect(page.Locator("body")).ToMatchAriaSnapshotAsync(@"
    - group:
      - text: |
          one
          two
      - link ""  link     1 "":
        - /url: ""#""
    - textbox:        hello  world
    - button ""he" + "­" + @"lloworld" + "​" + @"""
  ");
    }

    [UpstreamTest("AR-029 should handle long strings")]
    public async Task ShouldHandleLongStrings(IPage page)
    {
        var s = new string('a', 10000);
        await page.SetContentAsync($@"
    <a href='about:blank'>
      <div role='region'>{s}</div>
    </a>
  ");

        await CheckAndMatchSnapshot(page.Locator("body"), $@"
    - link:
      - /url: about:blank
      - region: {s}
  ");
    }

    [UpstreamTest("AR-030 should escape special yaml characters")]
    public async Task ShouldEscapeSpecialYamlCharacters(IPage page)
    {
        await page.SetContentAsync(@"
    <a href=""#"">@hello</a>@hello
    <a href=""#"">]hello</a>]hello
    <a href=""#"">hello" + "\n" + @"</a>
    hello" + "\n" + @"<a href=""#"">" + "\n" + @" hello</a>" + "\n" + @" hello
    <a href=""#"">#hello</a>#hello
  ");

        await CheckAndMatchSnapshot(page.Locator("body"), @"
    - link ""@hello"":
      - /url: ""#""
    - text: ""@hello""
    - link ""]hello"":
      - /url: ""#""
    - text: ""]hello""
    - link ""hello"":
      - /url: ""#""
    - text: hello
    - link ""hello"":
      - /url: ""#""
    - text: hello
    - link ""#hello"":
      - /url: ""#""
    - text: ""#hello""
  ");
    }

    [UpstreamTest("AR-031 should escape special yaml values")]
    public async Task ShouldEscapeSpecialYamlValues(IPage page)
    {
        await page.SetContentAsync(@"
    <a href=""#"">true</a>False
    <a href=""#"">NO</a>yes
    <a href=""#"">y</a>N
    <a href=""#"">on</a>Off
    <a href=""#"">null</a>NULL
    <a href=""#"">123</a>123
    <a href=""#"">-1.2</a>-1.2
    <a href=""#"">-</a>-
    <a href=""#"">~</a>~
    <a href=""#"">.inf</a>.NaN
    <input type=text value=""555"">
  ");

        await CheckAndMatchSnapshot(page.Locator("body"), @"
    - link ""true"":
      - /url: ""#""
    - text: ""False""
    - link ""NO"":
      - /url: ""#""
    - text: ""yes""
    - link ""y"":
      - /url: ""#""
    - text: ""N""
    - link ""on"":
      - /url: ""#""
    - text: ""Off""
    - link ""null"":
      - /url: ""#""
    - text: ""NULL""
    - link ""123"":
      - /url: ""#""
    - text: ""123""
    - link ""-1.2"":
      - /url: ""#""
    - text: ""-1.2""
    - link ""-"":
      - /url: ""#""
    - text: ""-""
    - link ""~"":
      - /url: ""#""
    - text: ""~""
    - link "".inf"":
      - /url: ""#""
    - text: "".NaN""
    - textbox: ""555""
  ");
    }

    [UpstreamTest("AR-032 should not report textarea textContent")]
    public async Task ShouldNotReportTextareaTextContent(IPage page)
    {
        await page.SetContentAsync("<textarea>Before</textarea>");
        await CheckAndMatchSnapshot(page.Locator("body"), @"
    - textbox: Before
  ");
        await page.EvaluateAsync(@"() => {
    document.querySelector('textarea').value = 'After';
  }");
        await CheckAndMatchSnapshot(page.Locator("body"), @"
    - textbox: After
  ");
    }

    [UpstreamTest("AR-033 should not show visible children of hidden elements")]
    public async Task ShouldNotShowVisibleChildrenOfHiddenElements(IPage page)
    {
        await page.SetContentAsync(@"
    <div style=""visibility: hidden;"">
      <div style=""visibility: visible;"">
        <button>Button</button>
      </div>
    </div>
  ");

        Assert.That(await page.Locator("body").AriaSnapshotAsync(), Is.EqualTo(""));
    }

    [UpstreamTest("AR-034 should not show unhidden children of aria-hidden elements")]
    public async Task ShouldNotShowUnhiddenChildrenOfAriaHiddenElements(IPage page)
    {
        await page.SetContentAsync(@"
    <div aria-hidden=""true"">
      <div aria-hidden=""false"">
        <button>Button</button>
      </div>
    </div>
  ");

        Assert.That(await page.Locator("body").AriaSnapshotAsync(), Is.EqualTo(""));
    }

    [UpstreamTest("AR-035 should snapshot placeholder when different from the name")]
    public async Task ShouldSnapshotPlaceholderWhenDifferentFromTheName(IPage page)
    {
        await page.SetContentAsync(@"
    <input placeholder=""Placeholder"">
  ");
        ExpectToContainYaml(await page.Locator("body").AriaSnapshotAsync(), @"
    - textbox ""Placeholder""
  ");

        await page.SetContentAsync(@"
    <input placeholder=""Placeholder"" aria-label=""Label"">
  ");
        ExpectToContainYaml(await page.Locator("body").AriaSnapshotAsync(), @"
    - textbox ""Label"":
      - /placeholder: Placeholder
  ");
    }

    [UpstreamTest("AR-036 match values both against regex and string")]
    public async Task MatchValuesBothAgainstRegexAndString(IPage page)
    {
        await page.SetContentAsync("<a href=\"/auth?r=/\">Log in</a>");
        await CheckAndMatchSnapshot(page.Locator("body"), @"
    - link ""Log in"":
      - /url: /auth?r=/
  ");
    }

    [UpstreamTest("AR-037 auto-waits the locator and does not include iframes")]
    public async Task AutoWaitsTheLocatorAndDoesNotIncludeIframes(IPage page)
    {
        await page.SetContentAsync(@"
    <div>Hello</div>
  ");

        var snapshotPromise = page.Locator("#target").AriaSnapshotAsync();
        await page.WaitForTimeoutAsync(2000);
        await page.SetContentAsync(@"
    <div id=target>
      Hello
      <iframe srcdoc=""<ul><li>Item 1</li><li>Item 2</li></ul>""></iframe>
    </div>
  ");
        ExpectToContainYaml(await snapshotPromise, @"
    - text: Hello
  ");
    }

    [UpstreamTest("AR-038 should limit depth")]
    public async Task ShouldLimitDepth(IPage page)
    {
        await page.SetContentAsync(@"
    <ul id=target>
      <li>item2</li>
      <li>
        <ul>
          <li>item3</li>
        </ul>
      </li>
    </ul>
  ");

        var snapshot = await page.Locator("#target").AriaSnapshotAsync(new() { Depth = 1 });
        ExpectToContainYaml(snapshot, @"
    - list:
      - listitem: item2
      - listitem
  ");
    }

    [UpstreamTest("AR-039 should snapshot a locator inside an iframe")]
    public async Task ShouldSnapshotALocatorInsideAnIframe(IPage page)
    {
        await page.SetContentAsync(@"
    <h1>Main Page</h1>
    <iframe srcdoc=""<ul><li>Item 1</li><li>Item 2</li></ul>""></iframe>
  ");

        var list = page.Frames[1].Locator("ul");
        var snapshot = await list.AriaSnapshotAsync();
        ExpectToContainYaml(snapshot, @"
    - list:
      - listitem: Item 1
      - listitem: Item 2
  ");
    }

    [UpstreamTest("AR-040 should include frames on frameset pages")]
    public async Task ShouldIncludeFramesOnFramesetPages(IPage page, Server server)
    {
        server.SetRoute("/frameset.html", (req, res) =>
        {
            res.SetHeader("Content-Type", "text/html");
            res.End("<frameset cols=\"50%,50%\"><frame src=\"/frames/frame.html\"><frame src=\"/frames/frame.html\"></frameset>");
        });
        await page.GotoAsync(server.Prefix + "/frameset.html");
        // Like iframes, frames are listed in the snapshot, but their content is not included.
        Assert.That(await page.AriaSnapshotAsync(new() { Timeout = 3000 }), Is.EqualTo(Unshift(@"
    - iframe
    - iframe
  ")));
    }

    [UpstreamTest("AR-041 should snapshot a locator inside a frameset frame")]
    public async Task ShouldSnapshotALocatorInsideAFramesetFrame(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/frames/frameset.html");
        await CheckAndMatchSnapshot(page.FrameLocator("frame").First.Locator("body"), @"
    - text: Hi, I'm frame
  ");
    }

    [UpstreamTest("AR-042 should snapshot with box from page")]
    public async Task ShouldSnapshotWithBoxFromPage(IPage page)
    {
        await page.SetContentAsync(@"
    <button style=""position:absolute;left:100px;top:50px;width:80px;height:40px;margin:0;padding:0;border:0;"">click</button>
  ");

        var snapshot = await page.AriaSnapshotAsync(new() { Boxes = true });
        Assert.That(snapshot, Is.EqualTo("- button \"click\" [box=100,50,80,40]"));
    }

    [UpstreamTest("AR-043 should snapshot with box from locator")]
    public async Task ShouldSnapshotWithBoxFromLocator(IPage page)
    {
        await page.SetContentAsync(@"
    <div style=""position:absolute;left:10px;top:20px;width:200px;height:100px;"">
      <button style=""position:absolute;left:5px;top:5px;width:60px;height:30px;margin:0;padding:0;border:0;"">ok</button>
    </div>
  ");

        var snapshot = await page.Locator("div").AriaSnapshotAsync(new() { Boxes = true });
        Assert.That(snapshot, Is.EqualTo("- button \"ok\" [box=15,25,60,30]"));
    }

    [UpstreamTest("AR-044 should not include box when option is omitted")]
    public async Task ShouldNotIncludeBoxWhenOptionIsOmitted(IPage page)
    {
        await page.SetContentAsync("<button>click</button>");
        var snapshot = await page.AriaSnapshotAsync();
        Assert.That(snapshot, Does.Not.Match(@"\[box="));
    }

    [UpstreamTest("AR-045 should not include hidden iframes")]
    public async Task ShouldNotIncludeHiddenIframes(IPage page)
    {
        await page.SetContentAsync(@"
    <iframe name=""visible"" srcdoc=""<button>Visible</button>""></iframe>
    <iframe name=""hidden"" style=""visibility: hidden"" srcdoc=""<button>Hidden</button>""></iframe>
    <iframe name=""none"" style=""display: none"" srcdoc=""<button>None</button>""></iframe>
  ");
        Assert.That(await page.AriaSnapshotAsync(), Is.EqualTo(Unshift(@"
    - iframe
  ")));
    }

    [UpstreamTest("AR-171 should include inert elements and elements outside of a modal dialog")]
    public async Task ShouldIncludeInertElementsAndElementsOutsideOfAModalDialog(IPage page)
    {
        await page.SetContentAsync(@"
    <button>Hi</button>
    <button inert>Hello</button>
    <div inert>
      <button>Yay</button>
    </div>
    <dialog id=dialog>
      <button>Inside</button>
    </dialog>
    <script>
      document.getElementById('dialog').showModal();
    </script>
  ");

        await CheckAndMatchSnapshot(page.Locator("body"), @"
    - button ""Hi""
    - button ""Hello""
    - button ""Yay""
    - dialog:
      - button ""Inside""
  ");
    }
}
