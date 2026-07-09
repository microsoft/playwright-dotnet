# NativeAOT Fork Changes

## Clearcote launch patch

- Normal Chromium launches keep using the regular Playwright Chromium binary. Clearcote is opt-in via `ClearcoteLaunchOptions`, `ClearcoteLaunchPersistentContextOptions`, the `ClearcoteBrowser.*Launch*` helpers, or `CLEARCOTE=1`.
- `ClearcoteLaunchOptions` and `ClearcoteLaunchPersistentContextOptions` expose the Clearcote fingerprint, language, WebRTC, proxy, canvas bridge, cache, and privacy-sandbox settings without editing generated API files.
- The Clearcote binary resolver mirrors the Node SDK's pinned mode:
  - `CLEARCOTE_BINARY` overrides the browser path.
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
  - supports `Geoip` for HTTP/HTTPS proxy geo lookup using the Node SDK's online fallback path,
  - downloads and caches the `geoip-all-in-one` MaxMind MMDB at runtime, decodes it with an internal AOT-safe reader, and uses it before falling back to `ip-api.com`,
  - emits launch coherence warnings for common proxy, platform, GPU, brand/version, and automation-arg mismatches,
  - collapses duplicate `--enable-features` and `--disable-features` flags.
- Clearcote high-level SDK helpers are ported to .NET:
  - `ClearcoteProfile` persists and loads saved personas from `CLEARCOTE_PROFILE_DIR` / `~/.clearcote/profiles`.
  - `Humanize` and `ShowCursor` options attach to new pages/contexts and route .NET `Mouse`, `Keyboard.TypeAsync`, page/frame selector actions, and locator-backed selector actions through native humanized timing where possible.
  - `ClearcoteBrowser.LaunchAgentAsync()` launches a persistent context for Chrome Actor, and `RunAgentTaskAsync()` calls `Browser.agentRunTask` over CDP.
  - `ClearcoteBrowser.CheckRenderCoherenceAsync()` probes WebGL vendor/renderer and returns the render coherence verdict.
  - `ClearcoteBrowser.FetchWidevineAsync()` and `SeedWidevineAsync()` fetch, verify, extract, and seed the opt-in Widevine CDM; persistent launch supports `Widevine = true`.

## NuGet packaging

- Package id is `Playwright.Clearcote`; assembly/title remain `Microsoft.Playwright` / `Playwright.AOTFork` for compatibility with the forked binary.
- The fork now targets only `net10.0`; the previous `net8.0`/`netstandard2.0` compatibility targets were removed from the fork packages.
- The package includes the bundled Playwright Node driver under `.playwright`.
- `build/Playwright.Clearcote.targets` and `buildTransitive/Playwright.Clearcote.targets` copy the platform Node binary, driver package, and `playwright.ps1` into consumer build and publish outputs.
- A consumer `PackageReference` imports the transitive target automatically; no separate post-install script is required.

## AOT hardening

- Removed the API compatibility suppression file and disabled old package-baseline comparison instead of carrying obsolete suppressions.
- Removed `IL2026`/`IL3050` pragmas from `JsonExtensions`.
- Removed reflection-based fallback deserialization in protocol JSON helpers. Missing source-generated metadata now throws instead of silently falling back.
- Added `JsonTypeInfo<T>` overloads for `IResponse.JsonAsync<T>` and `IAPIResponse.JsonAsync<T>`.
- Existing generic JSON response methods no longer call reflection-based `JsonSerializer.Deserialize<T>`; they require source-generated metadata or fail with an explicit message.
- `ChannelHelpers.ToObject(Exception)` no longer returns an anonymous type boxed as `object`. It returns `JsonObject` (registered in `PlaywrightJsonContext`), making binding error serialization AOT-safe.
- `BindingCall.CallAsync` now deserializes binding arguments from protocol `JsonElement` to the target parameter type via `EvaluateArgumentValueConverter.Deserialize`, fixing a previous `JsonElement` → `int` conversion failure.
- `BindingCall.CallAsync` removed the static `TaskResultProperty` (`typeof(Task<>).GetProperty("Result")`) field that failed at runtime for `Task<T>`. Async binding delegates returning `Task<T>` have a known AOT limitation: the return value is not extracted. Use sync delegates or non-generic `Task` instead.
- `EvaluateArgumentValueConverter` now clones protocol `ref` targets when materializing evaluate results as JSON, so repeated or cyclic JavaScript references do not attach the same `JsonNode` in multiple places.

## .NET-specific implementation notes

- The offline GPL MaxMind database is downloaded and cached at runtime like the Node SDK; it is not vendored into this NuGet package and does not require the reflection-heavy `MaxMind.Db` reader package.
- JavaScript-style monkey-patching is implemented as .NET core routing: supported .NET page/frame/locator/mouse/keyboard paths are humanized when `Humanize = true`, while unsupported action shapes fall back to Playwright's native implementation.

## Validation status

- `dotnet build src/Playwright/Playwright.csproj -p:TargetFramework=net10.0 -p:PublishAot=true -p:TrimMode=full -p:UseSharedCompilation=false` passes with 0 warnings.
- `dotnet publish samples/Playwright.AotSample/Playwright.AotSample.csproj -c Release -r linux-x64 -p:PublishAot=true -p:SelfContained=true -p:TrimMode=full -p:UseSharedCompilation=false` passes and the native executable exercises launch, locators, evaluate arguments/results, route interception, network/API request round-trips, source-gen JSON deserialization, cookie round-trips, sync/async binding invocation with error serialization, locator/page/frame/element/mouse/keyboard actions, `Console` event listeners, offline local-server fetches, screenshots, and Clearcote humanized input hooks offline (100 test groups).
- `dotnet publish samples/Playwright.AotSample.Clearcote/Playwright.AotSample.Clearcote.csproj -c Release -r linux-x64 -p:PublishAot=true -p:SelfContained=true -p:TrimMode=full -p:UseSharedCompilation=false` passes and the native executable resolves the pinned Clearcote browser from the verified cache, launches it, evaluates JavaScript, probes WebGL render info, and captures a screenshot offline (6 test groups).
- `dotnet test ./src/Playwright.Tests/Playwright.Tests.csproj -c Debug -f net10.0 --no-restore --filter "FullyQualifiedName~BrowserContextBasicTests.ShouldWorkWithOfflineOption|FullyQualifiedName~DefaultBrowserContext1Tests.ShouldSupportOfflineOption|FullyQualifiedName~PopupTests.ShouldInheritOfflineFromBrowserContext" --logger:"console;verbosity=detailed"` passes 3/3 upstream offline-emulation tests.
- `dotnet pack src/Playwright/Playwright.csproj -c Debug --no-build --no-restore -p:UseSharedCompilation=false -p:BuildInParallel=false -v:minimal` creates `Playwright.Clearcote.1.0.0.nupkg`.
- A throwaway `PackageReference` consumer restored and built against `Playwright.Clearcote.1.0.0` with the transitive target copying `.playwright/package`, `node/linux-x64/node`, and `playwright.ps1`.
