# NativeAOT Audit — playwright-dotnet

## Current status

**Updated:** 2026-07-11

The fork now targets `net10.0` for the core package and the main NativeAOT gates pass with 0 warnings. The audit below is kept as the original issue inventory; use this current-status section for the present state.

| Group | Current evidence | Status |
|-------|------------------|--------|
| Library AOT analyzers | `dotnet build src/Playwright/Playwright.csproj -p:TargetFramework=net10.0 -p:PublishAot=true -p:TrimMode=full -p:UseSharedCompilation=false` | Pass, 0 warnings |
| Native executable publish | `dotnet publish samples/Playwright.AotSample/Playwright.AotSample.csproj -c Release -r linux-x64 -p:PublishAot=true -p:SelfContained=true -p:TrimMode=full -p:UseSharedCompilation=false` | Pass |
| Core runtime groups | `Playwright.AotSample` native executable launches Chromium and exercises transport startup, locators, page/frame/element/mouse/keyboard actions, evaluate argument/result serialization including null-to-default value types, local-server network/API round-trips, source-generated JSON, binding callbacks, screenshots, and Clearcote humanized input hooks offline (188 groups). | Pass in this environment |
| Clearcote runtime groups | `Playwright.AotSample.Clearcote` native executable resolves the verified `v0.1.0-pre.18` browser cache, launches Clearcote, round-trips profile JSON, evaluates JavaScript, probes WebGL render info, and captures a screenshot offline (6 groups). | Pass in this environment |
| CLI NativeAOT publish | `dotnet publish src/Playwright.CLI/Playwright.CLI.csproj -c Release -r linux-x64 -p:PublishAot=true -p:SelfContained=true -p:TrimMode=full -p:UseSharedCompilation=false`; native CLI run with `-p src/Playwright/Playwright.csproj --version` and quoted unknown command. | Pass |
| Test adapter and wrapper AOT analyzers | `dotnet build` with `-p:PublishAot=true -p:TrimMode=full -p:UseSharedCompilation=false` for `Playwright.TestAdapter`, `Playwright.NUnit`, `Playwright.MSTest`, `Playwright.Xunit`, and `Playwright.Xunit.v3`. | Pass, 0 warnings |
| Security helper regression tests | `dotnet test ./src/Playwright.Tests/Playwright.Tests.csproj -c Debug -f net10.0 --no-restore --filter "FullyQualifiedName~SecurityHelpersTests" --logger:"console;verbosity=detailed"` | Pass, 10/10 |
| Process-drain and security helper regression tests | `dotnet test ./src/Playwright.Tests/Playwright.Tests.csproj -c Debug -f net10.0 --no-restore --filter "FullyQualifiedName~ClearcoteProcessTests|FullyQualifiedName~SecurityHelpersTests" --logger:"console;verbosity=detailed"` | Pass, 12/12 |
| Clearcote cache/profile/path regression tests | `dotnet test ./src/Playwright.Tests/Playwright.Tests.csproj -c Debug -f net10.0 --no-restore --filter "FullyQualifiedName~ClearcoteCacheTests|FullyQualifiedName~ClearcoteExecutablePathTests|FullyQualifiedName~ClearcoteProfileTests" --logger:"console;verbosity=detailed"` | Pass, 21/21 |
| Clearcote font launch env regression tests | `dotnet test ./src/Playwright.Tests/Playwright.Tests.csproj -c Debug -f net10.0 --no-restore --filter "FullyQualifiedName~ClearcoteFontLaunchEnvTests|FullyQualifiedName~ClearcoteCacheTests|FullyQualifiedName~ClearcoteProfileTests" --logger:"console;verbosity=detailed"` | Pass, 18/18 |
| Clearcote Widevine copy-safe regression tests | `dotnet test ./src/Playwright.Tests/Playwright.Tests.csproj -c Debug -f net10.0 --no-restore --filter "FullyQualifiedName~ClearcoteWidevineTests|FullyQualifiedName~ClearcoteFontLaunchEnvTests" --logger:"console;verbosity=detailed"` | Pass, 4/4 |
| Clearcote process helper regression tests | `dotnet test ./src/Playwright.Tests/Playwright.Tests.csproj -c Debug -f net10.0 --no-restore --filter "FullyQualifiedName~ClearcoteProcessTests" --logger:"console;verbosity=detailed"` | Pass, 2/2 |
| Clearcote executable path override regression tests | `dotnet test ./src/Playwright.Tests/Playwright.Tests.csproj -c Debug -f net10.0 --no-restore --filter "FullyQualifiedName~ClearcoteExecutablePathTests" --logger:"console;verbosity=detailed"` | Pass, 4/4 |
| CLI argument and host-path regression tests | `dotnet test ./src/Playwright.Tests/Playwright.Tests.csproj -c Debug -f net10.0 --no-build --no-restore --filter "FullyQualifiedName~CLITests.DotnetHostPathShould|FullyQualifiedName~CLITests.RunWithResultShouldPreserveSingleArgumentWithSpacesAndQuotes" --logger:"console;verbosity=detailed"` | Pass, 3/3 |
| Transport shutdown regression tests | `dotnet test ./src/Playwright.Tests/Playwright.Tests.csproj -c Debug -f net10.0 --no-restore --filter "FullyQualifiedName~StdIOTransportTests" --logger:"console;verbosity=detailed"` | Pass, 3/3 |
| Evaluate argument converter regression tests | `dotnet test ./src/Playwright.Tests/Playwright.Tests.csproj -c Debug -f net10.0 --no-restore --filter "FullyQualifiedName~EvaluateArgumentValueConverterTests" --logger:"console;verbosity=detailed"` | Pass, 6/6 |
| AOT enum converter regression tests | `dotnet test ./src/Playwright.Tests/Playwright.Tests.csproj -c Debug -f net10.0 --no-restore --filter "FullyQualifiedName~AotEnumMemberConverterTests" --logger:"console;verbosity=detailed"` | Pass, 3/3 |
| Transport argument normalizer regression tests | `dotnet test ./src/Playwright.Tests/Playwright.Tests.csproj -c Debug -f net10.0 --no-restore --filter "FullyQualifiedName~ConnectionNormalizeValueTests" --logger:"console;verbosity=detailed"` | Pass, 10/10 |
| Enumerable extension regression tests | `dotnet test ./src/Playwright.Tests/Playwright.Tests.csproj -c Debug -f net10.0 --no-restore --filter "FullyQualifiedName~EnumerableExtensionsTests" --logger:"console;verbosity=detailed"` | Pass, 1/1 |
| JSON helper regression tests | `dotnet test ./src/Playwright.Tests/Playwright.Tests.csproj -c Debug -f net10.0 --no-restore --filter "FullyQualifiedName~JsonExtensionsTests" --logger:"console;verbosity=detailed"` | Pass, 2/2 |
| User JSON serializer regression tests | `dotnet test ./src/Playwright.Tests/Playwright.Tests.csproj -c Debug -f net10.0 --no-restore --filter "FullyQualifiedName~UserJsonSerializerTests" --logger:"console;verbosity=detailed"` | Pass, 7/7 |
| Typed response JSON metadata regression tests | `dotnet test ./src/Playwright.Tests/Playwright.Tests.csproj -c Debug -f net10.0 --no-restore --filter "FullyQualifiedName~PageNetworkResponseTests.ShouldWorkWithGenerics|FullyQualifiedName~BrowserContextFetchTests.ShouldParseResponseJSONWhilePassingAType|FullyQualifiedName~BrowserContextFetchTests.ShouldRequireSourceGeneratedMetadataForTypedResponseJSONOptions" --logger:"console;verbosity=detailed"` | Pass, 4/4 |
| Driver downloader archive regression tests | `dotnet test ./src/Playwright.Tests/Playwright.Tests.csproj -c Debug -f net10.0 --no-restore --filter "FullyQualifiedName~DriverDownloaderTests" --logger:"console;verbosity=detailed"` | Pass, 2/2 |
| Driver path override regression tests | `dotnet test ./src/Playwright.Tests/Playwright.Tests.csproj -c Debug -f net10.0 --no-build --no-restore --filter "FullyQualifiedName~DriverPathTests" --logger:"console;verbosity=detailed"` | Pass, 2/2 |
| Runsettings parser regression tests | `dotnet test ./src/Playwright.Tests/Playwright.Tests.csproj -c Debug -f net10.0 --no-restore --filter "FullyQualifiedName~PlaywrightSettingsXmlTests" --logger:"console;verbosity=detailed"` | Pass, 2/2 |
| Source-generation metadata regression tests | `dotnet test ./src/Playwright.Tests/Playwright.Tests.csproj -c Debug -f net10.0 --no-build --no-restore --filter "FullyQualifiedName~PlaywrightJsonContextTests" --logger:"console;verbosity=detailed"` | Pass, 1/1 |
| Driver downloader archive hardening | `src/tools/Playwright.Tooling/DriverDownloader.cs` validates downloaded tar entry paths and stages playwright-core/Node archive output before publishing into `.drivers`; `dotnet build src/tools/Playwright.Tooling/Playwright.Tooling.csproj -p:PublishAot=true -p:TrimMode=full -p:UseSharedCompilation=false` succeeds with 0 warnings. | Pass |
| Upstream offline-emulation tests | `dotnet test ./src/Playwright.Tests/Playwright.Tests.csproj -c Debug -f net10.0 --no-restore --filter "FullyQualifiedName~BrowserContextBasicTests.ShouldWorkWithOfflineOption|FullyQualifiedName~DefaultBrowserContext1Tests.ShouldSupportOfflineOption|FullyQualifiedName~PopupTests.ShouldInheritOfflineFromBrowserContext" --logger:"console;verbosity=detailed"` | Pass, 3/3 |
| NuGet consumer path | `dotnet pack` creates `Playwright.Clearcote.1.5.0.nupkg`; a throwaway `PackageReference` consumer restores/builds and receives `.playwright/package`, `node/linux-x64/node`, and `playwright.ps1`. | Pass |
| Reflection/AOT hazard scan | `rg` over `src/Playwright`, `src/Playwright.CLI`, `src/Playwright.TestAdapter`, and the test-framework wrappers finds no remaining `Activator.CreateInstance`, `MakeGenericType`, `TypeDescriptor`, `GetProperties()`, `dynamic`, `Assembly.GetName`, `CodeBase`, `Convert.ChangeType`, process-argument string launch pattern, or AOT suppressions in implementation code. Remaining audited `JsonSerializer` calls use source-generated `JsonTypeInfo`, `PlaywrightJsonContext`, `ClearcoteJsonContext`, or `TestAdapterJsonContext`. | Pass |
| Clearcote catalog/version resolution + PRO download + license lease system | New `Clearcote.Catalog.cs`, `Clearcote.License.cs`, `ClearcoteJsonContext.cs` — version catalog fetch via source-gen JSON, PRO binary download with license auth, lease checkout/heartbeat/checkin, offline token cache. No pragmas, no AOT suppressions. | Pass |
| Clearcote full test suite | 27 Clearcote tests (cache, executable path, profile, process, font launch env, Widevine copy-safe) | Pass, 27/27 |
| Library AOT build (no pragmas anywhere in new files) | `dotnet build src/Playwright/Playwright.csproj -p:TargetFramework=net10.0 -p:PublishAot=true` | Pass, 0 warnings |

