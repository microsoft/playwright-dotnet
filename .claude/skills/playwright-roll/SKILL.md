---
name: playwright-roll
description: Roll Playwright .NET to a new version
---

Help the user roll to a new version of Playwright.
../../../ROLLING.md contains general instructions and scripts.
See also [CLAUDE.md](../../../CLAUDE.md) for build, test, and architecture details.

`DriverVersion` in `src/Common/Version.props` holds one `microsoft/playwright` commit. `.upstream/` (gitignored) is a full clone of `microsoft/playwright` at that commit. The driver is built from it, the API and the transport channels are generated from it, and the hand-written client ports its JS client. A roll moves `DriverVersion` from the old commit to the new one, and ports the diff between the two. Do not commit, branch or edit in `.upstream/`.

Roll to the ref that the user names: a branch (`main`, `release-1.66`), a tag (`v1.66.0`) or a commit. Without one, roll to `main`.

Always start with running the roll script, to see the state of things. It writes the commit to `DriverVersion`, checks it out in `.upstream/`, builds the driver from it and regenerates the API. It needs git and Node.js with npm.

```bash
./build.sh --roll <ref>
old=$(git show HEAD:src/Common/Version.props | sed -n 's|.*<DriverVersion>\(.*\)</DriverVersion>.*|\1|p')
new=$(git -C .upstream rev-parse HEAD)
```

The driver reports the version of `packages/playwright-core/package.json` at the new commit: `1.66.0` on a release tag, `1.67.0-next` on `main`. The script prints it.

Afterwards, walk through the upstream changes that affect the .NET client and port the relevant ones.

## Determining what to port

List the upstream commits between `$old` and `$new` that touched a client-relevant path. The paths cover everything that can change the public .NET surface or the wire protocol:

- `docs/src/api/` — the source of truth for `api.json`. Method/option additions, removals, and `langs:` filter changes flow from here.
- `packages/playwright-core/src/client/` — the JS client implementation that the .NET client mirrors.
- `packages/isomorphic/` — selector engines, locator generation/parsing, and aria-snapshot logic shared between client and server. Changes here can affect client-side helpers like `getByRoleSelector`.
- `packages/playwright/src/matchers/matchers.ts` — assertion-method definitions. Changes here usually correspond to new options on `LocatorAssertions` / `PageAssertions`.
- `packages/protocol/spec/**.yml` — the wire protocol schema. Method/event additions, parameter renames, and result-shape changes affect what the .NET implementation classes need to send/receive.

```bash
./build.sh --changes            # since DriverVersion at HEAD, oldest first
./build.sh --changes <from>     # or since another commit or tag, e.g. v1.65.0
```

Walk that list top-to-bottom. For each commit:
1. Read the commit (`git -C .upstream show <sha>`) to see what client/protocol/docs changed.
2. If it's JS-internal (bundling, dispatcher conventions, electron, mcp, dashboard, trace-viewer, test-runner) — skip.
3. If it touches `docs/src/api/` or types, check `langs:` annotations — features marked with languages other than "csharp" do not apply.
4. If it adds/changes a public API method or option that applies to .NET, port it. The 'build.sh --roll' already regenerated the types/options, so we usually only need to update the implementation.
5. Watch for follow-up reverts — a "feat: X" commit might be undone by a later "Revert X". Check whether the change still exists in `.upstream/` before porting.
6. Maintain a running notes file (e.g. `/tmp/roll-notes.md`) listing each upstream PR as ported / skipped / verified-already-supported, with a one-line reason. This file becomes the body of the eventual PR.
7. Do not forget to port tests that cover .NET-relevant changes. At least one test per every API method/option to excercise the new code paths. Some server-side implementation tests are not needed in this port.

## Mimicking the JavaScript implementation

The .NET client is a port of the JS client in `.upstream/packages/playwright-core/src/client/`. When implementing a new or changed method, always read the corresponding JS file first and mirror its logic:

```
.upstream/packages/playwright-core/src/client/browserContext.ts
.upstream/packages/playwright-core/src/client/page.ts
...
```

**Read the final state in `.upstream/`, not just the introducing PR diff.** When a feature evolves across multiple PRs (e.g. a method was introduced in one PR, then some options were added in follow-ups, then some logic was changed), the original PR's diff is a misleading reference — the final shape lives in `.upstream/packages/playwright-core/src/client/foo.ts`, which is at the new commit. Always read that file before porting non-trivial logic. `git -C .upstream diff "$old" "$new" -- <path>` shows everything that changed in one file.

Key translation rules:

**Protocol calls** — `await this._channel.methodName(params)` → `SendMessageToServerAsync("methodName", paramsDictionary)`

**Extracting a returned channel object from a result** — JS uses `SomeClass.from(result.foo)` which resolves the JS-side object for a channel reference. In .NET, extract it from the connection: `.GetObject<Foo>("foo", _connection)`

**Watch for channel migrations.** A method that lived on `BrowserContext` may move to `Tracing` (and vice-versa) without notice — the protocol spec is the source of truth. If a `SendMessageToServerAsync("foo", ...)` call suddenly fails, grep `packages/protocol/spec/*.yml` to find which channel actually owns `foo` now.

**Wire format changes can be subtle.** A method's result shape can change without renaming anything. These are easy to miss when walking client.ts because the JS code just adapts; the regression only surfaces when an existing test's deserialization breaks. Always diff `packages/protocol/spec/*.yml` for each commit on the walk list, not just the client code. `git -C .upstream diff "$old" "$new" -- packages/protocol/spec` shows the whole change of the protocol.

**Update tests similarly to upstream.** When upstream removes or modifies a test, apply similar changes.

## Renaming generated types

When the API generator produces an unhelpful name for a return type (e.g. `Bind` instead of `BrowserBindResult`), you can control it by adding a struct alias in the upstream docs.

In the docs markdown file (e.g. `docs/src/api/class-browser.md`), add `alias` (applies to all languages) or `alias-csharp` (overrides for .NET only) to the `<[Object]>` type:

```diff
-- returns: <[Object]>
+- returns: <[Object]>
+  - alias-csharp: BrowserBindResult
```

Use `alias-csharp` (in addition to bare `alias`) when upstream already declared a different `alias` for other languages and you only want to override .NET's name — typically to preserve a name we've already shipped and don't want to break.

The generated files come from `.upstream/` at `DriverVersion`, and CI fails when they differ, so the docs change must land in `microsoft/playwright` first. Make it in another checkout of `microsoft/playwright` (not in `.upstream/`), and try it with `PW_SRC_DIR=<that checkout> ./build.sh --generate`. Once the upstream PR has merged, roll to a commit that has it, then update any hand-written implementation code to use the new type name.

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
- `.upstream/` is the upstream checkout, at the new commit. Only read from it.
- Use the "gh" cli to interact with GitHub.
- When running git commands against the upstream repo, always use `git -C .upstream <subcommand>` instead of `cd .upstream && git <subcommand>`.
- `.upstream/ is not at <commit>`: run `./build.sh --build-driver`. It checks out `DriverVersion` and builds the driver again when the commit changed.
- `.upstream has local changes`: see them with `git -C .upstream status`, and discard them.
- `./build.sh --generate` regenerates the API and the transport channels without moving `DriverVersion`.
