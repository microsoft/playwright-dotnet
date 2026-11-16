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

using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Playwright.Tests.Fixtures;
using static Microsoft.Playwright.Assertions;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Page;

/// <summary>Port of tests/page/page-aria-snapshot-ai.spec.ts.</summary>
public class PageAriaSnapshotAiTests
{
    private static async Task<string> SnapshotForAI(IPage page, PageAriaSnapshotOptions options = null)
    {
        return await page.AriaSnapshotAsync(new(options) { Mode = AriaSnapshotMode.Ai });
    }

    [UpstreamTest("AR-048 should generate refs")]
    public async Task ShouldGenerateRefs(IPage page)
    {
        await page.SetContentAsync(@"
    <button>One</button>
    <button>Two</button>
    <button>Three</button>
  ");

        var snapshot1 = await SnapshotForAI(page);
        ExpectToContainYaml(snapshot1, @"
    - generic [active] [ref=e1]:
      - button ""One"" [ref=e2]
      - button ""Two"" [ref=e3]
      - button ""Three"" [ref=e4]
  ");
        await Expect(page.GetByRef("e2")).ToHaveTextAsync("One");
        await Expect(page.GetByRef("e3")).ToHaveTextAsync("Two");
        await Expect(page.GetByRef("e4")).ToHaveTextAsync("Three");

        await page.GetByRef("e3").EvaluateAsync(@"e => {
    e.textContent = 'Not Two';
  }");

        var snapshot2 = await SnapshotForAI(page);
        ExpectToContainYaml(snapshot2, @"
    - generic [active] [ref=e1]:
      - button ""One"" [ref=e2]
      - button ""Not Two"" [ref=e5]
      - button ""Three"" [ref=e4]
  ");
    }

    [UpstreamTest("AR-049 should list iframes")]
    public async Task ShouldListIframes(IPage page)
    {
        await page.SetContentAsync(@"
    <h1>Hello</h1>
    <iframe name=""foo"" src=""data:text/html,<h1>World</h1>"">
  ");

        var snapshot1 = await SnapshotForAI(page);
        Assert.That(snapshot1, Does.Contain("- iframe"));

        var frameSnapshot = await page.FrameLocator("iframe").Locator("body").AriaSnapshotAsync();
        Assert.That(frameSnapshot, Is.EqualTo("- heading \"World\" [level=1]"));
    }

    [UpstreamTest("AR-050 should snapshot a locator inside an iframe")]
    public async Task ShouldSnapshotALocatorInsideAnIframe(IPage page)
    {
        await page.SetContentAsync(@"
    <h1>Main Page</h1>
    <iframe srcdoc=""<ul><li>Item 1</li><li>Item 2</li></ul>""></iframe>
  ");

        var list = page.Frames[1].Locator("ul");
        var snapshot = await list.AriaSnapshotAsync(new() { Mode = AriaSnapshotMode.Ai });
        ExpectToContainYaml(snapshot, @"
    - list [ref=f1e1]:
      - listitem [ref=f1e2]: Item 1
      - listitem [ref=f1e3]: Item 2
  ");
    }

    [UpstreamTest("AR-051 should limit depth across iframe boundary")]
    public async Task ShouldLimitDepthAcrossIframeBoundary(IPage page)
    {
        await page.SetContentAsync(@"
    <nav>
      <iframe srcdoc=""<ul><li><button>Deep</button></li></ul>""></iframe>
    </nav>
  ");

        var snapshot = await SnapshotForAI(page, new() { Depth = 3 });
        ExpectToContainYaml(snapshot, @"
    - navigation [ref=e2]:
      - iframe [ref=e3]:
        - list [ref=f1e2]:
          - listitem [ref=f1e3]
  ");
        Assert.That(snapshot, Does.Not.Contain("button"));
    }

    [UpstreamTest("AR-052 should stitch all frame snapshots")]
    [Fixme("dotnet: locator.ToString() after NormalizeAsync() is the C# form Locator(\"body\"), upstream expects locator('body')")]
    public async Task ShouldStitchAllFrameSnapshots(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/frames/nested-frames.html");
        var snapshot = await SnapshotForAI(page);
        ExpectToContainYaml(snapshot, @"
    - generic [active] [ref=e1]:
      - iframe [ref=e2]:
        - generic [ref=f1e1]:
          - iframe [ref=f1e2]:
            - generic [ref=f3e1]: Hi, I'm frame
          - iframe [ref=f1e3]:
            - generic [ref=f4e1]: Hi, I'm frame
      - iframe [ref=e3]:
        - generic [ref=f2e1]: Hi, I'm frame
  ");

        var href = await page.GetByRef("e1").EvaluateAsync<string>("e => e.ownerDocument.defaultView.location.href");
        Assert.That(href, Is.EqualTo(server.Prefix + "/frames/nested-frames.html"));

        var href2 = await page.GetByRef("f1e2").EvaluateAsync<string>("e => e.ownerDocument.defaultView.location.href");
        Assert.That(href2, Is.EqualTo(server.Prefix + "/frames/two-frames.html"));

        var href3 = await page.GetByRef("f4e2").EvaluateAsync<string>("e => e.ownerDocument.defaultView.location.href");
        Assert.That(href3, Is.EqualTo(server.Prefix + "/frames/frame.html"));

        {
            var resolved = await page.GetByRef("e1").NormalizeAsync();
            Assert.That(resolved.ToString(), Is.EqualTo("locator('body')"));
        }
        {
            var resolved = await page.GetByRef("f4e2").NormalizeAsync();
            Assert.That(resolved.ToString(), Is.EqualTo("locator('iframe[name=\"2frames\"]').contentFrame().locator('iframe[name=\"dos\"]').contentFrame().getByText('Hi, I\\'m frame')"));
        }
        {
            // Should tolerate .describe().
            var resolved = await page.GetByRef("f3e2").Describe("foo bar").NormalizeAsync();
            Assert.That(resolved.ToString(), Is.EqualTo("locator('iframe[name=\"2frames\"]').contentFrame().locator('iframe[name=\"uno\"]').contentFrame().getByText('Hi, I\\'m frame')"));
        }
        {
            var error = await Catch(() => page.GetByRef("e1000").NormalizeAsync());
            Assert.That(error.Message, Does.Contain("No element matching aria-ref=e1000"));
        }

        // .NET: EvaluateAllAsync has no overload without a type argument.
        await page.Locator("iframe").EvaluateAllAsync<JsonElement?>(@"iframes => {
    for (const iframe of iframes)
      iframe.textContent = 'Your browser does not support frames';
  }");
        Assert.That(await SnapshotForAI(page), Is.EqualTo(snapshot));
    }

    [UpstreamTest("AR-053 should re-number refs across navigations but not same-document navigations")]
    public async Task ShouldReNumberRefsAcrossNavigationsButNotSameDocumentNavigations(IPage page, Server server)
    {
        server.SetRoute("/one.html", (req, res) =>
        {
            res.SetHeader("Content-Type", "text/html");
            res.End("<button>One</button>");
        });
        server.SetRoute("/two.html", (req, res) =>
        {
            res.SetHeader("Content-Type", "text/html");
            res.End("<button>Two</button>");
        });

        // The first committed document keeps the base seq, so the main frame has no prefix.
        await page.GotoAsync(server.Prefix + "/one.html");
        var oneRef = Regex.Match(await SnapshotForAI(page), "button \"One\" \\[ref=(e\\d+)\\]").Groups[1].Value;
        await Expect(page.GetByRef(oneRef)).ToHaveTextAsync("One");

        // Cross-document navigation re-numbers the main frame, so its refs gain a frame prefix.
        await page.GotoAsync(server.Prefix + "/two.html");
        var twoRef = Regex.Match(await SnapshotForAI(page), "button \"Two\" \\[ref=(f\\d+e\\d+)\\]").Groups[1].Value;
        await Expect(page.GetByRef(twoRef)).ToHaveTextAsync("Two");

        // The stale ref from the previous document must not resolve against the new one.
        var error = await Catch(() => page.GetByRef(oneRef).NormalizeAsync());
        Assert.That(error.Message, Does.Contain($"No element matching aria-ref={oneRef}"));

        // Same-document navigation keeps refs intact.
        await page.EvaluateAsync("() => history.pushState({}, '', '/pushed.html')");
        Assert.That(await SnapshotForAI(page), Does.Contain($"button \"Two\" [ref={twoRef}]"));
        await Expect(page.GetByRef(twoRef)).ToHaveTextAsync("Two");
    }

    [UpstreamTest("AR-054 should persist iframe references")]
    public async Task ShouldPersistIframeReferences(IPage page)
    {
        await page.SetContentAsync(@"
    <ul>
      <li><iframe srcdoc=""<button>button1</button>""></iframe></li>
      <li><iframe srcdoc=""<button>button2</button>""></iframe></li>
    </ul>
  ");
        ExpectToContainYaml(await SnapshotForAI(page), @"
    - list [ref=e2]:
      - listitem [ref=e3]:
        - iframe [ref=e4]:
          - button ""button1"" [ref=f1e2]
      - listitem [ref=e5]:
        - iframe [ref=e6]:
          - button ""button2"" [ref=f2e2]
  ");

        await page.EvaluateAsync("() => document.querySelector('iframe').remove()");
        ExpectToContainYaml(await SnapshotForAI(page), @"
    - list [ref=e2]:
      - listitem [ref=e3]
      - listitem [ref=e5]:
        - iframe [ref=e6]:
          - button ""button2"" [ref=f2e2]
  ");
        await Expect(page.GetByRef("f2e2")).ToHaveTextAsync("button2");

        await page.EvaluateAsync(@"() => {
    const frame = document.createElement('iframe');
    frame.setAttribute('srcdoc', '<button>button1</button>');
    document.querySelector('li').appendChild(frame);
  }");
        ExpectToContainYaml(await SnapshotForAI(page), @"
    - list [ref=e2]:
      - listitem [ref=e3]:
        - iframe [ref=e7]:
          - button ""button1"" [ref=f3e2]
      - listitem [ref=e5]:
        - iframe [ref=e6]:
          - button ""button2"" [ref=f2e2]
  ");
        await Expect(page.GetByRef("f3e2")).ToHaveTextAsync("button1");
        await Expect(page.GetByRef("f2e2")).ToHaveTextAsync("button2");
    }

    [UpstreamTest("AR-055 should not generate refs for elements with pointer-events:none")]
    public async Task ShouldNotGenerateRefsForElementsWithPointerEventsNone(IPage page)
    {
        await page.SetContentAsync(@"
    <button style=""pointer-events: none"">no-ref</button>
    <div style=""pointer-events: none"">
      <button style=""pointer-events: auto"">with-ref</button>
    </div>
    <div style=""pointer-events: none"">
      <div style=""pointer-events: initial"">
        <button>with-ref</button>
      </div>
    </div>
    <div style=""pointer-events: none"">
      <div style=""pointer-events: auto"">
        <button>with-ref</button>
      </div>
    </div>
    <div style=""pointer-events: auto"">
      <div style=""pointer-events: none"">
        <button>no-ref</button>
      </div>
    </div>
  ");

        var snapshot = await SnapshotForAI(page);
        ExpectToContainYaml(snapshot, @"
    - generic [active] [ref=e1]:
      - button ""no-ref""
      - button ""with-ref"" [ref=e2]
      - button ""with-ref"" [ref=e4]
      - button ""with-ref"" [ref=e6]
      - generic [ref=e7]:
        - generic:
          - button ""no-ref""
  ");
    }

    [UpstreamTest("AR-056 emit generic roles for nodes w/o roles")]
    public async Task EmitGenericRolesForNodesWORoles(IPage page)
    {
        await page.SetContentAsync(@"
    <style>
    input {
      width: 0;
      height: 0;
      opacity: 0;
    }
    </style>
    <div>
      <label>
        <span>
          <input type=""radio"" value=""Apple"" checked="""">
        </span>
        <span>Apple</span>
      </label>
      <label>
        <span>
          <input type=""radio"" value=""Pear"">
        </span>
        <span>Pear</span>
      </label>
      <label>
        <span>
          <input type=""radio"" value=""Orange"">
        </span>
        <span>Orange</span>
      </label>
    </div>
  ");

        var snapshot = await SnapshotForAI(page);

        ExpectToContainYaml(snapshot, @"
    - generic [ref=e2]:
      - generic [ref=e3]:
        - generic [ref=e4]:
          - radio ""Apple"" [checked]
        - text: Apple
      - generic [ref=e5]:
        - generic [ref=e6]:
          - radio ""Pear""
        - text: Pear
      - generic [ref=e7]:
        - generic [ref=e8]:
          - radio ""Orange""
        - text: Orange
  ");
    }

    [UpstreamTest("AR-057 should collapse generic nodes")]
    public async Task ShouldCollapseGenericNodes(IPage page)
    {
        await page.SetContentAsync(@"
    <div>
      <div>
        <div>
          <button>Button</button>
        </div>
      </div>
    </div>
  ");

        var snapshot = await SnapshotForAI(page);
        ExpectToContainYaml(snapshot, @"
    - button ""Button"" [ref=e5]
  ");
    }

    [UpstreamTest("AR-058 should include cursor pointer hint")]
    public async Task ShouldIncludeCursorPointerHint(IPage page)
    {
        await page.SetContentAsync(@"
    <button style=""cursor: pointer"">Button</button>
  ");

        var snapshot = await SnapshotForAI(page);
        ExpectToContainYaml(snapshot, @"
    - button ""Button"" [ref=e2] [cursor=pointer]
  ");
    }

    [UpstreamTest("AR-059 should not nest cursor pointer hints")]
    public async Task ShouldNotNestCursorPointerHints(IPage page)
    {
        await page.SetContentAsync(@"
    <a style=""cursor: pointer"" href=""about:blank"">
      Link with a button
      <button style=""cursor: pointer"">Button</button>
    </a>
  ");

        var snapshot = await SnapshotForAI(page);
        // The link's name is redundant - "Link with a button" prints as text and "Button" as the button -
        // so it is dropped even though the node is clickable.
        ExpectToContainYaml(snapshot, @"
    - link [ref=e2] [cursor=pointer]:
      - /url: about:blank
      - text: Link with a button
      - button ""Button"" [ref=e3]
  ");
    }

    [UpstreamTest("AR-060 should omit names that just repeat printed descendant nodes")]
    public async Task ShouldOmitNamesThatJustRepeatPrintedDescendantNodes(IPage page)
    {
        await page.SetContentAsync(@"
    <h3><a style=""cursor: pointer"" href=""/issues/1"">Clipboard API</a></h3>
    <dialog open aria-labelledby=""title"">
      <h2 id=""title"">Delete item?</h2>
      <button>OK</button>
    </dialog>
    <nav aria-labelledby=""nav-title""><h2 id=""nav-title"">Account</h2></nav>
    <table>
      <caption>Monthly totals</caption>
      <tr><th scope=""col""><button>Price</button></th></tr>
    </table>
  ");

        var snapshot = await SnapshotForAI(page);
        ExpectToContainYaml(snapshot, @"
    - generic [active] [ref=e1]:
      - heading [level=3] [ref=e2]:
        - link ""Clipboard API"" [ref=e3] [cursor=pointer]:
          - /url: /issues/1
      - dialog ""Delete item?"" [ref=e4]:
        - heading ""Delete item?"" [level=2] [ref=e5]
        - button ""OK"" [ref=e6]
      - navigation ""Account"" [ref=e7]:
        - heading ""Account"" [level=2] [ref=e8]
      - table ""Monthly totals"" [ref=e9]:
        - caption [ref=e10]: Monthly totals
        - rowgroup [ref=e11]:
          - row [ref=e12]:
            - columnheader ""Price"" [ref=e13]:
              - button ""Price"" [ref=e14]
  ");
    }

    [UpstreamTest("AR-061 should omit redundant name when a contributing wrapper is collapsed")]
    public async Task ShouldOmitRedundantNameWhenAContributingWrapperIsCollapsed(IPage page)
    {
        // The flex span contributes to the heading's name, but is then removed from the tree as a
        // single-child generic wrapper. Its contribution is fully represented by the link, so the
        // heading's name is still redundant.
        await page.SetContentAsync(@"
    <h3><span style=""display: flex""><a style=""cursor: pointer"" href=""/issues/1"">Clipboard API</a></span></h3>
  ");

        var snapshot = await SnapshotForAI(page);
        ExpectToContainYaml(snapshot, @"
    - heading [level=3] [ref=e2]:
      - link ""Clipboard API"" [ref=e4] [cursor=pointer]:
        - /url: /issues/1
  ");
    }

    [UpstreamTest("AR-062 should omit redundant name when a contributor is a skipped leaf generic")]
    public async Task ShouldOmitRedundantNameWhenAContributorIsASkippedLeafGeneric(IPage page)
    {
        // The outer span is not collapsed (its child is an element, not text), so it becomes a leaf
        // generic node that contributes to both names. The link's rendered name covers it, which in turn
        // makes the heading's name redundant.
        await page.SetContentAsync(@"
    <h3><a style=""cursor: pointer"" href=""/issues/1""><span><span>Clipboard API</span></span></a></h3>
  ");

        var snapshot = await SnapshotForAI(page);
        ExpectToContainYaml(snapshot, @"
    - heading [level=3] [ref=e2]:
      - link ""Clipboard API"" [ref=e3] [cursor=pointer]:
        - /url: /issues/1
  ");
    }

    [UpstreamTest("AR-063 should keep the name when the contributing wrapper collapses into repeating text")]
    public async Task ShouldKeepTheNameWhenTheContributingWrapperCollapsesIntoRepeatingText(IPage page)
    {
        // The outer span is marked as represented while it still wraps the icon. Once the nameless
        // image is dropped, the span collapses into a lone text repeating the button's name and removes
        // itself, so the button must keep the name derived from it.
        // A wrapper that only adds whitespace to the name - a stray text node, or the spacing around an
        // inline-block icon - is not where the name comes from.
        await page.SetContentAsync(@"
    <button>
      <span>
        <span>
          <svg focusable=""false"" tabindex=""-1"" aria-hidden=""true"" viewBox=""0 0 448 512"">
            <path d=""M416 208H272V64c0-17.67-14.33-32-32-32h-32c-17.67 0-32 14.33-32 32v144H32c-17.67 0-32 14.33-32 32v32c0 17.67 14.33 32 32 32h144v144c0 17.67 14.33 32 32 32h32c17.67 0 32-14.33 32-32V304h144c17.67 0 32-14.33 32-32v-32c0-17.67-14.33-32-32-32z""/>
          </svg>
        </span>
        <span>Add New Item</span>
      </span>
    </button>
    <button><span>
  <svg aria-hidden=""true""></svg></span>New survey</button>
    <button><span><svg aria-hidden=""true"" style=""width: 1em; height: 1em; display: inline-block""></svg></span>Edit survey</button>
  ");

        var snapshot = await SnapshotForAI(page);
        ExpectToContainYaml(snapshot, @"
    - generic [active] [ref=e1]:
      - button ""Add New Item"" [ref=e2]
      - button ""New survey"" [ref=e7]
      - button ""Edit survey"" [ref=e10]
  ");
    }

    [UpstreamTest("AR-064 should keep names not derived from printed nodes")]
    public async Task ShouldKeepNamesNotDerivedFromPrintedNodes(IPage page)
    {
        await page.SetContentAsync(@"
    <h3 aria-label=""Clipboard API issue""><a style=""cursor: pointer"" href=""/issues/1"">Clipboard API</a></h3>
  ");

        var snapshot = await SnapshotForAI(page);
        ExpectToContainYaml(snapshot, @"
    - heading ""Clipboard API issue"" [level=3] [ref=e2]:
      - link ""Clipboard API"" [ref=e3] [cursor=pointer]:
        - /url: /issues/1
  ");
    }

    [UpstreamTest("AR-065 should omit images without an accessible name")]
    public async Task ShouldOmitImagesWithoutAnAccessibleName(IPage page)
    {
        await page.SetContentAsync(@"
    <img src=""data:image/gif;base64,R0lGODlhAQABAAAAACwAAAAAAQABAAA="">
    <img alt=""A cat"" src=""data:image/gif;base64,R0lGODlhAQABAAAAACwAAAAAAQABAAA="">
    <img style=""cursor: pointer"" src=""data:image/gif;base64,R0lGODlhAQABAAAAACwAAAAAAQABAAA="">
  ");

        var snapshot = await SnapshotForAI(page);
        // A nameless image that cannot be clicked carries no information and is omitted.
        ExpectToContainYaml(snapshot, @"
    - generic [active] [ref=e1]:
      - img ""A cat"" [ref=e3]
      - img [ref=e4] [cursor=pointer]
  ");
        Assert.That(snapshot, Does.Not.Contain("[ref=e2]"));
    }

    [UpstreamTest("AR-066 should omit a nameless image nested inside a link")]
    public async Task ShouldOmitANamelessImageNestedInsideALink(IPage page)
    {
        // The decorative image has no name, so it is dropped even though it sits inside a clickable link.
        await page.SetContentAsync(@"
    <a style=""cursor: pointer"" href=""/issue/1"">Open issue <img src=""data:image/gif;base64,R0lGODlhAQABAAAAACwAAAAAAQABAAA=""></a>
  ");

        var snapshot = await SnapshotForAI(page);
        ExpectToContainYaml(snapshot, @"
    - link ""Open issue"" [ref=e2] [cursor=pointer]:
      - /url: /issue/1
  ");
        Assert.That(snapshot, Does.Not.Contain("img"));
    }

    [UpstreamTest("AR-067 should keep icon-only clickable elements")]
    public async Task ShouldKeepIconOnlyClickableElements(IPage page)
    {
        const string icon = "<svg viewBox=\"0 0 1024 1024\"><use xlink:href=\"#icon\"></use></svg>";
        await page.SetContentAsync($@"
    <div style=""cursor: pointer"" aria-haspopup=""true""><i>{icon}</i></div>
    <img style=""cursor: pointer"" src=""data:image/gif;base64,R0lGODlhAQABAAAAACwAAAAAAQABAAA="">
    <a style=""cursor: pointer"" href=""/target"">{icon}</a>
    <div onclick=""void 0"">{icon}</div>
  ");

        var snapshot = await SnapshotForAI(page);
        ExpectToContainYaml(snapshot, @"
    - generic [active] [ref=e1]:
      - generic [ref=e2] [cursor=pointer]
      - img [ref=e5] [cursor=pointer]
      - link [ref=e6] [cursor=pointer]:
        - /url: /target
  ");
    }

    [UpstreamTest("AR-068 should omit leaf generic whose text is already in an ancestor name")]
    public async Task ShouldOmitLeafGenericWhoseTextIsAlreadyInAnAncestorName(IPage page)
    {
        // The inner element is block so it survives as its own generic node (an inline single-text span
        // would be collapsed into the link instead). It inherits the link's pointer cursor.
        await page.SetContentAsync(@"
    <a style=""cursor: pointer"" href=""/issues/15860""><div>[Feature] a dedicated clipboard API</div></a>
  ");

        var snapshot = await SnapshotForAI(page);
        // The link keeps its name, and the inner leaf generic that the name was computed from is dropped
        // because its text is already shown by the name.
        ExpectToContainYaml(snapshot, @"
    - link ""[Feature] a dedicated clipboard API"" [ref=e2] [cursor=pointer]:
      - /url: /issues/15860
  ");
        Assert.That(snapshot, Does.Not.Contain("[ref=e3]"));
    }

    [UpstreamTest("AR-069 should omit name-repeating generic behind a wrapper")]
    public async Task ShouldOmitNameRepeatingGenericBehindAWrapper(IPage page)
    {
        // The leaf generic that repeats the link's name sits inside a nameless wrapper. Its text is
        // first inlined into the wrapper, which then faces the link and removes itself.
        await page.SetContentAsync(@"
    <a style=""cursor: pointer"" href=""/labels""><span style=""display: inline-block""><span style=""display: inline-block""><span>P3-collecting-feedback</span></span></span></a>
  ");

        var snapshot = await SnapshotForAI(page);
        ExpectToContainYaml(snapshot, @"
    - link ""P3-collecting-feedback"" [ref=e2] [cursor=pointer]:
      - /url: /labels
  ");
        Assert.That(snapshot.Split("P3-collecting-feedback"), Has.Length.EqualTo(2));
    }

    [UpstreamTest("AR-070 should resolve refs of distilled-away nodes")]
    public async Task ShouldResolveRefsOfDistilledAwayNodes(IPage page)
    {
        await page.SetContentAsync(@"
    <a style=""cursor: pointer"" href=""/issues/15860""><div>[Feature] a dedicated clipboard API</div></a>
  ");

        var snapshot = await SnapshotForAI(page);
        // The inner leaf generic is distilled away, but its ref still resolves to the element.
        Assert.That(snapshot, Does.Not.Contain("[ref=e3]"));
        await Expect(page.GetByRef("e3")).ToHaveTextAsync("[Feature] a dedicated clipboard API");
    }

    [UpstreamTest("AR-071 should not distill snapshots outside of ai mode")]
    public async Task ShouldNotDistillSnapshotsOutsideOfAiMode(IPage page)
    {
        await page.SetContentAsync(@"
    <h3><a href=""/issues/1"">Clipboard API</a></h3>
  ");

        // The heading name would be dropped as redundant in ai mode; matching mode keeps it.
        await Expect(page.Locator("body")).ToMatchAriaSnapshotAsync(@"
    - heading ""Clipboard API"" [level=3]:
      - link ""Clipboard API"":
        - /url: /issues/1
  ");
    }

    [UpstreamTest("AR-072 should truncate data url in link")]
    public async Task ShouldTruncateDataUrlInLink(IPage page)
    {
        var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes("<p>hello</p>"));
        await page.SetContentAsync($"<a href=\"data:text/html;base64,{base64}\">a link</a>");
        var snapshot = await SnapshotForAI(page);
        Assert.That(snapshot, Does.Contain("/url: data:text/html;base64,…"));
        Assert.That(snapshot, Does.Not.Contain(base64));
    }

    [UpstreamTest("AR-073 should gracefully fallback when child frame cant be captured")]
    public async Task ShouldGracefullyFallbackWhenChildFrameCantBeCaptured(IPage page, Server server)
    {
        await page.SetContentAsync($@"
    <p>Test</p>
    <iframe src=""{server.Prefix}/redirectloop1.html#depth=100000""></iframe>
  ", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        var snapshot = await SnapshotForAI(page);
        ExpectToContainYaml(snapshot, @"
    - generic [active] [ref=e1]:
      - paragraph [ref=e2]: Test
      - iframe [ref=e3]
  ");
    }

    [UpstreamTest("AR-074 should auto-wait for navigation")]
    public async Task ShouldAutoWaitForNavigation(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/frames/frame.html");
        var (_, snapshot) = await PromiseAll(
            page.EvaluateAsync("() => window.location.reload()"),
            SnapshotForAI(page));
        // The snapshot races the reload, which may re-number the main frame, so accept any ref.
        Assert.That(snapshot, Does.Match("- generic \\[active\\] \\[ref=(?:f\\d+)?e\\d+\\]: Hi, I'm frame"));
    }

    [UpstreamTest("AR-075 should auto-wait for blocking CSS")]
    public async Task ShouldAutoWaitForBlockingCSS(IPage page, Server server)
    {
        server.SetRoute("/css", (req, res) =>
        {
            res.SetHeader("Content-Type", "text/css");
            SetTimeout(() => res.End("body { monospace }"), 1000);
        });
        await page.SetContentAsync($@"
    <script src=""{server.Prefix}/css""></script>
    <p>Hello World</p>
  ", new() { WaitUntil = WaitUntilState.Commit });
        ExpectToContainYaml(await SnapshotForAI(page), "Hello World");
    }

    [UpstreamTest("AR-076 should show visible children of hidden elements")]
    public async Task ShouldShowVisibleChildrenOfHiddenElements(IPage page)
    {
        await page.SetContentAsync(@"
    <div style=""visibility: hidden"">
      <div style=""visibility: visible"">
        <button>Visible</button>
      </div>
      <div style=""visibility: hidden"">
        <button style=""visibility: visible"">Visible</button>
      </div>
      <div>
        <div style=""visibility: visible"">
          <button style=""visibility: hidden"">Hidden</button>
        </div>
        <button>Hidden</button>
      </div>
    </div>
  ");

        Assert.That(await SnapshotForAI(page), Is.EqualTo(Unshift(@"
    - generic [active] [ref=e1]:
      - button ""Visible"" [ref=e3]
      - button ""Visible"" [ref=e4]
  ")));
    }

    [UpstreamTest("AR-077 should include active element information")]
    public async Task ShouldIncludeActiveElementInformation(IPage page)
    {
        await page.SetContentAsync(@"
    <button id=""btn1"">Button 1</button>
    <button id=""btn2"" autofocus>Button 2</button>
    <div>Not focusable</div>
  ");

        // Wait for autofocus to take effect
        await page.WaitForFunctionAsync("() => document.activeElement?.id === 'btn2'");

        var snapshot = await SnapshotForAI(page);

        ExpectToContainYaml(snapshot, @"
    - generic [ref=e1]:
      - button ""Button 1"" [ref=e2]
      - button ""Button 2"" [active] [ref=e3]
      - generic [ref=e4]: Not focusable
  ");
    }

    [UpstreamTest("AR-078 should update active element on focus")]
    public async Task ShouldUpdateActiveElementOnFocus(IPage page)
    {
        await page.SetContentAsync(@"
    <input id=""input1"" placeholder=""First input"">
    <input id=""input2"" placeholder=""Second input"">
  ");

        // Initially there shouldn't be an active element on the inputs
        var initialSnapshot = await SnapshotForAI(page);
        ExpectToContainYaml(initialSnapshot, @"
    - generic [active] [ref=e1]:
      - textbox ""First input"" [ref=e2]
      - textbox ""Second input"" [ref=e3]
  ");

        // Focus the second input
        await page.Locator("#input2").FocusAsync();

        // After focus, the second input should be active
        var afterFocusSnapshot = await SnapshotForAI(page);

        ExpectToContainYaml(afterFocusSnapshot, @"
    - generic [ref=e1]:
      - textbox ""First input"" [ref=e2]
      - textbox ""Second input"" [active] [ref=e3]
  ");
    }

    [UpstreamTest("AR-079 should mark iframe as active when it contains focused element")]
    public async Task ShouldMarkIframeAsActiveWhenItContainsFocusedElement(IPage page)
    {
        // Create a simple HTML file for the iframe
        await page.SetContentAsync(@"
    <input id=""regular-input"" placeholder=""Regular input"">
    <iframe src=""data:text/html,<input id='iframe-input' placeholder='Input in iframe'>"" tabindex=""0""></iframe>
  ");

        // Test 1: Focus the input inside the iframe
        await page.FrameLocator("iframe").Locator("#iframe-input").FocusAsync();
        var inputInIframeFocusedSnapshot = await SnapshotForAI(page);

        // The iframe should be marked as active when it contains a focused element
        ExpectToContainYaml(inputInIframeFocusedSnapshot, @"
    - generic [ref=e1]:
      - textbox ""Regular input"" [ref=e2]
      - iframe [active] [ref=e3]:
        - textbox ""Input in iframe"" [active] [ref=f1e2]
  ");
    }

    [UpstreamTest("AR-080 return empty snapshot when iframe is not loaded")]
    public async Task ReturnEmptySnapshotWhenIframeIsNotLoaded(IPage page, Server server)
    {
        await page.SetContentAsync($@"
    <div style=""height: 5000px;"">Test</div>
    <iframe loading=""lazy"" src=""{server.Prefix}/frame.html""></iframe>
  ");

        // Wait for the iframe to load
        await page.WaitForSelectorAsync("iframe");

        // Get the snapshot of the page
        var snapshot = await SnapshotForAI(page, new() { Timeout = 3000 });

        // The iframe should be present but empty
        ExpectToContainYaml(snapshot, @"
    - generic [active] [ref=e1]:
      - generic [ref=e2]: Test
      - iframe [ref=e3]
  ");
    }

    [UpstreamTest("AR-081 should support many properties on iframes")]
    public async Task ShouldSupportManyPropertiesOnIframes(IPage page)
    {
        await page.SetContentAsync(@"
    <input id=""regular-input"" placeholder=""Regular input"">
    <iframe style='cursor: pointer' src=""data:text/html,<input id='iframe-input' placeholder='Input in iframe'/>"" tabindex=""0""></iframe>
  ");

        // Test 1: Focus the input inside the iframe
        await page.FrameLocator("iframe").Locator("#iframe-input").FocusAsync();
        var inputInIframeFocusedSnapshot = await SnapshotForAI(page);

        ExpectToContainYaml(inputInIframeFocusedSnapshot, @"
    - generic [ref=e1]:
      - textbox ""Regular input"" [ref=e2]
      - iframe [active] [ref=e3] [cursor=pointer]:
        - textbox ""Input in iframe"" [active] [ref=f1e2]
  ");
    }

    [UpstreamTest("AR-082 should snapshot frameset pages")]
    public async Task ShouldSnapshotFramesetPages(IPage page, Server server)
    {
        server.SetRoute("/frameset.html", (req, res) =>
        {
            res.SetHeader("Content-Type", "text/html");
            res.End("<frameset rows=\"50%,50%\"><frameset cols=\"50%,50%\"><frame src=\"/frame-one.html\"><frame src=\"/frame-two.html\"></frameset><frame src=\"/frame-three.html\"></frameset>");
        });
        foreach (var name in new[] { "one", "two", "three" })
        {
            server.SetRoute($"/frame-{name}.html", (req, res) =>
            {
                res.SetHeader("Content-Type", "text/html");
                res.End($"<button>Button {name}</button>");
            });
        }
        await page.GotoAsync(server.Prefix + "/frameset.html");

        var snapshot = await SnapshotForAI(page, new() { Timeout = 3000 });
        ExpectToContainYaml(snapshot, @"
    - generic [active] [ref=e1]:
      - generic [ref=e2]:
        - iframe [ref=e3]:
          - button ""Button one"" [ref=f1e2]
        - iframe [ref=e4]:
          - button ""Button two"" [ref=f2e2]
      - iframe [ref=e5]:
        - button ""Button three"" [ref=f3e2]
  ");
        await Expect(page.GetByRef("f2e2")).ToHaveTextAsync("Button two");
    }

    [UpstreamTest("AR-083 should snapshot a locator inside a frameset frame")]
    [Fixme("dotnet: locator.ToString() after NormalizeAsync() is the C# form Locator(\"frame\").First.ContentFrame.Locator(\"body\"), upstream expects locator('frame').first().contentFrame().locator('body')")]
    public async Task ShouldSnapshotALocatorInsideAFramesetFrame(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/frames/frameset.html");

        var snapshot = await page.Frames[1].Locator("body").AriaSnapshotAsync(new() { Mode = AriaSnapshotMode.Ai });
        ExpectToContainYaml(snapshot, @"
    - generic [ref=f1e1]: Hi, I'm frame
  ");

        await Expect(page.GetByRef("f1e1")).ToHaveTextAsync("Hi, I'm frame");
        var resolved = await page.GetByRef("f1e1").NormalizeAsync();
        Assert.That(resolved.ToString(), Is.EqualTo("locator('frame').first().contentFrame().locator('body')"));
    }

    [UpstreamTest("AR-084 should stitch iframes inside a frameset frame")]
    [Fixme("dotnet: locator.ToString() after NormalizeAsync() is the C# form Locator(\"frame\").ContentFrame.Locator(\"iframe\").ContentFrame.GetByRole(AriaRole.Button, new() { Name = \"In iframe\" }), upstream expects locator('frame').contentFrame().locator('iframe').contentFrame().getByRole('button', { name: 'In iframe' })")]
    public async Task ShouldStitchIframesInsideAFramesetFrame(IPage page, Server server)
    {
        server.SetRoute("/frameset-with-iframe.html", (req, res) =>
        {
            res.SetHeader("Content-Type", "text/html");
            res.End("<frameset><frame src=\"/frame-with-iframe.html\"></frameset>");
        });
        server.SetRoute("/frame-with-iframe.html", (req, res) =>
        {
            res.SetHeader("Content-Type", "text/html");
            res.End("<button>In frame</button><iframe srcdoc=\"<button>In iframe</button>\"></iframe>");
        });
        await page.GotoAsync(server.Prefix + "/frameset-with-iframe.html");

        ExpectToContainYaml(await SnapshotForAI(page), @"
    - iframe [ref=e2]:
      - generic [ref=f1e1]:
        - button ""In frame"" [ref=f1e2]
        - iframe [ref=f1e3]:
          - button ""In iframe"" [ref=f2e2]
  ");
        await Expect(page.GetByRef("f1e2")).ToHaveTextAsync("In frame");
        await Expect(page.GetByRef("f2e2")).ToHaveTextAsync("In iframe");

        var resolved = await page.GetByRef("f2e2").NormalizeAsync();
        Assert.That(resolved.ToString(), Is.EqualTo("locator('frame').contentFrame().locator('iframe').contentFrame().getByRole('button', { name: 'In iframe' })"));
    }

    [UpstreamTest("AR-085 should stitch nested frameset documents")]
    public async Task ShouldStitchNestedFramesetDocuments(IPage page, Server server)
    {
        server.SetRoute("/outer-frameset.html", (req, res) =>
        {
            res.SetHeader("Content-Type", "text/html");
            res.End("<frameset><frame src=\"/inner-frameset.html\"></frameset>");
        });
        server.SetRoute("/inner-frameset.html", (req, res) =>
        {
            res.SetHeader("Content-Type", "text/html");
            res.End("<frameset><frameset><frame src=\"/leaf.html\"></frameset></frameset>");
        });
        server.SetRoute("/leaf.html", (req, res) =>
        {
            res.SetHeader("Content-Type", "text/html");
            res.End("<button>Leaf button</button>");
        });
        await page.GotoAsync(server.Prefix + "/outer-frameset.html");

        ExpectToContainYaml(await SnapshotForAI(page), @"
    - iframe [ref=e2]:
      - iframe [ref=f1e3]:
        - button ""Leaf button"" [ref=f2e2]
  ");
        await Expect(page.GetByRef("f2e2")).ToHaveTextAsync("Leaf button");
    }

    [UpstreamTest("AR-086 should collapse inline generic nodes")]
    public async Task ShouldCollapseInlineGenericNodes(IPage page)
    {
        await page.SetContentAsync(@"
    <ul>
      <li><b>3</b> <abbr>bds</abbr></li>
      <li><b>2</b> <abbr>ba</abbr></li>
      <li><b>1,200</b> <abbr>sqft</abbr></li>
    </ul>
    <ul>
      <li><div>3</div></li>
      <li><div>2</div></li>
      <li><div>1,200</div></li>
    </ul>");

        var snapshot1 = await SnapshotForAI(page);
        ExpectToContainYaml(snapshot1, @"
    - generic [active] [ref=e1]:
      - list [ref=e2]:
        - listitem [ref=e3]: 3 bds
        - listitem [ref=e4]: 2 ba
        - listitem [ref=e5]: 1,200 sqft
      - list [ref=e6]:
        - listitem [ref=e7]:
          - generic [ref=e8]: ""3""
        - listitem [ref=e9]:
          - generic [ref=e10]: ""2""
        - listitem [ref=e11]:
          - generic [ref=e12]: 1,200
  ");
    }

    [UpstreamTest("AR-087 should inline single leaf generic child into parent generic")]
    public async Task ShouldInlineSingleLeafGenericChildIntoParentGeneric(IPage page)
    {
        // The nameless images are distilled away, so each wrapper is left with a single leaf generic
        // child, whose text is inlined into the wrapper - recursively for the second one.
        await page.SetContentAsync(@"
    <div><img src=""data:image/gif;base64,R0lGODlhAQABAAAAACwAAAAAAQABAAA=""><div>Status: Open.</div></div>
    <div><img src=""data:image/gif;base64,R0lGODlhAQABAAAAACwAAAAAAQABAAA=""><div><img src=""data:image/gif;base64,R0lGODlhAQABAAAAACwAAAAAAQABAAA=""><div>Nested twice.</div></div></div>
  ");

        var snapshot = await SnapshotForAI(page);
        ExpectToContainYaml(snapshot, @"
    - generic [active] [ref=e1]:
      - generic [ref=e2]: ""Status: Open.""
      - generic [ref=e5]: Nested twice.
  ");
    }

    [UpstreamTest("AR-088 should inline a deeply nested generic")]
    public async Task ShouldInlineADeeplyNestedGeneric(IPage page)
    {
        // Every wrapper contains a nameless image (distilled away) plus a single generic child, so the
        // text bubbles up the whole chain - all the way into the body.
        const string img = "<img src=\"data:image/gif;base64,R0lGODlhAQABAAAAACwAAAAAAQABAAA=\">";
        await page.SetContentAsync($@"
    <div>{img}<div>{img}<div>{img}<div>{img}<div>Deeply nested.</div></div></div></div></div>
  ");

        var snapshot = await SnapshotForAI(page);
        ExpectToContainYaml(snapshot, @"
    - generic [active] [ref=e1]: Deeply nested.
  ");
        Assert.That(snapshot, Does.Not.Contain("img"));
    }

    [UpstreamTest("AR-089 should not remove generic nodes with title")]
    public async Task ShouldNotRemoveGenericNodesWithTitle(IPage page)
    {
        await page.SetContentAsync("<div title=\"Element title\">Element content</div>");

        var snapshot = await SnapshotForAI(page);
        ExpectToContainYaml(snapshot, @"
    - generic ""Element title"" [ref=e2]
  ");
    }

    [UpstreamTest("AR-090 should limit depth")]
    public async Task ShouldLimitDepth(IPage page)
    {
        await page.SetContentAsync(@"
    <ul>
      <li>item1</li>
      <a href=""about:blank"" style=""cursor:pointer"">link</a>
      <li>
        <ul id=target>
          <li>item2</li>
          <li>
            <ul>
              <li>item3</li>
            </ul>
          </li>
        </ul>
      </li>
    </ul>
  ");

        var snapshot1 = await SnapshotForAI(page, new() { Depth = 1 });
        ExpectToContainYaml(snapshot1, @"
    - list [ref=e2]:
      - listitem [ref=e3]: item1
      - link ""link"" [ref=e4] [cursor=pointer]:
        - /url: about:blank
      - listitem [ref=e5]
  ");

        var snapshot2 = await SnapshotForAI(page, new() { Depth = 3 });
        ExpectToContainYaml(snapshot2, @"
    - list [ref=e2]:
      - listitem [ref=e3]: item1
      - link ""link"" [ref=e4] [cursor=pointer]:
        - /url: about:blank
      - listitem [ref=e5]:
        - list [ref=e6]:
          - listitem [ref=e7]: item2
          - listitem [ref=e8]
  ");

        var snapshot3 = await SnapshotForAI(page, new() { Depth = 100 });
        ExpectToContainYaml(snapshot3, @"
    - list [ref=e2]:
      - listitem [ref=e3]: item1
      - link ""link"" [ref=e4] [cursor=pointer]:
        - /url: about:blank
      - listitem [ref=e5]:
        - list [ref=e6]:
          - listitem [ref=e7]: item2
          - listitem [ref=e8]:
            - list [ref=e9]:
              - listitem [ref=e10]: item3
  ");

        var snapshot4 = await page.Locator("#target").AriaSnapshotAsync(new() { Mode = AriaSnapshotMode.Ai, Depth = 1 });
        ExpectToContainYaml(snapshot4, @"
    - list [ref=e6]:
      - listitem [ref=e7]: item2
      - listitem [ref=e8]
  ");
    }

    [UpstreamTest("AR-091 should annotate aria-hidden elements")]
    public async Task ShouldAnnotateAriaHiddenElements(IPage page)
    {
        await page.SetContentAsync(@"
    <h2>Visible heading</h2>
    <div aria-hidden=""true"">
      <h1>Hidden heading</h1>
      <p>Hidden content</p>
    </div>
    <h2>After hidden</h2>
  ");

        ExpectToContainYaml(await SnapshotForAI(page), @"
    - generic [active] [ref=e1]:
      - heading ""Visible heading"" [level=2] [ref=e2]
      - generic [aria-hidden] [ref=e3]:
        - heading [level=1] [ref=e4]: Hidden heading
        - paragraph [ref=e5]: Hidden content
      - heading ""After hidden"" [level=2] [ref=e6]
  ");

        // Default snapshot excludes aria-hidden elements entirely.
        var defaultSnapshot = await page.Locator("body").AriaSnapshotAsync();
        Assert.That(defaultSnapshot, Does.Not.Contain("Hidden content"));
    }

    [UpstreamTest("AR-092 should only annotate the top element in a hidden subtree")]
    public async Task ShouldOnlyAnnotateTheTopElementInAHiddenSubtree(IPage page)
    {
        await page.SetContentAsync(@"
    <div aria-hidden=""true"">
      <h1>Heading</h1>
      <p>Paragraph</p>
    </div>
  ");

        // Only the element with aria-hidden="true" gets the annotation, not its descendants.
        ExpectToContainYaml(await SnapshotForAI(page), @"
    - generic [aria-hidden] [ref=e2]:
      - heading [level=1] [ref=e3]: Heading
      - paragraph [ref=e4]: Paragraph
  ");
    }

    [UpstreamTest("AR-093 should not include hidden iframes")]
    public async Task ShouldNotIncludeHiddenIframes(IPage page)
    {
        await page.SetContentAsync(@"
    <iframe name=""visible"" srcdoc=""<button>Visible</button>""></iframe>
    <iframe name=""hidden"" style=""visibility: hidden"" srcdoc=""<button>Hidden</button>""></iframe>
    <iframe name=""none"" style=""display: none"" srcdoc=""<button>None</button>""></iframe>
  ");
        var snapshot = await SnapshotForAI(page);
        ExpectToContainYaml(snapshot, @"
    - iframe [ref=e2]:
      - button ""Visible"" [ref=f1e2]
  ");
        Assert.That(snapshot, Does.Not.Contain("Hidden"));
        Assert.That(snapshot, Does.Not.Contain("None"));
    }
}
