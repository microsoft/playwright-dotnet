# Playwright .NET — NativeAOT fork

This is a personal fork of `microsoft/playwright-dotnet` being modified for
NativeAOT compatibility.  Upstream contributions are not the goal — aggressive
but clean, documented, isolated changes are acceptable.

## Quick start

```bash
./build.sh --download-driver   # downloads Node.js + driver
dotnet build ./src              # builds the full solution
```

## Running tests (upstream-style)

```bash
BROWSER=chromium dotnet test ./src/Playwright.Tests/Playwright.Tests.csproj \
  -c Debug -f net8.0 --logger:"console;verbosity=detailed" > /tmp/test-results.txt 2>&1
grep "^  Failed" /tmp/test-results.txt
tail -5 /tmp/test-results.txt
```

No GH workflows in this fork — CI is Azure Pipelines (`.azure-pipelines/`).

## Architecture overview

| Layer | Directory | Namespace | Notes |
|-------|-----------|-----------|-------|
| Public API interfaces | `src/Playwright/API/Generated/` | `Microsoft.Playwright` | **Generated** by `../playwright/utils/doclint/generateDotnetApi.js`. Do not hand-edit. |
| Supplement interfaces | `src/Playwright/API/Supplements/` | `Microsoft.Playwright` | Hand-written .NET-specific overloads extending generated interfaces. |
| Internal impls | `src/Playwright/Core/` | `Microsoft.Playwright.Core` | Implements both generated + supplement interfaces. |
| Transport layer | `src/Playwright/Transport/` | `Microsoft.Playwright.Transport` | JSON-over-stdin/stdout to Node.js driver process. |
| Protocol models | `src/Playwright/Transport/Protocol/Generated/` | `Microsoft.Playwright.Transport.Protocol` | **Generated** initializer classes for channel object construction. |
| Helpers | `src/Playwright/Helpers/` | `Microsoft.Playwright.Helpers` | JSON extensions, driver path resolution, misc utilities. |

## Key patterns

- Every Playwright object extends `ChannelOwner` and calls `SendMessageToServerAsync(method, argsDict)`.
- `Connection.cs:265` dispatches incoming JSON. A factory switch in `CreateRemoteObject` maps `ChannelOwnerType` enum → concrete `Core/*` class.
- Adding a new channel type requires: enum entry in `ChannelOwnerType.cs`, case in `Connection.cs`, initializer class in `Transport/Protocol/Generated/`, and a `Core/` class.
- Messages are `Dictionary<string, object?>` dicts serialized to JSON with `System.Text.Json`.
- The driver is a bundled Node.js process. `StdIOTransport` forks it, communicates over length-prefixed JSON on stdin/stdout.

## Commit conventions

- **Before committing:** `dotnet format ./src/ -v:diag`
- `label(scope): description` — labels: `fix`, `feat`, `chore`, `docs`, `test`, `devops`
- Branch naming for issues: `fix-<issue-number>`
- No `Co-Authored-By`, no `Generated with`, no test-plan in PR body.

## Rolling to a new Playwright version

See `.claude/skills/playwright-roll/SKILL.md` (uses `build.sh --roll <version>`).

---

## NativeAOT work — what an agent needs to know

### The big picture

The library currently targets **netstandard2.0** with `System.Text.Json` 6.x.  AOT
requires either multi-targeting (netstandard2.0 + net8.0+/net10.0) or a full
bump.  All JSON serialization uses reflection-based `JsonSerializerOptions`
without source generation.  The message-passing protocol is `Dictionary<string, object?>`
everywhere — the biggest AOT challenge.

### Known AOT problem areas (already identified)

