# Playwright .NET

## Building

```bash
./build.sh --download-driver   # download the Playwright driver
dotnet build ./src              # build the entire solution
```

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

## Architecture

### Generated vs hand-written code
- Public API interfaces (e.g. `src/Playwright/API/Generated/IPage.cs`) are **generated** by `../playwright/utils/doclint/generateDotnetApi.js` from the upstream API docs. Do not hand-edit these — update the generator instead.
- The generator uses `classNameMap` for type mappings (e.g. `Disposable` → `IAsyncDisposable`, `boolean` → `bool`). Add entries there when a Playwright type should map to a different .NET type.
- The generator skips generating interface files for types like `TimeoutException` and `IAsyncDisposable` that map to built-in .NET types.
- Supplement interfaces (`src/Playwright/API/Supplements/`) are hand-written and extend the generated interfaces with .NET-specific overloads.
- Internal implementations live in `src/Playwright/Core/` (namespace `Microsoft.Playwright.Core`). These implement both the generated and supplement interfaces.

### Key patterns
- All Playwright objects extend `ChannelOwner` and communicate via `SendMessageToServerAsync`.
- `Connection.cs` has a factory switch that creates the right `ChannelOwner` subclass based on `ChannelOwnerType`.
- New channel object types require: enum entry in `ChannelOwnerType.cs`, case in `Connection.cs`, initializer in `Transport/Protocol/Generated/`, and a `Core/` class.
- Public APIs should use .NET standard types (e.g. `IAsyncDisposable`) not custom Playwright types. Internal helpers (e.g. `Disposable` class in `Core/`) stay internal.

## Commits
- Do not include "co-authored" block in the commit message.

## Rolling to a new Playwright version
See [.claude/skills/playwright-roll/SKILL.md](.claude/skills/playwright-roll/SKILL.md).
