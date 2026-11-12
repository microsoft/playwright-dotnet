# Playwright .NET

## Building

```bash
./build.sh --build-driver      # build the Playwright driver from .upstream/ (needs git and Node.js/npm on PATH)
dotnet build ./src              # build the entire solution
```

`DriverVersion` in `src/Common/Version.props` is the `microsoft/playwright` commit that the driver, the generated API and the port come from. `.upstream/` (gitignored) is a clone of `microsoft/playwright` at that commit: `./build.sh --build-driver` clones it, checks out the commit and builds the driver from it. Run it again when `DriverVersion` changes, for example after a pull. Read upstream code in `.upstream/`; do not commit, branch or edit in it.

The .NET client talks to the Node.js driver that is built from that commit. When the same calls fail in a Node.js test upstream too, the cause is in the driver or a browser, and the fix lands in `microsoft/playwright`. When Node.js passes, the bug is in the port: compare the method in `src/Playwright/Core/` with the same method in the JavaScript client, `packages/playwright-core/src/client/`, and fix it here. Write and run upstream tests in a separate checkout of `microsoft/playwright` at `main`, `~/code/playwright`. Paths such as `~/code/playwright` in this file assume that you check the repos out in `~/code`.

## Running tests

Follow `.github/workflows/tests.yml` for the canonical sequence.

```bash
# Install browsers (pick one: chromium, firefox, webkit)
pwsh src/Playwright/bin/Debug/netstandard2.0/playwright.ps1 install --with-deps chromium

# Run tests
BROWSER=chromium dotnet test ./src/Playwright.Tests/Playwright.Tests.csproj -c Debug -f net8.0 --logger:"console;verbosity=detailed"
```

Tests take ~8 minutes. Always save output to a file and grep from there:

```bash
BROWSER=chromium dotnet test ./src/Playwright.Tests/Playwright.Tests.csproj \
  -c Debug -f net8.0 --logger:"console;verbosity=detailed" > /tmp/test-results.txt 2>&1
grep "^  Failed" /tmp/test-results.txt        # list failures
tail -5 /tmp/test-results.txt                  # summary
```

- `--filter Playwright.Tests.PageRouteTests` runs one class, and `--filter "FullyQualifiedName=Microsoft.Playwright.Tests.PageRouteTests.ShouldIntercept"` runs one test. `BROWSER` is `chromium`, `firefox` or `webkit`; `HEADED=1` runs headed.
- Each test has a timeout of 30 seconds; `[PlaywrightTest(TestConstants.SlowTestTimeout)]` gives 150. When `CI` is set, a failed test runs up to 3 times; use `env -u CI` to see one run.
- `dotnet test` rebuilds what it needs. Run `dotnet build ./src` before you commit: it builds every project, also the ones that the tests do not reference, and `src/Playwright` treats analyzer warnings as errors.
- The NUnit, MSTest and xUnit packages next to `src/Playwright.NUnit/` are tested from `src/Playwright.TestingHarnessTest/`, a Node suite that writes a small .NET test project per test: `npm ci` there once, then `npx playwright test tests/nunit.spec.ts`.

## Tests

The tests are NUnit tests in `src/Playwright.Tests/`, one class per upstream spec file: `PageRouteTests.cs` ports `tests/page/page-route.spec.ts`. Locator and assertion classes are in `Locator/` and `Assertions/`. A regression test is one added case in the class that owns the feature, also when only the port has the bug.

- `[PlaywrightTest("page-route.spec.ts", "should intercept")]` names the upstream spec file and test title that the test ports. Upstream titles carry an ID prefix, so grep for the title. `[PlaywrightTest]` without arguments marks a test that only the port has.
- Tag a regression test with `// See <issue url>` on the line above the attribute.
- The base classes are in `BaseTests/`. `PageTestEx` gives `Page`, `Context`, `Browser` and `BrowserName`; `ContextTestEx` and `BrowserTestEx` stop at `Context` and `Browser` and add `NewContext(options)`. All give `Server`, a `SimpleServer` (`TestServer/SimpleServer.cs`) that serves `assets/`, with `Server.Prefix`, `Server.EmptyPage`, `Server.SetRoute(path, context => ...)` on an ASP.NET Core `HttpContext`, `Server.SetRedirect(from, to)` and `Server.WaitForRequest(path)`. Drive the page with `Page.SetContentAsync(...)` or `Page.GotoAsync(Server.Prefix + "/...")`. Do not start a server or a browser of your own. Override `ContextOptions()` when the class needs other context options, as `TapTests.cs` does.
- Good models: `UrlShouldWorkWithBothDomcontentloadedAndLoad` in `PageWaitForUrlTests.cs`, a ported test with a route that holds its response and the issue comment, and `ShouldNotReturnDisposedJsonElement` in `PageEvaluateTests.cs`, a .NET-only test.