| File | Issue | Severity |
|------|-------|----------|
| `Transport/Converters/ChannelOwnerConverterFactory.cs` | `Activator.CreateInstance` + `MakeGenericType` in `JsonConverterFactory` | hard |
| `Transport/Converters/JsonStringEnumMemberConverter.cs` | `Activator.CreateInstance` + `MakeGenericType` in `JsonConverterFactory` | hard |
| `Transport/Converters/EvaluateArgumentValueConverter.cs` | `TypeDescriptor.GetProperties`, `GetProperties()`, `ExpandoObject`, `Activator.CreateInstance` | hard |
| `Helpers/ChannelHelpers.cs:83` | `dynamic` return type on `Exception.ToObject()` | easy |
| `Connection.cs` | `Dictionary<string, object?>` everywhere, LINQ in hot path | medium |
| `Playwright.cs:48` | `JsonSerializer.Deserialize<PlaywrightServerMessage>` using reflection options | medium |
| `Helpers/Driver.cs` | `Assembly.GetName()`, `assembly.CodeBase` (throws in single-file publish) | medium |
| `Program.cs` | Process launch for CLI driver extraction | low |
| `StdIOTransport.cs` | Driver process launch via `Driver.GetExecutablePath()` | medium |
| `Transport/Protocol/Generated/*.cs` | 68 initializer classes — no `JsonSerializable` attributes | medium |

### Build / validation commands for AOT

```bash
# Validate the library project compiles with AOT analyzers
dotnet build src/Playwright/Playwright.csproj \
  -p:TargetFramework=net10.0 \
  -p:PublishAot=true \
  -p:TrimMode=full

# Publish the smoke-test project
dotnet publish samples/Playwright.NativeAotSmokeTest \
  -c Release -r linux-x64 \
  -p:PublishAot=true -p:SelfContained=true -p:TrimMode=full
```

### Strategy for fixing

1. **JSON protocol** — Add a central `JsonSerializerContext` with source-generated
   metadata for all initializer types, `PlaywrightServerMessage`, `ErrorEntry`,
   and channel payloads.  Replace `JsonSerializerOptions`-based calls with
   source-gen calls.
2. **ChannelOwner converter** — Replace `JsonConverterFactory` + `MakeGenericType`
   with explicit `JsonConverter<T>` per channel type, or use a typed
   `WriteOnlyJsonConverter<ChannelOwner>` that writes `{"guid":"..."}` without
   reflection.
3. **Enum converter** — Replace `JsonConverterFactory` with explicit per-enum
   converters, or use `JsonStringEnumConverter` with `JsonStringEnumConverterOptions`.
4. **Message args** — Replace `Dictionary<string, object?>` with typed message
   classes or `JsonSerializerContext`-compatible dictionaries.
5. **Driver dependency** — Evaluate Option A (keep + AOT-harden) vs Option B
   (experimental Chromium-only CDP pipe backend).  For Option B, use
   `--remote-debugging-pipe`, implement minimum `Page`/`Target`/`Browser` CDP
   commands, behind feature flag.
6. **EvaluateArgumentValueConverter** — This handles `EvaluateAsync`/`EvalAsync`
   argument marshaling.  Hard to fully type because user objects are arbitrary.
   May need to be rewritten as a closed-form visitor with source-generatable
   payload structures, or accept a `RequiresDynamicCode` annotation here only.
7. **Test project** — Keep targeting net8.0+ for test execution; AOT smoke test
   gets a separate project.

### Files to audit first

Use this order: `Transport/Connection.cs` → `Playwright.cs` →
`Transport/Converters/*.cs` → `Helpers/JsonExtensions.cs` →
`Core/BrowserType.cs` → `Transport/Protocol/Generated/*` → `Helpers/Driver.cs` →
`StdIOTransport.cs` → `Program.cs` → `Helpers/ChannelHelpers.cs`.

### Reports / documentation

- `AOT_AUDIT.md` — full audit with file, function, problem, warning code, suggested fix, difficulty.
- `AOT_CHANGES.md` — running changelog.
- `AOT_PUBLISH.md` — final publish instructions.
- `samples/Playwright.NativeAotSmokeTest/` — AOT validation app.