Known limits that are intentional or still need broader test coverage:

- `EvaluateAsync` argument serialization no longer reflects over arbitrary POCOs. It supports primitives, strings, enums, dates, URIs, `BigInteger`, exceptions, regexes, `Guid`, dictionaries with string keys, `ExpandoObject`, enumerables, and channel owners. Unsupported arbitrary object graphs now fail explicitly.
- `Response.JsonAsync<T>()` / `APIResponse.JsonAsync<T>()` require source-generated user metadata through `JsonTypeInfo<T>` or options with a resolver.
- The full upstream Playwright test suite has not been re-run as proof for every generated API surface in this current validation pass.
- Clearcote render coherence depends on the host GPU/headless environment. Headless Linux in this workspace reports a software renderer, so the coherence probe can return `Coherent = false` even though launch and protocol support work correctly.

## Original audit summary

**Date:** 2026-07-06
**Scope:** `src/Playwright/` (core library), `src/Playwright.CLI/`, `src/Playwright.TestAdapter/`
**Excluded from audit:** Test projects (still run on net8.0+ via reflection), NUnit/MSTest/Xunit wrappers (only reference the library, their own reflection usage is out of scope for the library AOT goal).

Original finding before the NativeAOT fork changes: the core library targeted
**netstandard2.0** with `System.Text.Json` 6.x. All JSON serialization used
reflection-based `JsonSerializerOptions` without source generation. The
message-passing protocol was `Dictionary<string, object?>` everywhere — the
biggest AOT challenge.

