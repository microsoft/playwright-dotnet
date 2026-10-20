---
name: playwright-dotnet-release
description: Prepare a Playwright .NET release after the rolling PR has merged — mark the version, cut the release branch, port the Node.js release notes to .NET, draft the GitHub release, and tick the .NET boxes in the internal checklist.
---

Use this skill once the roll PR has merged into `main` and the upstream JS `v1.X.0` is published. The rolling work itself is covered by the [[playwright-roll]] skill.

Throughout this doc, replace `X` with the minor version (e.g. `64` for `1.64.0`) and `<user>` with the fork owner (`gh api user --jq .login`).

The full release checklist lives in the private `microsoft/playwright-internal` repo as the `v1.X release checklist` issue. Find its number once:

```bash
unset GITHUB_TOKEN
ISSUE=$(gh search issues --repo microsoft/playwright-internal "v1.X release checklist" --json number --jq '.[0].number')
```

Tick each .NET box incrementally (one PATCH per item) so the issue reflects accurate state if the flow is interrupted. The PR links in a new checklist are copied from the previous release, so replace them as you go:

```bash
gh api repos/microsoft/playwright-internal/issues/$ISSUE --jq '.body' > /tmp/body.md
# edit /tmp/body.md to flip "- [ ]" → "- [x]" on the relevant .NET item
gh api repos/microsoft/playwright-internal/issues/$ISSUE -X PATCH --field body=@/tmp/body.md
```

## 1. Mark the version on `main`

Set `AssemblyVersion` in `src/Common/Version.props` to `1.X.0`, dropping any suffix. `PackageVersion` stays `$(AssemblyVersion)`. Then PR it to `main`:

```bash
git fetch upstream main
git checkout -b mark-v1.X.0 upstream/main
# edit src/Common/Version.props: <AssemblyVersion>1.X.0</AssemblyVersion>
git commit -am "chore: mark v1.X.0"
git push -u origin mark-v1.X.0
gh pr create --repo microsoft/playwright-dotnet --head <user>:mark-v1.X.0 --base main \
  --title "chore: mark v1.X.0" \
  --body "Updates \`AssemblyVersion\` to \`1.X.0\` for the v1.X release."
```