## Architecture

### Generated vs hand-written code
- Public API interfaces (e.g. `src/Playwright/API/Generated/IPage.cs`) are **generated** by `.upstream/utils/doclint/generateDotnetApi.js` from the upstream API docs. Do not hand-edit these — update the generator instead.
- `./build.sh --generate` generates them again from `.upstream/`, and CI fails when the committed files differ. The generator change lands upstream first, and a roll brings it here. To try it before that, `PW_SRC_DIR=~/code/playwright ./build.sh --generate` runs the generators of another checkout.
- The generator uses `classNameMap` for type mappings (e.g. `Disposable` → `IAsyncDisposable`, `boolean` → `bool`). Add entries there when a Playwright type should map to a different .NET type.
- The generator skips generating interface files for types like `TimeoutException` and `IAsyncDisposable` that map to built-in .NET types.
- Supplement interfaces (`src/Playwright/API/Supplements/`) are hand-written and extend the generated interfaces with .NET-specific overloads.
- Internal implementations live in `src/Playwright/Core/` (namespace `Microsoft.Playwright.Core`). These implement both the generated and supplement interfaces.

### Key patterns
- All Playwright objects extend `ChannelOwner` and communicate via `SendMessageToServerAsync`.
- `Connection.cs` has a factory switch that creates the right `ChannelOwner` subclass based on `ChannelOwnerType`.
- New channel object types require: enum entry in `ChannelOwnerType.cs`, case in `Connection.cs`, initializer in `Transport/Protocol/Generated/`, and a `Core/` class.
- Public APIs should use .NET standard types (e.g. `IAsyncDisposable`) not custom Playwright types. Internal helpers (e.g. `Disposable` class in `Core/`) stay internal.

### Threading model
The client mirrors the JS event loop: each connection has one dispatcher thread (`Transport/Dispatcher.cs`) that handles driver messages and runs all library code, one item at a time. Read [docs/threading.md](docs/threading.md) before changing code in `Core/` or `Transport/`; it has the model and the rules of thumb. The short version:
- A public async method is a single `SendMessageToServerAsync` call, or wraps its whole body in `WrapApiCallAsync(async () => ...)`.
- State is touched only on the dispatcher thread. A `TaskCompletionSource` is completed with `Dispatcher.Complete`/`Fail`, and code that awaited something else (a user callback, a timeout, I/O) calls `await _connection.Dispatcher.SwitchTo()` before it touches state.
- Lists that getters hand out are copy-on-write (`Helpers/CopyOnWrite.cs`), and nothing blocks on the dispatcher thread.
- `ThreadingModelTests.cs` and `DispatcherTests.cs` pin the rules.

## Commit Convention

Before committing, run `dotnet format ./src/ -v:diag` and fix style issues.

Semantic commit messages: `label(scope): description`

Labels: `fix`, `feat`, `chore`, `docs`, `test`, `devops`

```bash
git checkout -b fix-39562
# ... make changes ...
git add <changed-files>
git commit -m "$(cat <<'EOF'
fix(proxy): handle SOCKS proxy authentication

Fixes: https://github.com/microsoft/playwright/issues/39562
EOF
)"
git push origin fix-39562
gh pr create --repo microsoft/playwright --head username:fix-39562 \
  --title "fix(proxy): handle SOCKS proxy authentication" \
  --body "$(cat <<'EOF'
## Summary
- <describe the change very! briefly>

Fixes https://github.com/microsoft/playwright/issues/39562
EOF
)"
```

Never add Co-Authored-By agents in commit message.
Never add "Generated with" in commit message.
Never add test plan to PR description. Keep PR description short — a few bullet points at most.
Branch naming for issue fixes: `fix-<issue-number>`

## Commits
- Do not include "co-authored" block in the commit message.

## Rolling to a new Playwright version
See [.claude/skills/playwright-roll/SKILL.md](.claude/skills/playwright-roll/SKILL.md).

## Releasing
See [.claude/skills/playwright-dotnet-release/SKILL.md](.claude/skills/playwright-dotnet-release/SKILL.md).