**Counts:**
- Hard issues: 6
- Medium issues: 12
- Easy issues: 5

---

## 1. `Transport/Converters/ChannelOwnerConverterFactory.cs`

| Field | Value |
|-------|-------|
| Function | `ChannelOwnerConverterFactory.CreateConverter` / `ChannelOwnerToGuidConverter<T>.Read` |
| Problem | `Activator.CreateInstance(typeof(ChannelOwnerToGuidConverter<>).MakeGenericType(...))` — runtime type construction. `JsonDocument.ParseValue(ref reader)` allocates unnecessarily for a simple guid lookup. |
| Warning | IL3050 (MakeGenericType), IL2067 (Activator.CreateInstance on closed type) |
| Suggested fix | Replace `JsonConverterFactory` with a non-generic `WriteOnlyJsonConverter<ChannelOwner>` that always writes `{"guid":"..."}`. For reading, use a lookup converter that reads the guid string directly without `JsonDocument`. |
| Difficulty | hard |

## 2. `Transport/Converters/JsonStringEnumMemberConverter.cs`

| Field | Value |
|-------|-------|
| Function | `JsonStringEnumMemberConverter.CreateConverter` |
| Problem | `Activator.CreateInstance(typeof(EnumMemberConverter<>).MakeGenericType(...))` — same pattern. Also `Type.GetTypeCode(typeof(TEnum))` is a reflection call. |
| Warning | IL3050 (MakeGenericType), IL2067 (Activator.CreateInstance) |
| Suggested fix | Replace with `JsonStringEnumConverter` (built-in, AOT-safe when used with `JsonStringEnumConverterOptions`). Or generate explicit per-enum converters. |
| Difficulty | hard |

