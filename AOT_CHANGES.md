# NativeAOT Fork Changes

## Clearcote launch patch

- Normal Chromium launches keep using the regular Playwright Chromium binary. Clearcote is opt-in via `ClearcoteLaunchOptions`, `ClearcoteLaunchPersistentContextOptions`, the `ClearcoteBrowser.*Launch*` helpers, or `CLEARCOTE=1`.
- `ClearcoteLaunchOptions` and `ClearcoteLaunchPersistentContextOptions` expose the Clearcote fingerprint, language, WebRTC, proxy, canvas bridge, cache, and privacy-sandbox settings without editing generated API files.
- The Clearcote binary resolver mirrors the Node SDK's pinned mode:
  - `CLEARCOTE_BINARY` overrides the browser path only when it is a fully-qualified path to an existing file.
  - `CLEARCOTE_CACHE` overrides the cache root.
  - Linux x64 and Windows x64 releases are pinned with archive and inner-binary SHA-256 checks.
  - Linux `.tar.xz` extraction delegates to system `tar`; Windows uses `ZipFile`.
- `AutoUpdate` / `CLEARCOTE_AUTO_UPDATE=1` mirrors the Node SDK's opt-in latest-release mode:
  - resolves the newest compatible GitHub release asset for the current platform,
  - verifies the archive against `SHA256SUMS.txt`,
  - verifies `SHA256SUMS.txt.asc` against the pinned signing-key fingerprint when `gpg` is available,
  - falls back to the pinned release when GitHub metadata is unavailable.
- `ClearcoteBrowser.ExecutablePathAsync()` and `ClearcoteBrowser.DownloadAsync()` mirror the Node SDK's `executablePath()` and `download()` helpers.
- The launch argument patch mirrors the Node SDK's core behavior:
  - removes `--enable-automation` unless the caller provided ignore defaults,
  - applies saved profiles from `Profile`,
  - applies in-browser AI agent flags (`agentLlmUrl`, `agentLlmKey`, `agentModel`, `agentToolMode`, `agentTyping`),
  - applies fingerprint flags,
  - normalizes `Accept-Language` and `--lang`,
  - disables Privacy Sandbox runtime features by default,
  - disables QUIC when a proxy is configured,
  - defaults WebRTC to `disable_non_proxied_udp` unless the caller or persona IP overrides it,
  - loads unpacked extension directories via `--load-extension` and `--disable-extensions-except`,
  - supports `Geoip` for proxy geo lookup using HTTPS-only online fallback paths,
  - downloads and caches the `geoip-all-in-one` MaxMind MMDB at runtime, decodes it with an internal AOT-safe reader, and uses it before falling back to HTTPS lookup,
  - emits launch coherence warnings for common proxy, platform, GPU, brand/version, and automation-arg mismatches,
  - collapses duplicate `--enable-features` and `--disable-features` flags.
- Clearcote high-level SDK helpers are ported to .NET:
  - `ClearcoteProfile` persists and loads saved personas from `CLEARCOTE_PROFILE_DIR` / `~/.clearcote/profiles`.
  - `ClearcoteProfile.Save("name.json")` now supports explicit bare JSON filenames without attempting to create an empty parent directory, and profile paths are canonicalized before reads/writes.
  - `CLEARCOTE_PROFILE_DIR` must be a fully-qualified path so named profile reads/writes cannot silently resolve relative to the current working directory.
  - Clearcote profiles now persist and apply inherited persistent-context persona options such as locale, user agent, proxy, geolocation, viewport/screen size, permissions, extra headers, HTTP credentials, and media emulation settings.
  - Clearcote profile save/load now persists launch-base options with an AOT-safe JSON writer/parser, including environment variables, artifact/download/trace directories, default-argument controls, signal handling, timeout, slow motion, and Firefox user preferences.
  - Clearcote profiles used with `LaunchAsync` now carry launch-compatible options from persistent profiles, including proxy, environment, downloads/artifacts/traces directories, default-arg controls, timeout, slow motion, and Firefox user prefs.
  - `Humanize` and `ShowCursor` options attach to new pages/contexts and route .NET `Mouse`, `Keyboard.TypeAsync`, page/frame selector actions, and locator-backed selector actions through native humanized timing where possible.
  - Linux fontconfig launch support now writes generated `fonts.conf` files into a secure temp cache instead of mutating the verified browser fonts directory.
  - `ClearcoteBrowser.LaunchAgentAsync()` launches a persistent context for Chrome Actor, and `RunAgentTaskAsync()` calls `Browser.agentRunTask` over CDP.
  - `ClearcoteBrowser.CheckRenderCoherenceAsync()` probes WebGL vendor/renderer and returns the render coherence verdict.
  - `ClearcoteBrowser.FetchWidevineAsync()` and `SeedWidevineAsync()` fetch, verify, extract, and seed the opt-in Widevine CDM; persistent launch supports `Widevine = true`. Widevine seeding now copies through a staging directory, refuses to overwrite existing files, and rejects source reparse points.
