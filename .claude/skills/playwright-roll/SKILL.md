---
name: playwright-roll
description: Roll Playwright .NET to a new version
---

Help the user roll to a new version of Playwright.
../../../ROLLING.md contains general instructions and scripts.
See also [CLAUDE.md](../../../CLAUDE.md) for build, test, and architecture details.

Unless the exact version is specified by the user, you need to find the latest version of the driver.
Check the publish workflow here: https://github.com/microsoft/playwright/actions/workflows/publish_release.yml. The step that builds and publishes the driver contains the exact version you need.

Now, with the driver version known, always start with running the roll script to update the version and regenerate the API to see the state of things.

```bash
./build.sh --roll <driver-version>
```

Afterwards, walk through the upstream changes that affect the Java client and port the relevant ones.

## Determining what to port

List the upstream commits that touched a client-relevant path since the last roll. The paths cover everything that can change the public Java surface or the wire protocol:

- `docs/src/api/` — the source of truth for `api.json`. Method/option additions, removals, and `langs:` filter changes flow from here.
- `packages/playwright-core/src/client/` — the JS client implementation that the Java client mirrors.
- `packages/isomorphic/` — selector engines, locator generation/parsing, and aria-snapshot logic shared between client and server. Changes here can affect client-side helpers like `getByRoleSelector`.
- `packages/playwright/src/matchers/matchers.ts` — assertion-method definitions. Changes here usually correspond to new options on `LocatorAssertions` / `PageAssertions`.
- `packages/protocol/spec/**.yml` — the wire protocol schema. Method/event additions, parameter renames, and result-shape changes affect what the .NET implementation classes need to send/receive.

```bash
cd ~/playwright
PREV_TAG=$(git tag | grep -E '^v1\.[0-9]+\.[0-9]+$' | sort -V | tail -1)  # e.g. v1.59.1
git log "$PREV_TAG"..HEAD --oneline -- \
  'docs/src/api/' \
  'packages/playwright-core/src/client/' \
  'packages/isomorphic/' \
  'packages/playwright/src/matchers/matchers.ts' \
  'packages/protocol/spec/**.yml'
```

Walk that list top-to-bottom (oldest-first is easier — newest is at top, so reverse). For each commit:
1. Read the commit (`git show <sha>`) to see what client/protocol/docs changed.
2. If it's JS-internal (bundling, dispatcher conventions, electron, mcp, dashboard, trace-viewer, test-runner) — skip.
3. If it touches `docs/src/api/` or types, check `langs:` annotations — features marked with languages other than "csharp" do not apply.
4. If it adds/changes a public API method or option that applies to .NET, port it. The 'build.sh --roll' already regenerated the types/options, so we usually only need to update the implementation.
5. Watch for follow-up reverts — a "feat: X" commit might be undone by a later "Revert X". Check whether the change still exists in HEAD before porting.
6. Maintain a running notes file (e.g. `/tmp/roll-notes.md`) listing each upstream PR as ported / skipped / verified-already-supported, with a one-line reason. This file becomes the body of the eventual PR.
7. Do not forget to port tests that cover .NET-relevant changes. At least one test per every API method/option to excercise the new code paths. Some server-side implementation tests are not needed in this port.

## Mimicking the JavaScript implementation

The .NET client is a port of the JS client in `../playwright/packages/playwright-core/src/client/`. When implementing a new or changed method, always read the corresponding JS file first and mirror its logic:

```
../playwright/packages/playwright-core/src/client/browserContext.ts
../playwright/packages/playwright-core/src/client/page.ts
...
```

**Read the final state at the release tag, not just the introducing PR diff.** When a feature evolves across multiple PRs (e.g. a method was introduced in one PR, then some options were added in follow-ups, then some logic was changed), the original PR's diff is a misleading reference — the final shape lives in `git show v1.X.0:packages/playwright-core/src/client/foo.ts`. Always read the v1.X.0 file before porting non-trivial logic.

Key translation rules:

**Protocol calls** — `await this._channel.methodName(params)` → `SendMessageToServerAsync("methodName", paramsDictionary)`

**Extracting a returned channel object from a result** — JS uses `SomeClass.from(result.foo)` which resolves the JS-side object for a channel reference. In .NET, extract it from the connection: `.GetObject<Foo>("foo", _connection)`

**Watch for channel migrations.** A method that lived on `BrowserContext` may move to `Tracing` (and vice-versa) without notice — the protocol spec is the source of truth. If a `SendMessageToServerAsync("foo", ...)` call suddenly fails, grep `packages/protocol/spec/*.yml` to find which channel actually owns `foo` now.

**Wire format changes can be subtle.** A method's result shape can change without renaming anything. These are easy to miss when walking client.ts because the JS code just adapts; the regression only surfaces when an existing test's deserialization breaks. Always diff `packages/protocol/spec/*.yml` for each commit on the walk list, not just the client code.

**Update tests similarly to upstream.** When upstream removes or modifies a test, apply similar changes.

## Renaming generated types

When the API generator produces an unhelpful name for a return type (e.g. `Bind` instead of `BrowserBindResult`), you can control it by adding a struct alias in the upstream docs.

In the docs markdown file (e.g. `docs/src/api/class-browser.md`), add `alias` (applies to all languages) or `alias-csharp` (overrides for .NET only) to the `<[Object]>` type:

```diff
-- returns: <[Object]>
+- returns: <[Object]>
+  - alias-csharp: BrowserBindResult
```

Use `alias-csharp` (in addition to bare `alias`) when upstream already declared a different `alias` for other languages and you only want to override .NET's name — typically to preserve a name we've already shipped and don't want to break. After making this change, re-run `./build.sh --roll <version>` to regenerate, then update any hand-written implementation code to use the new type name. The upstream docs change must also land in `microsoft/playwright` via a separate PR or it will revert on the next roll.

## Running the full test suite

After porting, run the chromium suite end-to-end before opening the PR — many regressions only surface in older tests that depended on now-changed behavior. Tests take ~8–10 minutes:

```bash
BROWSER=chromium dotnet test ./src/Playwright.Tests/Playwright.Tests.csproj \
  -c Debug -f net8.0 --logger:"console;verbosity=detailed" > /tmp/test-results.txt 2>&1
grep "^  Failed " /tmp/test-results.txt        # list failures
tail -5 /tmp/test-results.txt                  # summary
```

Run as a background task — don't block the conversation on it. For each failure: check whether it's a flake (port collision, browser timing) by running the single test in isolation, then investigate whether your roll introduced it or it's pre-existing.

## Tips & Tricks
- Project checkouts are in the parent directory (`../`).
- Use the "gh" cli to interact with GitHub.
- When running git commands against an upstream repo, always use `git -C /path/to/repo <subcommand>` instead of `cd /path/to/repo && git <subcommand>`.