## 3. `Transport/Converters/EvaluateArgumentValueConverter.cs`

| Field | Value |
|-------|-------|
| Function | `Serialize` — line 197: `TypeDescriptor.GetProperties(value)` |
| Problem | `TypeDescriptor.GetProperties` uses runtime type descriptor discovery — requires `RequiresUnreferencedCode`. `Activator.CreateInstance(t)` (lines 244, 258) for array/list target types. `ExpandoObject` usage (lines 149, 211, 253, 393). `ObjectIDGenerator` (line 422) uses `__Identity` which is internal to `System.ObjectModel`. `Convert.ChangeType` (line 301) — reflection-based. |
| Warning | IL2026 (TypeDescriptor.GetProperties), IL2067 (Activator.CreateInstance), IL2070 (GetProperties on arbitrary type), AOT analysis warnings |
| Suggested fix | This is the hardest file. The `Serialize` path handles user-supplied objects for `EvaluateAsync` — inherently dynamic. Option A: annotate with `RequiresDynamicCode` and document. Option B: rewrite as a closed-form visitor with source-generatable payload structs for known types, fallback to `RequiresDynamicCode` for unknown user types. The `Deserialize` path can be rewritten to avoid `Activator.CreateInstance` by switching on known target types. |
| Difficulty | hard |

## 4. `Transport/Connection.cs`

| Field | Value |
|-------|-------|
| Function | `InnerSendMessageToServerAsync` — lines 171-176 |
| Problem | `Dictionary<string, object?>` everywhere — the entire protocol message is an untyped dictionary. LINQ: `.Where(f => f.Value != null).ToDictionary(...)` on every message send (line 174-176), `.Any()` (line 236), `.FirstOrDefault()` (line 237). `typeof(ChannelOwner).IsAssignableFrom(typeof(T))` and `typeof(ChannelOwner[]).IsAssignableFrom(typeof(T))` (line 232). |
| Warning | IL2070 (IsAssignableFrom on arbitrary T), performance (LINQ allocation on hot path) |
| Suggested fix | Replace `Dictionary<string, object?>` with typed message classes for the transport protocol (e.g. `PlaywrightRequestMessage` with typed fields). Remove LINQ from hot path — use raw loops. Replace `IsAssignableFrom` checks with a constrained generic or explicit type switch. |
| Difficulty | medium |

## 5. `Transport/PlaywrightServerMessage.cs`

| Field | Value |
|-------|-------|
| Function | `PlaywrightServerMessage` / `ErrorEntry` |
| Problem | Uses `JsonElement` for `Params`, `Result`, `ErrorDetails` — requires reflection when deserialized via `JsonSerializer.Deserialize<PlaywrightServerMessage>(bytes, options)`. |
| Warning | IL2026 (reflection-based deserialization) |
| Suggested fix | Add source-generated `JsonSerializerContext` that includes `PlaywrightServerMessage`, `ErrorEntry`, `PlaywrightServerError`. Replace `JsonSerializer.Deserialize<T>(bytes, options)` with `JsonSerializer.Deserialize(bytes, MyContext.Default.PlaywrightServerMessage)`. |
| Difficulty | medium |