- Clearcote browser, GeoIP MMDB, and Widevine ZIP extraction now rejects path traversal and symlink entries. ZIP extraction preflights every entry and destination conflict before writing files, so an unsafe late archive entry cannot leave partial output behind. Clearcote metadata/download URLs, GeoIP exit-IP probes, and Widevine CDM downloads are required to be HTTPS, and Widevine versions are validated as single path segments before use as cache directory names.
- Clearcote browser downloads now extract and verify into a staging browser directory and temporary archive path. The existing verified browser cache is replaced only after archive SHA-256, optional GPG, extraction, executable lookup, and executable SHA-256 checks pass; publish uses a backup/restore step so a failed stamp write restores the previous browser directory. Cache hits now require the `.verified` stamp to match the expected archive SHA-256 and, when available, the cached executable hash to match the expected executable SHA-256.
- `ClearcoteBrowser.DownloadAsync(Dest=...)` now validates and canonicalizes the destination, copies the verified binary into a staging file in the destination directory, applies executable permissions there, and only then replaces the requested destination.
- Clearcote cache roots are canonicalized before use. `CLEARCOTE_CACHE` and `XDG_CACHE_HOME` must be fully-qualified paths, while explicit `CacheDir` options are resolved to full paths before cache directory creation, moves, or deletes.
- GeoIP MMDB cache refresh now downloads/extracts into unique staging paths and replaces the cached database only after a valid `.mmdb` is found, preserving the previous cache if refresh fails.
- `ClearcoteBrowser.ExecutablePathAsync()` now validates explicit `ExecutablePath` and `CLEARCOTE_BINARY` overrides as fully-qualified paths to existing files before returning them to launch/serve code.
- Clearcote Linux `.tar.xz` extraction now lists and validates archive entry names and verbose entry types before invoking `tar -xf`, rejecting absolute paths, traversal, empty path segments, drive/ADS-style names, symlinks, hardlinks, and special files before any files are written.
- Clearcote GPG verification now resolves `gpg` through the hardened tool lookup before running it, so signature verification is not accidentally skipped by resolving `"gpg"` relative to the current directory. The Clearcote process helper now drains stdout/stderr concurrently with a timeout to avoid deadlocks on verbose child processes; tar listing/extraction uses the same drained helper with archive-sized timeouts.
- `Clearcote.ServeAsync` no longer redirects the long-running browser process stdout/stderr into unread pipes, avoiding browser hangs when verbose output fills an OS pipe.
- Driver/CLI process launches now pass `cli.js` and all user/driver arguments through `ProcessStartInfo.ArgumentList` instead of building a quoted argument string, avoiding quote parsing bugs for arguments containing spaces or quotes.
- Runtime driver resolution now requires `PLAYWRIGHT_NODEJS_PATH` and `PLAYWRIGHT_DRIVER_SEARCH_PATH` to be fully-qualified paths, requires the driver search path to be a directory, and validates both the Node executable and `.playwright/package/cli.js` before returning a driver process path.
- `Playwright.CLI` now accepts `DOTNET_HOST_PATH` only when it is a fully-qualified path to an existing `dotnet` / `dotnet.exe` file; relative or malformed overrides fall back to the normal `dotnet` host instead of running a file from the current directory.
- Security helper tool lookup now validates tool names as single path segments, handles multi-line `which`/`where` output by selecting the first existing candidate, and drains `which`/`where` stdout/stderr while enforcing the lookup timeout.
- Proxy server validation now rejects control characters, embedded credentials, paths, queries, and fragments; Clearcote launch proxy patching normalizes proxy servers through this validator before passing them to Chromium or Playwright.
- Public file-write paths for `Artifact.SaveAsAsync`, `ElementHandle.ScreenshotAsync`, and `APIRequestContext.StorageStateAsync` now go through `SecurityHelpers.ResolveAndValidatePath`, matching the existing page screenshot/PDF/storage-state validation.