The diff is exactly one line in `Version.props` (e.g. microsoft/playwright-dotnet#3382). Anything else is a red flag.

## 2. Cut the release branch

Once the mark PR has merged, push `release-1.X` from `upstream/main`, then confirm it carries the new version:

```bash
git fetch upstream main
git push upstream upstream/main:refs/heads/release-1.X
git show upstream/release-1.X:src/Common/Version.props | grep AssemblyVersion
```

## 3. Release notes

There are two pieces of text, both written by hand:
- the `## Version 1.X` section of `docs/src/release-notes-csharp.md` in the upstream `playwright` repo. playwright.dev renders it.
- the body of the GitHub release, which is written from that section.

### 3a. Port the Node.js notes to `docs/src/release-notes-csharp.md`

Check whether the section is already upstream:

```bash
cd ~/playwright && git fetch upstream main release-1.X
git show upstream/release-1.X:docs/src/release-notes-csharp.md | grep -n '^## Version 1.X$'
```

If it's missing, write it by porting the `## Version 1.X` section of `docs/src/release-notes-js.md`. Do the work in a worktree off `upstream/main` so the local checkout stays untouched. If someone else already wrote it, still review it against the rules below.

Go through the JS section top to bottom and decide what to do with each item:

- **Keep** library features (Page, Frame, Locator, BrowserContext, APIRequestContext, Tracing, Screencast, assertions, …). Also keep breaking changes that affect the library, announcements about OS support and browser builds, and the Browser Versions list as is.
- **Drop** whatever exists only in the Node.js test runner or JS tooling:
  - `@playwright/test`, `playwright.config.ts`, and `npx playwright test` flags
  - `TestOptions`, `TestConfig`, `TestProject`, `FullConfig`, `TestStep` and `Fixtures`
  - reporters, the HTML report and UI mode
  - component testing
  - TypeScript-only features (generics, JSX, `tsconfig`)
  - `devices` descriptors and npm packages

  The .NET test integrations (`Microsoft.Playwright.NUnit`, `MSTest`, `Xunit`) are separate from the Node.js test runner.
- **Check `langs:`** of every linked member in `docs/src/api/` (`grep -n -A4 'method: Page.foo$' docs/src/api/class-page.md`). A member whose `langs:` doesn't include `csharp` doesn't exist in .NET, so drop the item or reword it around the .NET API.
- **Describe the .NET shape** when the .NET API differs from the JS one, using the names in the generated interfaces of this repo (`src/Playwright/API/Generated/`, including `Options/` and `Enums/`):
  - 1.63: the JS `trace.snapshots: { aria, screen }` object became the `AriaSnapshots` and `ScreenSnapshots` options of `Tracing.StartAsync`.
  - 1.64: the JS `recordVideo.fps` became the `RecordVideoFps` option.
  - Options whose JS type is a union get one typed property per type, e.g. `HttpCredentialsList` for the list form of `httpCredentials`.
- **Replace config examples.** When a JS example shows a library feature through `playwright.config.ts`, show the equivalent API call instead. In 1.64, the video config became `page.Screencast.StartAsync(...)` plus `ShowActionsAsync(...)`.
- **Add .NET-only highlights.** List the user-visible changes in this repo since the previous release branch was cut:
  ```bash
  git log --oneline --no-merges $(git merge-base upstream/release-1.<X-1> upstream/main)..upstream/main | grep -E '^\w+ (feat|fix)'
  ```

Write code samples and prose in .NET style:

| Node.js | .NET |
|---|---|
| `await page.goto('...')` | `await page.GotoAsync("...");`: PascalCase members, `Async` suffix on async methods, `var` locals, double quotes, semicolons |
| `page.getByRole('button', { name: 'Save' })` | `page.GetByRole(AriaRole.Button, new() { Name = "Save" })` |
| options object `{ path: 'video.webm', fps: 60 }` | options initializer `new() { Path = "video.webm", Fps = 60 }` |
| nested object `size: { width: 1920, height: 1080 }` | `Size = new() { Width = 1920, Height = 1080 }` |
| object literal `{ name: 'session-id', value: '42', url }` | `new Cookie { Name = "session-id", Value = "42", Url = url }` |
| `['a', 'b']`, free-form object `{ a: 2 }` | `new[] { "a", "b" }`, anonymous object `new { a = 2 }` |
| string-literal union value `'ai'`, `'button'` | enum member `AriaSnapshotMode.Ai`, `AriaRole.Button` |
| property `page.webmcp`, `context.tracing` | property `page.Webmcp`, `context.Tracing` |
| event `page.on('dialogclosed', handler)` | `page.DialogClosed += (_, dialog) => { ... };` |
| `expect(locator).toHaveText(...)` | `await Expect(locator).ToHaveTextAsync(...);` |
| `console.log(x)` | `Console.WriteLine(x);` |
| members with `alias-csharp:` | the alias, e.g. `page.request` → `page.APIRequest` (`grep -rn 'alias-csharp' docs/src/api/`) |
| `npx playwright install --no-remove` | `pwsh bin/Debug/netX/playwright.ps1 install --no-remove` |
| option names in prose (`fontSize`), `undefined` | `FontSize`, `null` |

In the markdown, keep API references as links in the docs format (``[`method: Locator.within`]``, ``[`property: Page.webmcp`]``, ``[`option: Browser.newContext.recordVideoFps`]``, `[Locator]`) instead of writing out names. playwright.dev renders them with the .NET names. Use ```` ```csharp ```` code fences. Every API a sample uses must exist in the generated interfaces, so grep for each method, property and option.

Send the change as an upstream PR titled `docs: release notes for v1.X .NET`. The upstream `infra` workflow checks the syntax of `csharp` snippets, so make sure it passes. Once the PR merges, ask the user to get it cherry-picked to `release-1.X`: upstream maintainers push `cherry-pick(#NNNN): ...` commits straight to the release branch. Then tick the .NET "write release notes" box and set its PR link.

### 3b. Write the GitHub release body

Write `/tmp/v1.X.0-release-notes.md` by hand from the `## Version 1.X` section of `upstream/release-1.X:docs/src/release-notes-csharp.md`:

- Drop the `## Version 1.X` heading and move every other heading up one level (`###` → `##`, `####` → `###`).
- Replace each docs-format link with a playwright.dev link whose text is the .NET name:

| Markdown | GitHub release |
|---|---|
| `[Locator]` | `[Locator](https://playwright.dev/dotnet/docs/api/class-locator)` |
| ``[`method: Locator.within`]`` | `[Locator.Within()](https://playwright.dev/dotnet/docs/api/class-locator#locator-within)` |
| ``[`method: Page.ariaSnapshot`]`` | `[Page.AriaSnapshotAsync()](https://playwright.dev/dotnet/docs/api/class-page#page-aria-snapshot)` |
| ``[`method: Locator.first`]`` (a property in .NET) | `[Locator.First](https://playwright.dev/dotnet/docs/api/class-locator#locator-first)` |
| ``[`property: Page.webmcp`]`` | `[Page.Webmcp](https://playwright.dev/dotnet/docs/api/class-page#page-webmcp)` |
| ``[`event: Page.dialogClosed`]`` | `[Page.DialogClosed](https://playwright.dev/dotnet/docs/api/class-page#page-event-dialog-closed)` |
| ``[`method: LocatorAssertions.toBeVisible`]`` | `[Expect(Locator).ToBeVisibleAsync()](https://playwright.dev/dotnet/docs/api/class-locatorassertions#locator-assertions-to-be-visible)` |
| ``[`option: Screencast.start.fps`]`` | ``[`Fps`](https://playwright.dev/dotnet/docs/api/class-screencast#screencast-start-option-fps)`` |
| `[Playwright MCP](./getting-started-mcp.md)` | `[Playwright MCP](https://playwright.dev/dotnet/docs/getting-started-mcp)` |
| `![alt](./images/foo.png)` | `![alt](https://github.com/microsoft/playwright/blob/main/docs/src/images/foo.png?raw=true)` |

How to build the links:
- **Link text:** the class name without the interface `I` (`Page`, `BrowserContext`, `APIRequestContext`), then the member exactly as the generated `I<Class>.cs` declares it. Async methods take the `Async` suffix and `()`, sync methods get `()`, and properties (`{ get; }`) and events get no parentheses. Assertions start with `Expect(Locator).`, `Expect(Page).` or `Expect(Response).`.
- **Page path:** `class-<class name in lowercase>`.
- **Anchor:** built from the names in the docs, not the .NET names: `<kebab(Class)>-<kebab(member)>`. Insert `-event-` for events and append `-option-<kebab(option)>` for options. kebab-case splits at every case change, so `APIRequestContext.addCookies` → `class-apirequestcontext#api-request-context-add-cookies`.

Then check that every link resolves. The GitHub draft is written after the JS release, so playwright.dev already documents the new APIs. playwright.dev serves minified HTML, so the anchor may appear without quotes (`id=page-webmcp>`):

```bash
grep -oE 'https://playwright\.dev/[^) ]+' /tmp/v1.X.0-release-notes.md | sort -u | while read -r url; do
  page=${url%%#*}
  html=$(curl -sfL "$page") || { echo "MISSING PAGE   $url"; continue; }
  if [[ "$url" == *"#"* ]]; then
    anchor=${url#*#}
    grep -qE "id=\"?${anchor}[\" >]" <<<"$html" || echo "MISSING ANCHOR $url"
  fi
done
```

Scan for anything that wasn't converted, including stray spaces such as `Page. DialogClosed` and `` `HttpCredentialsList ` `` (both shipped in v1.63.0). Both commands should print nothing:

```bash
grep -nE '\[`(method|property|event|option|param): |\]\(\./|\[[A-Z][A-Za-z]*\]([^(]|$)' /tmp/v1.X.0-release-notes.md
grep -nE "\.on\('|\bundefined\b|\[[a-z][A-Za-z]*\.|\[[A-Z][A-Za-z]*\.[a-z]|\[(Expect\()[a-z]|\[[A-Za-z]+\. |\[\`[^\`]+ \`\]" /tmp/v1.X.0-release-notes.md
```

If the body exposes a problem in the markdown itself, such as a JS-only feature or a JS-style sample, fix it upstream as well (3a). Otherwise playwright.dev keeps the problem.

Create the draft directly against `release-1.X`. Drafting against `main` and retargeting later is fragile, because every `gh release edit` rotates the `untagged-<hash>` ID:

```bash
gh release create v1.X.0 --repo microsoft/playwright-dotnet --draft \
  --title "v1.X.0" --notes-file /tmp/v1.X.0-release-notes.md --target release-1.X
```

## 4. Publish

The user publishes the draft release manually. Publishing creates the `v1.X.0` tag, which triggers the Azure DevOps pipelines:
- `.azure-pipelines/publish.yml` pushes the packages to NuGet.
- `.azure-pipelines/publish-docker.yml` pushes the Docker images.

The checklist links both pipelines. Tick its boxes once they succeed.