## 6. `Playwright.cs`

| Field | Value |
|-------|-------|
| Function | `CreateAsync` — line 48: `JsonSerializer.Deserialize<PlaywrightServerMessage>(message, JsonExtensions.DefaultJsonSerializerOptions)` |
| Problem | Reflection-based deserialization of every incoming message. |
| Warning | IL2026 |
| Suggested fix | Use source-generated context instead of `JsonExtensions.DefaultJsonSerializerOptions`. |
| Difficulty | medium |

## 7. `Playwright.cs`

| Field | Value |
|-------|-------|
| Function | `CreateAsync` — line 63: `JsonSerializer.SerializeToUtf8Bytes(message, keepNulls ? ...)` |
| Problem | `message` is `Dictionary<string, object?>` — serialized with reflection options. |
| Warning | IL2026 |
| Suggested fix | Replace message dictionary with typed `PlaywrightRequestMessage` that has a source-generated serializer. |
| Difficulty | medium |

## 8. `Helpers/JsonExtensions.cs`

| Field | Value |
|-------|-------|
| Function | `ToObject<T>`, `ToObject(Type,...)`, `ToJson<T>`, `GetNewDefaultSerializerOptions` |
| Problem | All methods use `JsonSerializerOptions` without source generation. `ToObject(this JsonElement element, Type type, ...)` uses runtime `Type` argument — cannot be AOT-compiled. |
| Warning | IL2026, IL2070 |
| Suggested fix | Replace callsites with source-generated context calls. Remove the `Type`-based overload entirely (or annotate with `RequiresUnreferencedCode`). |
| Difficulty | medium |

## 9. `Helpers/ChannelHelpers.cs` ✅ FIXED

| Field | Value |
|-------|-------|
| Function | `ToObject(this Exception exception)` — line 83 |
| Problem | Returns `dynamic` — an anonymous type boxed as dynamic. |
| Warning | IL3050 (dynamic requires code generation) |
| Fix | Changed return type to `JsonObject` (registered in `PlaywrightJsonContext`), eliminating both the anonymous type and the implicit `dynamic` concern. Binding error serialization is now AOT-safe. |
| Difficulty | easy |

## 10. `Helpers/Driver.cs`

| Field | Value |
|-------|-------|
| Function | `GetExecutablePath` — line 40: `typeof(Driver).Assembly.GetName().Version.ToString(3)` |
| Problem | `Assembly.GetName()` throws in single-file/NativeAOT publish when the assembly is loaded from a bundle. |
| Warning | IL3000 (single-file), SYSLIB0012 |
| Suggested fix | Read version from `AssemblyInformationalVersionAttribute` or from a generated constant at build time. |
| Difficulty | medium |

## 11. `Helpers/Driver.cs`

| Field | Value |
|-------|-------|
| Function | `TryGetCodeBase` — line 105: `assembly.CodeBase` |
| Problem | `assembly.CodeBase` throws `NotSupportedException` in single-file publish (already has a try/catch, but the fallback is fragile). |
| Warning | IL3002 |
| Suggested fix | Simplify: use `AppContext.BaseDirectory` only (already the first path tried). Remove `CodeBase` path entirely for AOT builds. |
| Difficulty | easy |

## 12. `Helpers/ClassUtils.cs`

| Field | Value |
|-------|-------|
| Function | `Clone<T>` |
| Problem | `source.GetType().GetProperties()` followed by `targetType.GetProperty(...)` — runtime reflection on arbitrary types. |
| Warning | IL2070 |
| Suggested fix | Replace with a source-generated mapper or constrain to known types. Currently only called from `Locator.cs:ConvertOptions<T>` — inline the copy logic per type. |
| Difficulty | medium |

## 13. `Helpers/EnumHelper.cs`

| Field | Value |
|-------|-------|
| Function | `ToValueString<TEnum>`, `FromValueString<TEnum>` |
| Problem | `typeof(TEnum).GetField(t)` and `field.GetCustomAttribute<EnumMemberAttribute>()` — reflection on enum fields. Every call re-caches, but cache key is `typeof(TEnum)` which is fine; the reflection itself triggers IL2070. |
| Warning | IL2070 |
| Suggested fix | If `JsonStringEnumMemberConverter` is replaced with `JsonStringEnumConverter`, this helper becomes unused and can be removed. Otherwise, generate enum-to-string maps at build time with source generators. |
| Difficulty | medium |