## NuGet packaging

- Package id is `Playwright.Clearcote`; assembly/title remain `Microsoft.Playwright` / `Playwright.AOTFork` for compatibility with the forked binary.
- The fork now targets only `net10.0`; the previous `net8.0`/`netstandard2.0` compatibility targets were removed from the fork packages.
- The package includes the bundled Playwright Node driver under `.playwright`.
- `build/Playwright.Clearcote.targets` and `buildTransitive/Playwright.Clearcote.targets` copy the platform Node binary, driver package, and `playwright.ps1` into consumer build and publish outputs.
- A consumer `PackageReference` imports the transitive target automatically; no separate post-install script is required.
- `src/tools/Playwright.Tooling` now validates archive entry destinations and stages downloaded `playwright-core` / Node archive contents before publishing into `.drivers`, rejecting rooted, traversal, empty, current-directory, duplicate, and drive/ADS-style path segments without leaving partial package output behind.

## AOT hardening

- Removed the API compatibility suppression file and disabled old package-baseline comparison instead of carrying obsolete suppressions.
- Removed `IL2026`/`IL3050` pragmas from `JsonExtensions`.
- Removed reflection-based fallback deserialization in protocol JSON helpers. Missing source-generated metadata now throws instead of silently falling back.
- Added `JsonTypeInfo<T>` overloads for `IResponse.JsonAsync<T>` and `IAPIResponse.JsonAsync<T>`.
- Existing generic JSON response methods no longer call reflection-based `JsonSerializer.Deserialize<T>`; they require source-generated metadata or fail with an explicit message. `IResponse.JsonAsync<T>()` and `IAPIResponse.JsonAsync<T>(JsonSerializerOptions?)` now share the same metadata resolver, and serializer options without `TypeInfoResolver` fail before any reflection fallback is attempted.
- `ChannelHelpers.ToObject(Exception)` no longer returns an anonymous type boxed as `object`. It returns `JsonObject` (registered in `PlaywrightJsonContext`), making binding error serialization AOT-safe.
- `BindingCall.CallAsync` now deserializes binding arguments from protocol `JsonElement` to the target parameter type via `EvaluateArgumentValueConverter.Deserialize`, fixing a previous `JsonElement` → `int` conversion failure.
- `BindingCall.CallAsync` removed the static `TaskResultProperty` (`typeof(Task<>).GetProperty("Result")`) field that failed at runtime for `Task<T>`. Async binding delegates returning `Task<T>` have a known AOT limitation: the return value is not extracted. Use sync delegates or non-generic `Task` instead.
- `EvaluateArgumentValueConverter` now clones protocol `ref` targets when materializing evaluate results as JSON, so repeated or cyclic JavaScript references do not attach the same `JsonNode` in multiple places.
- `EvaluateArgumentValueConverter` no longer uses `Convert.ChangeType` to materialize default values for non-nullable evaluate/binding return types; it uses an explicit AOT-safe value-type map.
- `EvaluateArgumentValueConverter.RegisterTypeInfo` now keys registrations by `JsonTypeInfo.Type`, so source-generated metadata objects are accepted reliably.
- Evaluate argument dictionary serialization now treats empty dictionaries as JS objects and rejects non-string keys explicitly instead of silently stringifying mixed dictionary keys.
- Registered source-generated evaluate argument DTOs now serialize their generated `JsonNode` / `JsonElement` output structurally, including primitive, array, nested object, and null values.
- Evaluate argument enum serialization now respects non-`int` enum backing types instead of invalidly unboxing every enum as `int`.
- Evaluate result deserialization now materializes byte-sized, short, unsigned, `ulong`, and `char` value types through explicit checked conversions instead of only supporting `int`/`long`/floating-point primitives.
- `JsonExtensions.GetNewDefaultSerializerOptions()` now attaches `PlaywrightJsonContext.Default`, so static default options and `JsonDocument.ToObject<T>()` paths resolve source-generated metadata instead of relying on reflection defaults.
- `AotEnumMemberConverter` now preserves all known non-lowercase `[EnumMember]` wire names, including channel owner types, and reads numeric enum tokens using the target enum's actual backing type.
- `Connection.NormalizeValue` now recursively normalizes protocol argument dictionaries, lists, general enumerables, and object-valued key-value pairs before source-generated DTO fallback, preserving nested `JsonElement` values, byte-array base64 behavior, unsigned/small numeric primitives, shared non-cyclic references, and rejecting cycles or non-string dictionary keys explicitly. Unsupported arbitrary protocol objects now fail deterministically instead of being stringified or passed to the serializer as raw objects. Outgoing protocol messages now preserve top-level null args when `keepNulls` is requested instead of dropping those keys before serialization.
- `APIRequestContextOptions.Params` now serializes object values with invariant formatting and JSON-style boolean casing instead of culture-sensitive `ToString()` and null-forgiving conversion.
- `UserJsonSerializer` now recursively normalizes JSON request/fulfill body dictionaries, lists, and object-valued key-value pairs into `JsonNode` values, preserving byte-array base64 behavior, explicitly writing small/unsigned numeric primitives, floats, and chars without metadata fallback, rejecting non-string dictionary keys instead of silently stringifying them, and failing cyclic collections with a `PlaywrightException` instead of recursing into serializer cycle failures.
- `Playwright.CLI` no longer uses `Assembly.LoadFile`, `CreateInstance`, or `dynamic` to invoke project-local Playwright. It delegates out-of-process to the located `Microsoft.Playwright.dll` through the dotnet host with `ArgumentList`, preserving project-local version behavior while making the CLI NativeAOT-publishable.
- `Playwright.CLI -p <project.csproj>` now resolves the containing directory with `Path.GetDirectoryName`, skips reparse points during traversal, and chooses the highest target framework/newest `Microsoft.Playwright.dll` instead of the first recursive match, avoiding stale `net8.0` binaries when `net10.0` output is present.
- `StdIOTransport.Close` now bounds driver shutdown wait time and kills the driver process tree if closing stdin does not make it exit, avoiding indefinite native-app shutdown hangs. The transport reader task is now unwrapped correctly, runs on `TaskScheduler.Default`, closes the transport on stdout EOF instead of spinning on zero-byte reads, and rejects zero, negative, or oversized driver message frames.
- `Playwright.TestAdapter` no longer uses a reflection-based runsettings object binder. `PlaywrightSettingsXml` now parses the supported `BrowserTypeLaunchOptions` and nested `Proxy` settings with closed switches, and JSON list/dictionary fragments use `TestAdapterJsonContext`.
- `PlaywrightSettingsProvider` now persists adapter settings and serializes launch options with source-generated metadata. NUnit, MSTest, Xunit, and Xunit.v3 wrappers use `PlaywrightSettingsProvider.SerializeLaunchOptions()` for `x-playwright-launch-options` instead of constructing reflection-based `JsonSerializerOptions`.
- `ClearcoteServeOptions` is registered in `PlaywrightJsonContext`, and a regression test now verifies every public `Microsoft.Playwright.*Options` type has source-generated metadata.
- Added version-catalog resolution (`Clearcote.Catalog.cs`) — fetches and caches available Clearcote browser versions from the upstream API, with version-plan filtering and PRO-only download eligibility, all AOT-safe with `ClearcoteJsonContext`.
- Added PRO binary download (`Clearcote.Catalog.cs`) — downloads PRO Clearcote builds with license-key authorization, verifying archive SHA-256 + GPG signature before extraction into the verified cache.
- Added license/lease system (`Clearcote.License.cs`) — checkout/heartbeat/checkin lifecycle against the Clearcote license server, offline token cache, configurable lease duration and retry policy.
- Added `ClearcoteJsonContext` (`ClearcoteJsonContext.cs`) — `JsonSerializerContext` for all catalog, version-plan, and license-lease DTOs, registered in `PlaywrightJsonContext` default options.
- All new files are AOT-clean: no pragmas, no `Activator.CreateInstance`, no `dynamic`, no `MakeGenericType`.