## 14. `Transport/Converters/ChannelOwnerListToGuidListConverter.cs`

| Field | Value |
|-------|-------|
| Function | `CanConvert` — line 42-43 |
| Problem | `type.IsArray && type.GetElementType() == typeof(T)` — reflection check. Also a `JsonConverterFactory`-like pattern. |
| Warning | IL2070 |
| Suggested fix | Convert to a non-generic converter that writes `IEnumerable<ChannelOwner>` by reading `guid` from each item. |
| Difficulty | medium |

## 15. `Core/BindingCall.cs` ✅ FIXED (partial)

| Field | Value |
|-------|-------|
| Function | `CallAsync` — binding arg deserialization + Task result extraction |
| Problem | (1) `_initializer.Args[i]` was passed as raw `JsonElement` to `DynamicInvoke`, causing `JsonElement → int` conversion failure. (2) `Task<T>.Result` extraction via `typeof(Task<>).GetProperty("Result")` failed at runtime with "ContainsGenericParameters is true". |
| Fix | (1) Args are now deserialized via `EvaluateArgumentValueConverter.Deserialize(argElement, targetType)`. (2) Removed `Task<T>.Result` extraction — non-generic `Task` is supported (awaited, result = null). `Task<T>` async bindings are a known AOT limitation. |
| Warning | No remaining warnings. |
| Difficulty | medium |

## 16. `Core/Locator.cs`

| Field | Value |
|-------|-------|
| Function | `ConvertOptions<T>` (lines 409-429) |
| Problem | `source.GetType().GetProperties()`, `targetType.GetProperty(name)`, `SetValue/GetValue` — runtime reflection on option types. |
| Warning | IL2070 |
| Suggested fix | Generate closed-form copy methods per option type, or constraint the generic to known types and use static interfaces. |
| Difficulty | medium |

## 17. `Core/Locator.cs`

| Field | Value |
|-------|-------|
| Function | `_locatorSerializerOptions` (line 44) |
| Problem | `new JsonSerializerOptions()` without source generation — used for `JsonSerializer.Serialize` calls (lines 89, 100, 221, 260, 269, 477, 644). |
| Warning | IL2026 |
| Suggested fix | Replace with source-generated context or create minimal serializer for the known types (string selectors only). |
| Difficulty | easy |

## 18. `Playwright.CLI/Program.cs`

| Field | Value |
|-------|-------|
| Function | `Main` — line 79-82: `Assembly.LoadFile(file); dynamic c = dll.CreateInstance(...)` |
| Problem | `Assembly.LoadFile` loads arbitrary assembly at runtime. `dynamic` + `CreateInstance` requires runtime code generation. |
| Warning | IL3050, IL3000 |
| Suggested fix | The CLI project is an external tool, not the library itself. For AOT, this project needs a different approach — either ship it separately (non-AOT) or rewrite it as a simple process launcher that delegates to the AOT-published binary. |
| Difficulty | medium |

## 19. `Helpers/SetInputFilesHelpers.cs`

| Field | Value |
|-------|-------|
| Function | Lines 89-92, 99, 108, 121 |
| Problem | `Dictionary<string, object?>` patterns, `.ToObject<WritableStream>`, `.ElementAt(i).ToObject<WritableStream>` — reflection-based deserialization. |
| Warning | IL2026 |
| Suggested fix | Use source-generated context for `WritableStream` deserialization. Replace `ElementAt(i)` with direct indexer. |
| Difficulty | easy |

## 20. `Core/Page.cs`, `Core/BrowserContext.cs`, `Core/Frame.cs` (event dispatch)

| Field | Value |
|-------|-------|
| Function | Various `OnMessage` handlers — e.g. `serverParams.GetProperty("binding").ToObject<BindingCall>(...)` |
| Problem | Every event dispatch deserializes `JsonElement` properties into concrete types using `ToObject<T>(DefaultJsonSerializerOptions)` — reflection based. Widespread across ~40 callsites. |
| Warning | IL2026 |
| Suggested fix | Replace `ToObject<T>(options)` with source-generated deserialization. Each `GetProperty("X").ToObject<T>(...)` becomes `JsonSerializer.Deserialize(ref source, MyContext.Default.T)`. |
| Difficulty | medium |

## 21. `Transport/Protocol/Generated/*.cs` (68 initializer classes)

| Field | Value |
|-------|-------|
| Function | All 68 `*Initializer` classes |
| Problem | No `JsonSerializable` attributes. When deserialized via `ToObject<T>(options)`, the trimmer cannot preserve the necessary members. |
| Warning | IL2026 |
| Suggested fix | Add a central `[JsonSerializable]` attribute on `JsonSerializerContext` for every initializer type. These are simple DTOs — easy to source-generate metadata for. |
| Difficulty | medium |

## 22. `Transport/StdIOTransport.cs`

| Field | Value |
|-------|-------|
| Function | `GetProcess` — line 109: `Driver.GetExecutablePath()` |
| Problem | Driver path resolution uses `Assembly.CodeBase` and `Assembly.GetName()` (see Driver.cs issues). The process launch itself is fine (no reflection), but the path resolution breaks in single-file. |
| Warning | IL3000, IL3002 |
| Suggested fix | Fix Driver.cs first. The transport itself is AOT-safe as-is — it reads bytes from stdin/stdout. |
| Difficulty | medium (depends on Driver.cs fix) |

## 23. `Program.cs`

| Field | Value |
|-------|-------|
| Function | `CreateDriverProcess` — line 97-98: `Driver.GetExecutablePath()` |
| Problem | Same as StdIOTransport — depends on Driver.cs. This `Program.cs` is the CLI entrypoint (`dotnet Microsoft.Playwright.dll`). |
| Warning | IL3000 |
| Suggested fix | After fixing Driver.cs, this should work. The process launch itself uses `ProcessStartInfo` which is AOT-safe. |
| Difficulty | easy (after Driver.cs fix) |

## 24. `Core/Request.cs`, `Core/Response.cs`, `Core/APIRequestContext.cs`, `Core/APIResponse.cs`

| Field | Value |
|-------|-------|
| Function | `JsonDocument.Parse(content).RootElement` (Request.cs:182, Response.cs:108), `JsonSerializer.Deserialize<JsonElement>(await BodyAsync())` (APIResponse.cs:99) |
| Problem | `JsonDocument.Parse` is okay, but the subsequent `.ToObject<T>()` calls use reflection. Also `JsonSerializer.Deserialize<JsonElement>(...)` uses reflection options. |
| Warning | IL2026 |
| Suggested fix | Replace `JsonSerializer.Deserialize<JsonElement>(...)` with source-gen context. The `JsonDocument.Parse` pattern is fine for raw JSON access. |
| Difficulty | easy |

---

## Patterns not found (good)

- `Newtonsoft.Json`, `JObject`, `JToken` — not used
- `Expression.Compile` — not used
- `DispatchProxy` — not used
- `Reflection.Emit` — not used
- `RequiresUnreferencedCode` / `RequiresDynamicCode` / `UnconditionalSuppressMessage` — none present (clean slate, but means no AOT annotations exist anywhere)

## Priority order for fixes

1. **`Transport/PlaywrightServerMessage.cs` + `Playwright.cs`** — add source-generated `JsonSerializerContext` for the wire protocol. This is the central choke point.
2. **`Transport/Converters/ChannelOwnerConverterFactory.cs`** — replace with non-generic write-only converter.
3. **`Transport/Converters/JsonStringEnumMemberConverter.cs`** — replace with `JsonStringEnumConverter` or generated per-enum converters.
4. **`Helpers/ChannelHelpers.cs`** — remove `dynamic` (easy win).
5. **`Helpers/Driver.cs`** — fix `Assembly.GetName()` and `CodeBase` for single-file.
6. **`Helpers/EnumHelper.cs`** — if enum converter is replaced, this becomes dead code.
7. **`Helpers/JsonExtensions.cs`** — migrate remaining callsites to source-gen context.
8. **`Transport/Connection.cs`** — reduce LINQ, replace dictionary messages with typed classes.
9. **`Transport/Converters/EvaluateArgumentValueConverter.cs`** — hardest, tackle last.
10. **`Core/Locator.cs`**, **`Helpers/ClassUtils.cs`**, **`Core/BindingCall.cs`** — fix reflection-based property copiers.
11. **68 `Transport/Protocol/Generated/*.cs`** — add `[JsonSerializable]` attributes.