## .NET-specific implementation notes

- The offline GPL MaxMind database is downloaded and cached at runtime like the Node SDK; it is not vendored into this NuGet package and does not require the reflection-heavy `MaxMind.Db` reader package.
- JavaScript-style monkey-patching is implemented as .NET core routing: supported .NET page/frame/locator/mouse/keyboard paths are humanized when `Humanize = true`, while unsupported action shapes fall back to Playwright's native implementation.

## Validation status

- `dotnet build src/Playwright/Playwright.csproj -p:TargetFramework=net10.0 -p:PublishAot=true -p:TrimMode=full -p:UseSharedCompilation=false` passes with 0 warnings.
- `dotnet test ./src/Playwright.Tests/Playwright.Tests.csproj -c Debug -f net10.0 --no-restore --filter "FullyQualifiedName~ClearcoteCacheTests|FullyQualifiedName~ClearcoteExecutablePathTests|FullyQualifiedName~ClearcoteProfileTests|FullyQualifiedName~ClearcoteProcessTests|FullyQualifiedName~ClearcoteFontLaunchEnvTests|FullyQualifiedName~ClearcoteWidevineTests" --logger:"console;verbosity=detailed"` passes 27/27 Clearcote tests: cache integrity, download destination staging, executable path override, cache/profile root validation, profile persistence, process drain/serve, font launch env, and Widevine copy-safe regression tests.
- `dotnet publish samples/Playwright.AotSample/Playwright.AotSample.csproj -c Release -r linux-x64 -p:PublishAot=true -p:SelfContained=true -p:TrimMode=full -p:UseSharedCompilation=false` passes and the native executable exercises launch, locators, evaluate arguments/results including null-to-default value types, route interception, network/API request round-trips, source-gen JSON deserialization, cookie round-trips, sync/async binding invocation with error serialization, locator/page/frame/element/mouse/keyboard actions, `Console` event listeners, offline local-server fetches, screenshots, and Clearcote humanized input hooks offline (188 test groups).
- `dotnet test ./src/Playwright.Tests/Playwright.Tests.csproj -c Debug -f net10.0 --no-restore --filter "FullyQualifiedName~SecurityHelpersTests" --logger:"console;verbosity=detailed"` passes 10/10 security helper regression tests.
- `dotnet test ./src/Playwright.Tests/Playwright.Tests.csproj -c Debug -f net10.0 --no-restore --filter "FullyQualifiedName~ClearcoteCacheTests|FullyQualifiedName~ClearcoteExecutablePathTests|FullyQualifiedName~ClearcoteProfileTests" --logger:"console;verbosity=detailed"` passes 21/21 Clearcote cache integrity, download destination staging, executable path override, cache/profile root validation, and profile persistence regression tests.
- `dotnet test ./src/Playwright.Tests/Playwright.Tests.csproj -c Debug -f net10.0 --no-restore --filter "FullyQualifiedName~ClearcoteFontLaunchEnvTests|FullyQualifiedName~ClearcoteCacheTests|FullyQualifiedName~ClearcoteProfileTests" --logger:"console;verbosity=detailed"` passes 18/18 Clearcote font launch env, cache integrity, cache/profile root validation, and profile persistence regression tests.
- `dotnet test ./src/Playwright.Tests/Playwright.Tests.csproj -c Debug -f net10.0 --no-restore --filter "FullyQualifiedName~ClearcoteWidevineTests|FullyQualifiedName~ClearcoteFontLaunchEnvTests" --logger:"console;verbosity=detailed"` passes 4/4 Clearcote Widevine copy-safe and font launch env regression tests.
- `dotnet test ./src/Playwright.Tests/Playwright.Tests.csproj -c Debug -f net10.0 --no-restore --filter "FullyQualifiedName~EvaluateArgumentValueConverterTests" --logger:"console;verbosity=detailed"` passes 6/6 evaluate argument converter regression tests.
- `dotnet test ./src/Playwright.Tests/Playwright.Tests.csproj -c Debug -f net10.0 --no-restore --filter "FullyQualifiedName~AotEnumMemberConverterTests" --logger:"console;verbosity=detailed"` passes 3/3 AOT enum converter regression tests.
- `dotnet test ./src/Playwright.Tests/Playwright.Tests.csproj -c Debug -f net10.0 --no-restore --filter "FullyQualifiedName~ConnectionNormalizeValueTests" --logger:"console;verbosity=detailed"` passes 10/10 transport argument normalizer regression tests.
- `dotnet test ./src/Playwright.Tests/Playwright.Tests.csproj -c Debug -f net10.0 --no-restore --filter "FullyQualifiedName~EnumerableExtensionsTests" --logger:"console;verbosity=detailed"` passes the object-valued query parameter formatting regression test.
- `dotnet test ./src/Playwright.Tests/Playwright.Tests.csproj -c Debug -f net10.0 --no-restore --filter "FullyQualifiedName~JsonExtensionsTests" --logger:"console;verbosity=detailed"` passes 2/2 JSON helper regression tests.
- `dotnet test ./src/Playwright.Tests/Playwright.Tests.csproj -c Debug -f net10.0 --no-restore --filter "FullyQualifiedName~UserJsonSerializerTests" --logger:"console;verbosity=detailed"` passes 7/7 user JSON serializer regression tests.
- `dotnet test ./src/Playwright.Tests/Playwright.Tests.csproj -c Debug -f net10.0 --no-restore --filter "FullyQualifiedName~ClearcoteProcessTests" --logger:"console;verbosity=detailed"` passes 2/2 Clearcote process helper stdout/stderr drain and ServeAsync process-start regression tests.
- `dotnet test ./src/Playwright.Tests/Playwright.Tests.csproj -c Debug -f net10.0 --no-restore --filter "FullyQualifiedName~ClearcoteProcessTests|FullyQualifiedName~SecurityHelpersTests" --logger:"console;verbosity=detailed"` passes 12/12 process-drain and security helper regression tests.
- `dotnet test ./src/Playwright.Tests/Playwright.Tests.csproj -c Debug -f net10.0 --no-restore --filter "FullyQualifiedName~ClearcoteExecutablePathTests" --logger:"console;verbosity=detailed"` passes 4/4 Clearcote executable path override regression tests.
- `dotnet test ./src/Playwright.Tests/Playwright.Tests.csproj -c Debug -f net10.0 --no-restore --filter "FullyQualifiedName~PageNetworkResponseTests.ShouldWorkWithGenerics|FullyQualifiedName~BrowserContextFetchTests.ShouldParseResponseJSONWhilePassingAType|FullyQualifiedName~BrowserContextFetchTests.ShouldRequireSourceGeneratedMetadataForTypedResponseJSONOptions" --logger:"console;verbosity=detailed"` passes 4/4 typed response JSON metadata regression tests.
- `dotnet test ./src/Playwright.Tests/Playwright.Tests.csproj -c Debug -f net10.0 --no-restore --filter "FullyQualifiedName~DriverDownloaderTests" --logger:"console;verbosity=detailed"` passes 2/2 driver downloader archive staging regression tests.
- `dotnet test ./src/Playwright.Tests/Playwright.Tests.csproj -c Debug -f net10.0 --no-build --no-restore --filter "FullyQualifiedName~DriverPathTests" --logger:"console;verbosity=detailed"` passes 2/2 runtime driver path override regression tests.
- `dotnet test ./src/Playwright.Tests/Playwright.Tests.csproj -c Debug -f net10.0 --no-build --no-restore --filter "FullyQualifiedName~CLITests.DotnetHostPathShould|FullyQualifiedName~CLITests.RunWithResultShouldPreserveSingleArgumentWithSpacesAndQuotes" --logger:"console;verbosity=detailed"` passes 3/3 CLI argument and `DOTNET_HOST_PATH` regression tests.
- `dotnet test ./src/Playwright.Tests/Playwright.Tests.csproj -c Debug -f net10.0 --no-restore --filter "FullyQualifiedName~StdIOTransportTests" --logger:"console;verbosity=detailed"` passes 3/3 transport process shutdown timeout, reader task unwrap, and frame-size validation regression tests.
- `dotnet test ./src/Playwright.Tests/Playwright.Tests.csproj -c Debug -f net10.0 --no-restore --filter "FullyQualifiedName~PlaywrightSettingsXmlTests" --logger:"console;verbosity=detailed"` passes 2/2 runsettings parser regression tests.
- `dotnet test ./src/Playwright.Tests/Playwright.Tests.csproj -c Debug -f net10.0 --no-build --no-restore --filter "FullyQualifiedName~PlaywrightJsonContextTests" --logger:"console;verbosity=detailed"` passes the source-generation metadata registration regression test.
- `dotnet build src/Playwright.TestAdapter/Playwright.TestAdapter.csproj -p:PublishAot=true -p:TrimMode=full -p:UseSharedCompilation=false` passes with 0 warnings.
- `dotnet build` with the same AOT analyzer properties passes with 0 warnings for `src/Playwright.NUnit/Playwright.NUnit.csproj`, `src/Playwright.MSTest/Playwright.MSTest.csproj`, `src/Playwright.Xunit/Playwright.Xunit.csproj`, and `src/Playwright.Xunit.v3/Playwright.Xunit.v3.csproj`.
- `dotnet publish src/Playwright.CLI/Playwright.CLI.csproj -c Release -r linux-x64 -p:PublishAot=true -p:SelfContained=true -p:TrimMode=full -p:UseSharedCompilation=false` passes; the native CLI resolves `-p src/Playwright/Playwright.csproj --version` and preserves quoted unknown commands.
- `dotnet publish samples/Playwright.AotSample.Clearcote/Playwright.AotSample.Clearcote.csproj -c Release -r linux-x64 -p:PublishAot=true -p:SelfContained=true -p:TrimMode=full -p:UseSharedCompilation=false` passes and the native executable resolves the pinned Clearcote browser from the verified cache, launches it, evaluates JavaScript, probes WebGL render info, and captures a screenshot offline (6 test groups).
- `dotnet test ./src/Playwright.Tests/Playwright.Tests.csproj -c Debug -f net10.0 --no-restore --filter "FullyQualifiedName~BrowserContextBasicTests.ShouldWorkWithOfflineOption|FullyQualifiedName~DefaultBrowserContext1Tests.ShouldSupportOfflineOption|FullyQualifiedName~PopupTests.ShouldInheritOfflineFromBrowserContext" --logger:"console;verbosity=detailed"` passes 3/3 upstream offline-emulation tests.
- `dotnet pack src/Playwright/Playwright.csproj -c Debug --no-build --no-restore -p:UseSharedCompilation=false -p:BuildInParallel=false -v:minimal` creates `Playwright.Clearcote.1.0.0.nupkg`.
- A throwaway `PackageReference` consumer restored and built against `Playwright.Clearcote.1.0.0` with the transitive target copying `.playwright/package`, `node/linux-x64/node`, and `playwright.ps1`.
- `dotnet build src/tools/Playwright.Tooling/Playwright.Tooling.csproj -p:PublishAot=true -p:TrimMode=full -p:UseSharedCompilation=false` passes with 0 warnings.
