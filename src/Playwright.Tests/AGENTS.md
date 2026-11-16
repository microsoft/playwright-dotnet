# How to port the upstream tests to .NET

These rules say how to write `src/Playwright.Tests/{Page,Library}` from `tests/page` and
`tests/library` in `microsoft/playwright`, at the commit in `DriverVersion`
(`src/Common/Version.props`). Use them at each roll, for every new or changed upstream test.
The upstream tests are in `.upstream/tests/`, the checkout at that commit. Nothing of it is
copied here: the registry (`.upstream/tests/registry.md`), the assets and the screenshots are
read from `.upstream/`.

The port is line by line, with **zero deviations**. A reviewer reads the `.spec.ts` file and
the `.cs` file side by side. Each upstream statement maps to one C# statement, or to one of the
fixed patterns in this document. The test does what the upstream test does, in the same order,
with the same values, and checks the same things. Port from the upstream source in front of
you, never from memory of what the test does.

**Never change a port to make it pass.** No skip, condition, wait or assertion that upstream
does not have, for any reason: not a browser channel, not a CI image, not an operating system,
not flakiness. A port that fails shows a bug in the .NET client, the driver, the test harness
or the environment: first read the upstream test again and check that the port is faithful,
then report it with the evidence.

The one allowed mark is `[Fixme("dotnet: <reason>")]` for a bug or a gap of the .NET client
that is not fixed yet, or `[Fixme("harness: <reason>")]` for a missing piece of the test
harness. The test still runs, its failure is reported as skipped with the reason, and it fails
when it starts to pass, so that the mark gets removed.

- `Browsers = "webkit"` limits the mark to some browsers, `Platforms = "darwin"` to some
  platforms (`win32`, `darwin`, `linux`), `Case = "..."` to the copies of a looped test or of a
  looped describe whose full name contains the text (`ShouldWork("a")`, `Describe("launch")`).
- `Strict = false` is for a test that fails nearly always and can pass by chance (a time that
  the client rounds): it does not fail when it passes.
- The reason says what .NET does and what upstream expects. It holds for every browser of the
  mark: a text that only one browser gives goes in a mark with `Browsers`. A line break in a
  reason is written `\\n`.
- A test that fails only sometimes for another reason is reported as flaky, not marked.

To tell a bug of the client from a bug of the driver or of the machine, run the same calls
with the Node.js client of the same driver (`require` the package in
`src/Playwright/bin/Debug/netstandard2.0/.playwright/package`): when Node.js fails too, the
port is not at fault and gets no mark. `DEBUG=pw:channel:send,pw:channel:recv` prints the
messages of the .NET client, which shows their order.

The old tests in `src/Playwright.Tests/*.cs`, `Locator/` and `Assertions/` are not part of this
scheme. They are a good place to look up how a call is written with the .NET API. Do not copy
their structure, their skips or their helpers (`TestConstants`, `TestUtils`, `PlaywrightAssert`,
`PageTestEx`, `SimpleServer`), and do not change them.

The harness (fixtures, test server, helpers) is `src/Playwright.Tests/Fixtures/`, the port of
`tests/config/`. A port of a test file does not change it. Section 7 says what to do when a
test needs something that the harness does not have.

## 1. Files and names

- The upstream file decides the C# file. `tests/page/page-check.spec.ts` is
  `src/Playwright.Tests/Page/PageCheckTests.cs`, class `PageCheckTests` in the namespace
  `Microsoft.Playwright.Tests.Page`. `tests/library/chromium/oopif.spec.ts` is
  `Library/Chromium/OopifTests.cs`, class `OopifTests` in
  `Microsoft.Playwright.Tests.Library.Chromium`. The class name is the file name in PascalCase
  with `-` and `.` as the word separators, then `Tests`: `elementhandle-click` is
  `ElementhandleClickTests`, `page-clock.frozen` is `PageClockFrozenTests`, `har` is `HarTests`.
  The file starts with the license header of the upstream file, then:

  ```csharp
  using Microsoft.Playwright.Tests.Fixtures;
  using static Microsoft.Playwright.Assertions;
  using static Microsoft.Playwright.Tests.Fixtures.Utils;

  namespace Microsoft.Playwright.Tests.Page;

  /// <summary>Port of tests/page/page-check.spec.ts.</summary>
  public class PageCheckTests
  {
  ```

  The tests keep the upstream order, and they run in that order. `PageCheckTests.cs` is the
  model. A file without a test that can be ported has no C# file.
- Every upstream test has an identifier, the first token of its title
  (`it('CH-001 should check the box @smoke', ...)`). `.upstream/tests/registry.md` lists them
  all. The port is a `public async Task` method with `[UpstreamTest("...")]` holding the
  upstream title verbatim, identifier and `@smoke` tag included, and a name that is the title in
  PascalCase without the identifier and the tags:

  ```csharp
  [UpstreamTest("CH-001 should check the box @smoke")]
  public async Task ShouldCheckTheBox(IPage page)
  {
  ```

  The words of the name are the runs of letters and digits of the title, each with an
  upper-case first letter and the rest as written: `should work with $eval` is
  `ShouldWorkWithEval`, `toHaveCount pass` is `ToHaveCountPass`, `should throw when not a
  checkbox 2` is `ShouldThrowWhenNotACheckbox2`. A `${...}` of a looped title adds no word.
  When two tests of one class get the same name (`should work with :visible` and `should work
  with >> visible=`), each takes its identifier as a suffix: `ShouldWorkWithVisible_SE135`.
  The title in the attribute is the title of the `it()` call itself, with its characters as in
  the source: the registry shows the same title after the describe titles and a `›`, with
  markdown escapes, which are not part of it.
- `test.describe('toHaveCount', () => { ... })` is a nested `public class ToHaveCount { ... }`,
  named after the describe title in PascalCase.
  - When that name is the name of a test in it, the name of an earlier describe, or does not
    compile, it is `ToHaveCountDescribe`.
  - A describe whose whole title is a loop variable is `Describe`.
  - A describe without a title (`test.describe(() => { ... })`) is a nested class `Describe`
    (`Describe2` for a second one) when it has a `test.use()`, a hook or a modifier of its own;
    else its tests are tests of the class around it.
  - A constant or a helper function of a describe is a member of its class. Tests and nested
    classes keep the upstream order, mixed as upstream mixes them.
- A test that a loop declares several times (`for (const kind of kinds) it(...)`) is one
  method with a `[Case(...)]` per copy, or `[CaseSource(nameof(Kinds))]` for a static field,
  property or method of the class that holds the loop values. The values go to the first
  parameters of the method, the fixtures come after them; a loop over objects has one
  parameter per property. The title keeps `${kind}` as written in the source:

  ```csharp
  [UpstreamTest("EV-090 should work with ${kind}")]
  [Case("a")]
  [Case("b")]
  public async Task ShouldWorkWith(string kind, IPage page)
  ```

  A describe that a loop declares several times is a nested class with a constructor and one
  NUnit `[TestFixture(value)]` per copy; its tests read the field.
- `beforeEach`/`afterEach` are `[BeforeEach]`/`[AfterEach]` methods, which take fixture
  parameters like a test, named `BeforeEach` and `AfterEach` (`BeforeEach2` for a second one).
  `beforeAll`/`afterAll` are static `[BeforeAll]`/`[AfterAll]` methods with worker fixtures.
  A variable of the file or of a describe (`let page; beforeEach(...)`) is a private field of
  the class with the upstream name, at the upstream place. A hook or a fixture that the tests
  of a nested class use is static, and so are the fields it sets.
- The fixtures that a file defines (`test.extend({ name: async ({ dep }, use) => { ... } })`)
  are `[Fixture]` methods at the top of the class. The name of the fixture is the name of the
  method with a lower-case first letter. The parameters are fixtures. The method returns the
  value, or takes a last parameter `Func<T, Task> use` and awaits `use(value)` where upstream
  does; what follows is the teardown. `[Fixture(Scope = FixtureScope.Worker)]` is
  `{ scope: 'worker' }`, `Auto = true` is `{ auto: true }`. A fixture whose value is a function
  has a nested delegate type, named after the fixture with `Fixture` at the end
  (`public delegate Task<IBrowser> ConnectFixture(string wsEndpoint, BrowserTypeConnectOptions options = null);`).
- Helper functions of the file are private static methods of the class, at the upstream
  place; a helper that takes a page or a frame has two overloads. A function or a constant
  that a test declares stays in the test, as a lambda, a local function or a local constant
  with the upstream name. A block `{ ... }` inside a test stays a block. A helper or a variable
  that only tests that are not ported use is left out.
- `test.use({ ... })` at the top of a file or a describe is a `[Fixture]` method that overrides
  the fixture. A parameter with the name of the fixture itself is the value that it overrides.
  Context options (`hasTouch`, `viewport`, `locale`, `ignoreHTTPSErrors`, `httpCredentials`,
  `serviceWorkers`, ...) override `contextOptions`; launch options override `launchOptions`:

  ```csharp
  // it.use({ hasTouch: true });
  [Fixture]
  public static BrowserNewContextOptions ContextOptions(BrowserNewContextOptions contextOptions) => new(contextOptions) { HasTouch = true };

  // it.use({ launchOptions: async ({ launchOptions }, use) => { await use({ ...launchOptions, args: ['--site-per-process'] }); } });
  [Fixture]
  public static BrowserTypeLaunchOptions LaunchOptions(BrowserTypeLaunchOptions launchOptions) => new(launchOptions) { Args = ["--site-per-process"] };
  ```

- `UpstreamRegistryTests` checks that every test of the registry is ported or listed as not
  ported (section 6), under the expected class, title and method name. Its failure message
  names what is missing.

## 2. Fixtures

Fixtures are parameters of the test method (or of a hook or a `[Fixture]` method), resolved by
**name**, as upstream destructures them. A fixture is set up when a parameter asks for it:
a test that takes only `IBrowser browser` has no context and no page. A fixture that upstream
destructures and does not use stays a parameter. An options object that a fixture gives is a
copy: a test may change it.

| Upstream | C# |
|---|---|
| `page`, `context`, `browser`, `browserType`, `playwright` | `IPage page`, `IBrowserContext context`, `IBrowser browser`, `IBrowserType browserType`, `IPlaywright playwright` |
| `request` | `IAPIRequestContext request` |
| `server`, `httpsServer` | `Server server`, `Server httpsServer` |
| `browserName`, `channel`, `platform`, `mode` | `string browserName` (`"chromium"`, `"firefox"`, `"webkit"`), `string channel` (null when not set), `string platform` (`"win32"`, `"darwin"`, `"linux"`), `string mode` (`"default"`) |
| `headless`, `isWindows`, `isMac`, `isLinux`, `isBidi`, `isHeadlessShell`, `isFrozenWebkit`, `allowsThirdParty` | `bool` parameters of the same names |
| `isAndroid`, `isElectron` | `bool` parameters, always false |
| `macVersion`, `browserMajorVersion`, `electronMajorVersion` | `int` parameters |
| `browserVersion`, `defaultSameSiteCookieValue` | `string` parameters (`"Lax"`, `"None"`) |
| `video`, `trace`, `loopback` | `string video`, `string trace` (always `"off"`), `string loopback` (null) |
| `nodeVersion` | `(int Major, int Minor, int Patch) nodeVersion`: no test is skipped for it |
| `launchOptions`, `contextOptions` | `BrowserTypeLaunchOptions launchOptions`, `BrowserNewContextOptions contextOptions` (see below) |
| `contextFactory(options)` | `ContextFactory contextFactory`: `await contextFactory(new() { ... })` |
| `launchPersistent(options)` | `LaunchPersistent launchPersistent`: `var (context, page) = await launchPersistent(new() { ... })` |
| `createUserDataDir()` | `CreateUserDataDir createUserDataDir`: `await createUserDataDir()` |
| `asset('file')` | `Func<string, string> asset`, or `Asset("file")` of `Utils` |
| `testInfo`, `test.info()` | `TestInfo testInfo`, `It.Info()`: `OutputPath(...)`, `SnapshotPath(...)`, `Timeout`, `Retry`, `WorkerIndex`, `TestId`, `Project.OutputDir`, `Attach(...)` (does nothing) |
| `workerInfo` | `WorkerInfo workerInfo`: `WorkerIndex`, `ParallelIndex` |
| `proxyServer`, `socksPort`, `setupSocksForwardingServer` | `TestProxy proxyServer`, `int socksPort`, `TestProxy.SetupSocksForwardingServer(...)` |
| `startRemoteServer(kind, options)` | `StartRemoteServer startRemoteServer`: `await startRemoteServer("launchServer", new() { ... })`; `remoteServer.WsEndpoint()`, `Close()`, `Out(key)`, `Child().Id`, `ChildExitCode()` |
| `childProcess(params)`, `waitForPort(port)` | `ChildProcess childProcess`, `WaitForPort waitForPort` (`TestChildProcess.cs`) |
| `pageWithHar(options)` | `PageWithHar pageWithHar`: `Page`, `Context`, `GetLog()` (a `JsonElement`), `GetZip()` |
| `toImpl` | not available: a test that uses it is not ported; one that only destructures it is ported without the parameter |
| `process.execPath`, `process.kill(pid, signal)` | `NodeProcess.ExecPath` (the Node.js of the driver), `NodeProcess.Kill(pid, signal)` |
| `process.env.X` (read) | `Environment.GetEnvironmentVariable("X")` |
| `process.env.PWTEST_SERVER_REGISTRY = dir` | `string serverRegistry`: the directory that the driver of the worker got at its start |
| `kTargetClosedErrorMessage` | `TargetClosedErrorMessage` (`Utils`) |
| `attachFrame`, `detachFrame`, `verifyViewport`, `expectedSSLError`, `chromiumVersionLessThan`, `rafraf`, `roundBox`, `unshift`, `stripAnsi`, `waitForTestLog`, `parseHar` | the same names in PascalCase in `Utils` |
| `parseTraceRaw(file)` | `ParseTraceRaw(file)` (`Utils`): `trace.Events` (`JsonElement`s), `trace.Resources`, `trace.Actions` (the titles), `trace.ActionObjects`, `trace.Stacks`; `const { events, actions } = await parseTraceRaw(file)` is `var (events, _, actions, _, _) = await ParseTraceRaw(file)` |
| `artifactsFolderName(workerIndex)` | `ArtifactsFolderName(workerIndex)` (`Utils`) |
| `expect(received).toContainYaml(expected)` | `ExpectToContainYaml(received, expected)` (`Utils`) |
| `expect(buffer).toMatchSnapshot('x.png')`, `comparePNGs` | `ExpectToMatchSnapshot(buffer, "x.png", new() { ... })`, `ComparePNGs` (`Utils`) |
| `new VideoPlayer(file)` of `tests/library/videoPlayer.ts` | `new VideoPlayer(file)` (`Fixtures/VideoPlayer.cs`): `Duration`, `Frames`, `VideoWidth`, `VideoHeight`, `Fps`, `SeekLastFrame((x: 95, y: 45)).Data`, `FindFrame(pixels => ...)` |
| `formidable` | `IncomingForm` (`Fixtures/Formidable.cs`): `new formidable.IncomingForm({ uploadDir })` is `new IncomingForm(new() { UploadDir = ... })`; `form.parse(req, (err, fields, files) => { ... })` is `var (err, fields, files) = await form.Parse(req);` and the body of the callback; `files.file1.originalFilename` is `files["file1"].OriginalFilename` (`Mimetype`, `Size`, `Filepath`), `fields['name']` is `fields["name"]`; a body that the test holds is `form.Parse(contentType, body)` |

### The defaults of Playwright Test

Playwright Test adds defaults to calls of the test without a word. The .NET client does not,
so a port passes them. The parameters that these rules add to a test come after the fixtures
of upstream.

- `browserType.launch(options)` gets the headless mode, the channel, the executable and the
  traces directory of the run. Build the options on the `launchOptions` fixture:
  `browserType.launch()` is `browserType.LaunchAsync(launchOptions)`,
  `browserType.launch({ proxy: { server } })` is
  `browserType.LaunchAsync(new(launchOptions) { Proxy = new() { Server = server } })`.
- `browserType.launchPersistentContext(dir, options)` gets the same, and the context options.
  Build the options on the `BrowserTypeLaunchPersistentContextOptions launchPersistentContextOptions`
  fixture: `new(launchPersistentContextOptions) { ... }`. `launchPersistent(options)` does this
  itself.
- `browser.newContext(options)` and `browser.newPage(options)` get `locale: 'en-US'` and the
  context options of `test.use()`, in every test, also in a browser that the test launches or
  connects to. Build the options on the `contextOptions` fixture: `browser.newContext()` is
  `browser.NewContextAsync(contextOptions)`, `browser.newContext({ hasTouch: true })` is
  `browser.NewContextAsync(new(contextOptions) { HasTouch = true })`. For `browser.newPage` the
  fixture is `BrowserNewPageOptions newPageOptions`. The `context`, `page` and `contextFactory`
  fixtures do this themselves: `contextFactory({ hasTouch: true })` is
  `contextFactory(new() { HasTouch = true })`.
- A spread of options is `MergeOptions(options, defaults)` (`Utils`), which sets what `options`
  does not set: `browser.newContext({ ...iPhone })` with `playwright.devices['iPhone 6']` is
  `browser.NewContextAsync(MergeOptions(new BrowserNewContextOptions(iPhone), contextOptions))`.
- The contexts of the browser of the `browser` fixture have the default timeout 0, as in
  Playwright Test: a call without a timeout waits until the test times out.

### The test server

`Server` has the members of upstream's `TestServer`: `Prefix`, `CrossProcessPrefix`,
`EmptyPage`, `Host`, `Hostname`, `Port`, `HelloWorld`, `SetRoute`, `SetRedirect`, `SetAuth`,
`SetCSP`, `SetExtraHeaders`, `EnableGzip`, `SetContent`, `ServeFile`, `WaitForRequest`,
`OnceWebSocketConnection`, `WaitForWebSocket`, `WaitForWebSocketConnectionRequest`,
`WaitForUpgrade`, `SendOnWebSocketConnection`.

- A route handler is `(req, res) => { ... }`, or `async (req, res) => { ... }` when it awaits,
  with the Node.js shapes: `req.Url`, `req.Method`, `req.Headers` (lower-case names;
  `req.Headers["cookie"]` is null when the request does not have the header), `req.RawHeaders`,
  `await req.PostBody` (a `byte[]`), `req.Socket.Destroy()`; `res.SetHeader(name, value)`,
  `res.StatusCode = 404`, `res.Write(...)`, `res.End(...)`, `res.FlushHeaders()`, `res.Socket`,
  `res.Connection`. A handler that upstream writes as `() => {}` (never answers) is
  `(req, res) => { }`.
- `res.writeHead(200, { 'Content-Type': 'text/html' })` is
  `res.WriteHead(200, new() { ["Content-Type"] = "text/html" })`; the array form of `writeHead`
  is `res.WriteHeadRaw(200, "a", "1", "a", "2")`; `res.writeHead(...).end(...)` is two
  statements. `await new Promise(f => res.write(chunk, f))` is `await res.WriteAsync(chunk)`,
  and `res.end(data, f)` is `await res.EndAsync(data)`.
- `await server.waitForRequest('/x')` is `await server.WaitForRequest("/x")`; create the task
  before the action that triggers the request, as upstream does.
- A server WebSocket (`ws` of `OnceWebSocketConnection`) has `Send`, `Close(code, reason)`,
  `OnMessage(message => ...)` (`message.Text`, `message.Data`, `message.IsBinary`),
  `OnceMessage(...)`, `OnClose((code, reason) => ...)`, `OnError(error => ...)`,
  `WaitForMessage()`, `WaitForClose()`, `Terminate()`, `ReadyState` (1 is open), and
  `await ws.SendAsync(data)` for `ws.send(data, callback)`.
- A server that a test makes itself (`new TestServer(dir, port, loopback, { key, cert })`,
  `http.createServer`) is `Server.Create(dir, port)` or
  `Server.CreateHttps(dir, port, loopback, Server.Certificate(certFile, keyFile))`, with a
  route, and `await server.StopAsync()` at the end. A server that `Server` can not be (HTTP/2,
  client certificates, a raw `net` server) is a private helper of the test class on Kestrel,
  `SslStream` or `TcpListener`, with the member names of Node.js.

## 3. Skips and marks

- `it.skip(condition, reason)`, `it.fixme(condition, reason)`, `it.fail(condition, reason)` and
  `it.slow()` inside a test or a hook are `It.Skip(condition, reason)`, `It.Fixme(...)`,
  `It.Fail(...)` and `It.Slow()`, at the same place, with the same condition; `test.` is `It.`
  too. The values of the condition are fixture parameters:
  `it.skip(browserName === 'webkit' && isMac)` is `It.Skip(browserName == "webkit" && isMac)`
  in a test that takes `string browserName, bool isMac`. A skip upstream is a skip in the port;
  it is not a deviation. A test after `It.Fail()` runs and must fail.
- `it.skip('title', ...)`, `it.fixme('title', ...)` and `it.fail('title', ...)` are
  `[ItSkip]`, `[ItFixme]` and `[ItFail]` on the method, next to `[UpstreamTest]`. The body is
  ported all the same.
- A modifier at the top of a file or a describe, with a callback
  (`it.skip(({ browserName }) => browserName !== 'chromium', 'reason')`) or with a plain
  condition, is a static `[BeforeEach]` method before the tests, named after the modifier
  (`Skip2` for a second one, `Fixme` for a `fixme`):

  ```csharp
  [BeforeEach]
  public static void Skip(string browserName) => It.Skip(browserName != "chromium", "reason");
  ```

  `test.slow()` at the top of a file or a describe is `[ItSlow]` on the class.
- Upstream runs the files of `library/chromium/` in the Chromium project only, and the files
  of `library/firefox/` in the Firefox project only: its config ignores a file with the name
  of another browser in its path. The class of such a file starts with
  `[BeforeEach] public static void Skip(string browserName) => It.Skip(browserName != "chromium", "chromium only");`
  under a `// .NET:` mark, before the modifiers of the file.
- `test.setTimeout(ms)` is `It.SetTimeout(ms)`. The timeout of a test is 30 seconds and the
  timeout of `Expect` is 10 seconds, as upstream: the .NET default for `Expect` is 5 seconds,
  and the harness sets 10 for a ported test.
- `test.info().annotations` and `{ annotation: ... }` are left out, without a comment.
  `await test.step('name', async () => { ... })` is `await It.Step("name", async () => { ... })`.
- Do not add `[Fixme]`, `[ItSkip]`, an `It.Skip` or an NUnit `[Ignore]` that upstream does not
  have, except the `[Fixme]` of the rule at the top.

## 4. Statements

### Assertions

| Upstream | C# |
|---|---|
| `expect(a).toBe(b)` | `Assert.That(a, Is.EqualTo(b))`: the received value comes first; `Is.SameAs(b)` for two objects of the API |
| `expect(a).toEqual(b)` on strings, numbers, flat arrays of them, arrays of objects of the API | `Assert.That(a, Is.EqualTo(b))`: `Assert.That(browser.Contexts, Is.EqualTo(new[] { context }))` |
| `expect(a).toEqual(b)` on an object, a nested array, a value from the page | `ExpectToEqual(a, b)` (`Utils`), see below |
| `toBeTruthy()`, `toBeFalsy()` | `Is.True`/`Is.False` for a boolean, `Is.Not.Null`/`Is.Null` for an object, `Is.Not.Empty`/`Is.Null.Or.Empty` for a string |
| `toBeNull()`, `toBeUndefined()`, `toBe(undefined)`, `toBeDefined()` | `Is.Null`, `Is.Not.Null` |
| `toContain(x)`, `toMatch(/re/)`, `toMatch(/re/i)` | `Does.Contain(x)`, `Does.Match("re")`, `Does.Match("re").IgnoreCase` |
| `toHaveLength(n)` | `Has.Length.EqualTo(n)` for an array or a string, `Has.Count.EqualTo(n)` for a list |
| `toBeGreaterThan(n)` and the like | `Is.GreaterThan(n)` and the like |
| `toBeCloseTo(n, digits)` | `Is.EqualTo(n).Within(0.5 * Math.Pow(10, -digits))` |
| `toBeInstanceOf(X)` | `Is.InstanceOf<X>()` |
| `.not.` | `Is.Not...`, `Does.Not...` |
| `expect(value, 'message')` | the message is the last argument of `Assert.That` or of `ExpectToEqual` |
| `expect(a).toMatchObject(b)`, `toContainEqual(b)` | `ExpectToEqual(a, ObjectContaining(b))`, `ExpectToEqual(a, ArrayContaining(new[] { b }))` |
| `expect(() => x).toThrow('text')`, not async | `Assert.Throws<PlaywrightException>(() => ...)` |
| `await expect(locator).toHaveText(...)` and the other matchers on a locator, a page, a response | the same matcher of the .NET API: `await Expect(locator).ToHaveTextAsync(...)`; `.not.` is `.Not.`; `Expect(locator, "message")` |
| `await expect(async () => { ... }).toPass()` | `await Expect(async () => { ... }).ToPassAsync()` |
| `await expect.poll(() => x).toBe(y)` | `await Expect(async () => Assert.That(await x, Is.EqualTo(y))).ToPassAsync()`, without `async` and `await` when `x` is a plain value |
| `expect.soft(...)` | the plain assertion |

`ExpectToEqual(actual, expected)` compares the two sides as JavaScript values, whatever they
are in .NET: `expect(await page.evaluate(() => ({ a: 1, b: [1, 2] }))).toEqual({ a: 1, b: [1, 2] })`
is `ExpectToEqual(await page.EvaluateAsync<object>("() => ({ a: 1, b: [1, 2] })"), new { a = 1, b = new[] { 1, 2 } })`.

- A key that is not a C# identifier, or a value that is `null`, needs a
  `Dictionary<string, object>`: a null property of an object is absent, as `undefined`.
- An object of the client (a page, a request) is equal to itself only. A cookie, a viewport
  size and the other data objects of the API are compared by their properties.
- `expect(new Set(a)).toEqual(new Set(b))` is
  `ExpectToEqual(new HashSet<T>(a), new HashSet<object> { ... })`: the order does not count.
- `expect.any(Number)`, `expect.objectContaining({ ... })` and the like inside the expected
  value are `AnyNumber`, `AnyString`, `ObjectContaining(new { ... })`, `ArrayContaining(...)`,
  `StringContaining(...)`, `StringMatching(...)`.
- `Object.is(a, b)` is `ObjectIs(a, b)`: -0 is not 0, NaN is NaN.
- A `float` of the API is compared with a `double`, not with a `long`: NUnit would round both
  to `float`.

### Errors

- `const error = await promise.catch(e => e); expect(error.message).toContain('text')` is
  `var error = await Catch(() => ...);` then `Assert.That(error.Message, Does.Contain("text"))`.
  `.then(() => null, e => e)` is `Catch` too. `let error = null; await promise.catch(e => error = e)`
  is `Exception error = null;` and `error = await Catch(() => ...);`.
  `.catch(e => e.message)` is `(await Catch(() => ...)).Message` when the call must throw. A
  value that is the result or the error (`const x = await call().catch(e => e)`, used both
  ways) is a `try`/`catch` that sets an `object`.
- `await expect(promise).rejects.toThrow('text')` is
  `var error = await ThrowsAsync<PlaywrightException>(() => ...);` and the same check. `Catch`
  and `ThrowsAsync` are in `Utils`; do not use `Assert.ThrowsAsync`, it blocks the thread.
- The class is the one upstream expects: `TimeoutException` for `playwright.errors.TimeoutError`
  and for `expect(error.name).toBe('TimeoutError')`, `PlaywrightException` otherwise; `Error`
  is `Exception`. A target closed error is a `PlaywrightException` whose message contains
  `TargetClosedErrorMessage`. `error.stack` as a text is `error.ToString()`.
- Compare messages as upstream does. When the .NET message differs from the one upstream
  checks, the test gets `[Fixme("dotnet: ...")]`, with the .NET text in the reason. This is so
  for the missing prefix with the name of the call (`page.click: Timeout ...`), for the
  messages of the `Expect` matchers, and for the C# form of a locator in a message or a call
  log (`Locator("option")` for `locator('option')`): the port shows the difference, it does
  not hide it. A message that the driver words for .NET on purpose
  (`'BrowserType.LaunchPersistentContextAsync(...)'` for `'browserType.launchPersistentContext'`)
  is another matter: the test is not ported.
- A check of `error.stack` against `__filename` is a statement left out with a `// .NET:`
  comment: a .NET stack trace has the frames of the library. `__filename` as a value is
  `new System.Diagnostics.StackFrame(true).GetFileName()`, and a `.spec.ts` name that a trace
  or a location holds is the name of the C# file.

### Promises and events

- `const [popup] = await Promise.all([page.waitForEvent('popup'), page.click('a')])` is
  `var popup = await PromiseAll(page.WaitForPopupAsync(), page.ClickAsync("a"));`
  `PromiseAll` (`Utils`) starts nothing itself: the calls start where they are written, in the
  upstream order, and it fails as soon as one fails, as `Promise.all` does.
  - When a later call has a value too (`page.goto`, `page.evaluate` without a type argument),
    it gives a tuple: `var (popup, _) = await PromiseAll(page.WaitForPopupAsync(), page.EvaluateAsync("..."))`,
    `var (request, response) = await PromiseAll(..., ...)`. There are forms for up to four tasks.
  - For an array of the values, `await PromiseAll(new[] { a, b })`;
    `await Promise.all(items.map(async item => { ... }))` is
    `await PromiseAll(items.Select(async item => { ... }))`.
  - `Promise.race` is `Task.WhenAny`.
- The waits for events: `page.waitForEvent('popup')` is `page.WaitForPopupAsync()`, and the
  same for `requestfinished`, `console` (`WaitForConsoleMessageAsync`), `download`,
  `filechooser`, `websocket`, `worker`, and `context.waitForEvent('page')` (`WaitForPageAsync`),
  `context.waitForEvent('console')`. A predicate or a timeout goes in the options:
  `new() { Predicate = ..., Timeout = ... }`. `page.waitForRequest(urlOrPredicate)` and
  `page.waitForResponse(urlOrPredicate)` take the url, the regular expression or the predicate
  as the first argument: `page.WaitForRequestAsync(url, new() { Timeout = 1 })`;
  `waitForEvent('request')` is `page.WaitForRequestAsync(_ => true)`.
- An event without such a method (`close`, `dialog`, `pageerror`, `crash`, `load`,
  `framenavigated`, `requestfailed`, every event of a context but the two above, of a worker
  and of a WebSocket) and `new Promise(f => page.once('dialog', f))` are
  `Once<IDialog>(h => page.Dialog += h, h => page.Dialog -= h)` (`Utils`), which gives the task
  of the next event; a predicate is its third argument. An event of `EventHandler` without an
  argument is `Once(h => x.Changed += h, h => x.Changed -= h)`.
- In JavaScript the code after an `await` runs before the next event of the driver. In .NET it
  does not: the driver thread can raise the next event first.
  - `emitter.waitForEvent('x').then(value => { ... })` is
    `OnceThen<T>(h => emitter.X += h, h => emitter.X -= h, value => { ... })` (`Utils`): the
    body runs when the event is raised, in its upstream order with the other events and replies.
  - A test that awaits one event and only then starts to wait for an event that follows at
    once (`const ws = await page.waitForEvent('websocket'); await ws.waitForEvent('framereceived')`),
    or that starts the action first and the wait for its event second, can miss the event,
    sometimes or always. When you have shown that this is why a faithful port fails, the test
    is not ported, with the reason `dotnet: race: <event> can come before the test subscribes
    to it`. Do not move the subscription to make it pass.
- `page.on('request', request => ...)` is `page.Request += (_, request) => ...`; `page.off` is
  `-=` with the same handler variable; `page.once('x', handler)` is a handler that removes
  itself first. `async route => { await route.fulfill(...) }` is
  `async route => { await route.FulfillAsync(new() { ... }); }`; a handler that awaits nothing
  is not `async`. A route handler takes the route only: the request, the second argument of
  upstream, is `route.Request`.
- A promise that is created before and awaited later is a task variable, created at the same
  place and awaited at the same place: `const promise = page.goto(url); ...; await promise` is
  `var promise = page.GotoAsync(url); ...; await promise;`.
- A call that upstream does not await and whose failure it ignores
  (`page.evaluate(...).catch(() => {})`, `void page.evaluate(...)`) is
  `IgnoreErrors(page.EvaluateAsync(...))`.
- `const promise = call().then(() => { done = true; })` is a local async function that makes
  the call, awaits it and runs the body, called at the same place:
  `async Task Then() { await Call(); done = true; }` and `var promise = Then();`.
- `new Promise(resolve => ...)` that the test settles itself, or a `ManualPromise`, is a
  `TaskCompletionSource<T>`: `let resolve; const promise = new Promise<void>(f => resolve = f)`
  is one `var promise = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);`
  and `resolve()` is `promise.TrySetResult()`, which does nothing the second time, as
  `resolve()`. Always pass `RunContinuationsAsynchronously`, so that the test does not go on in
  the thread of the server or of the driver.
- `new Promise(f => setTimeout(f, 100))` is `await Task.Delay(100)`.
  `setTimeout(() => { ... }, 100)` that upstream does not await is
  `SetTimeout(() => { ... }, 100)` (`Utils`), whose result goes to `ClearTimeout`;
  `setInterval` and `clearInterval` are `SetInterval` and `ClearInterval`. `ClearTimeout` runs
  a callback that is due first, as Node.js runs the due timers before the code that waited for
  the driver.

### Page functions and values

- `page.evaluate(() => ...)`: the function stays JavaScript, in a string, with the same body
  without the TypeScript syntax (`(e as any).foo` is `e.foo`):
  `page.EvaluateAsync<bool>("() => window['checkbox'].checked")`. A function on several lines
  is a verbatim string with the lines of upstream. A function that upstream passes by name
  (`page.evaluate(dispatchTouch)`) is a `const string` with the text of the function, declared
  before its use.
- A function with arguments takes one argument:
  `page.EvaluateAsync<int>("([a, b]) => a + b", new[] { a, b })` or
  `page.EvaluateAsync("x => x", 7)`; an object argument is an anonymous object with the same
  property names, `new { frameId, url }`.
- The type argument is what the test does with the result:

  | The result | Type argument |
  |---|---|
  | not used | none (`<object>` where .NET has no such overload: a worker, `EvaluateAllAsync`) |
  | a whole number, a number that may have a fraction or be large | `<int>`, `<double>` (`<long>` for a time in milliseconds) |
  | a string, a boolean, an array of them | `<string>`, `<bool>`, `<string[]>` |
  | an object, or anything that goes to `ExpectToEqual` | `<object>`: the client gives an `ExpandoObject`, `object[]`, `double`, `DateTime`, `Uri`, ... |
  | an object that the test changes | `<Dictionary<string, object>>` |
  | an object whose properties the test reads one by one | `<JsonElement>`: `result.GetProperty("status").GetInt32()`; an absent property is `!result.TryGetProperty("x", out _)` |

- `handle.asElement()` is `handle.AsElement()`; a handle that the test uses as an element
  without it is `(IElementHandle)handle`.
- A Node.js value that goes to the page is the .NET value:

  | JavaScript | .NET |
  |---|---|
  | `undefined`, `null` | `null` |
  | an object | an anonymous object, or a `Dictionary<string, object>` (the dictionary type that the API takes, for example `Dictionary<string, string>` for headers) |
  | an array, a `Buffer` | an array, `byte[]` |
  | a `RegExp` | a `Regex`; the flags `i`, `m` and `s` are `RegexOptions`; `g`, `y`, `u` and `v` have no .NET form, and a test about them is not ported |
  | a `Date`, `new Date(ms)`, a time in milliseconds for the clock API | a `DateTime`, `DateTime.UnixEpoch.AddMilliseconds(ms)`: `clock.pauseAt(1000)` is `PauseAtAsync(DateTime.UnixEpoch.AddMilliseconds(1000))` |
  | a `URL`, a bigint | a `Uri`, a `BigInteger` |
  | `NaN`, `Infinity`, `-0` | `double.NaN`, `double.PositiveInfinity`, `-0d` |

  A test whose point is a value that .NET does not have (`undefined` next to `null`, a
  function as an argument, a `Map` or a `Set` as its own kind) is not ported.
- The keys of the data that the page or the driver returns stay as they are
  (`request.Headers["content-type"]`); a key that the test expects to be absent is read with
  `GetValueOrDefault("key")`.

### Strings

- A string keeps its exact characters. A template literal on one line is a C# string, with
  `$"..."` when it has `${}`. A template literal on several lines is a verbatim string
  `@"..."` (`$@"..."` with `${}`) with the lines and the indentation of upstream, `"` written
  as `""`, and `{`/`}` as `{{`/`}}` in `$@"..."`. An escape that a verbatim string can not hold
  (`\n`, `­`) is spliced in as a regular string: `@"one  " + "\n" + @" two"`.
- A boolean in a template literal is `true` or `false`: `{(enabled ? "true" : "false")}`.
- `JSON.stringify(x)` is `JsonStringify(x)` (`Utils`). `text.replace('a', 'b')` with a string
  replaces the first occurrence only: `ReplaceFirst(text, "a", "b")` (`Utils`).
- `console.log` and comments stay, as comments. A comment that explains an upstream workaround
  stays with the code it explains.

### The .NET API

- The names: `page.goto` is `page.GotoAsync`, `page.$` is `QuerySelectorAsync`, `page.$$` is
  `QuerySelectorAllAsync`, `page.$eval` is `EvalOnSelectorAsync`, `page.$$eval` is
  `EvalOnSelectorAllAsync`, `page.mouse` is `page.Mouse`, `page.url()` is `page.Url`,
  `context.pages()` is `context.Pages`. The signatures are in
  `src/Playwright/API/Generated/*.cs` and `src/Playwright/API/Supplements/*.cs`; read the file
  rather than guessing a name. A call that is `[Obsolete]` in .NET is ported as it is.
- Options are the options object of the call: `{ waitUntil: 'load', timeout: 5000 }` is
  `new() { WaitUntil = WaitUntilState.Load, Timeout = 5000 }`. `viewport: null` is
  `ViewportSize = ViewportSize.NoViewport`: a C# null is the default viewport. A string that
  the API takes as an enum is the enum value.
- Some members have another shape in .NET:

  | Upstream | .NET |
  |---|---|
  | the error of a `pageerror` event, `webError.error()` | a string, the stack of the error or `name: message`: `error.message` and `error.stack` are that string, a check that fails on it gets `[Fixme("dotnet: ...")]`, and a statement on `error.name` is left out |
  | `page.addInitScript(fn)`, `selectors.register(name, fn)` | the text of a script, as the Node.js client sends it: `"(" + fn + ")()"` for an init script, `"(" + fn + ")(undefined)"` for a selector engine |
  | `frame.payload` of a WebSocket frame | `frame.Text` or `frame.Binary` |
  | `request.failure().errorText` | the string `request.Failure` |
  | `postData: 'text'` of a route call | `PostData = Encoding.UTF8.GetBytes("text")` |
  | a storage state | a JSON string: `storageState: { ... }` is `StorageState = JsonStringify(new { ... })`, `storageState: path` is `StorageStatePath = path`, and a state that the test reads is `JsonSerializer.Deserialize<JsonElement>(state)` |
  | `connectOverCDP({ endpointURL, ...options })` | `ConnectOverCDPAsync(endpointURL, options)` |
  | `locator.visible()` | `locator.Visible` |

- A test that the typed API can not express is not ported: an API or an option that .NET does
  not have, a value outside of an enum (`'bad'`), a call under `as any`, `@ts-expect-error` or
  `@ts-ignore`, an array that mixes strings and regular expressions. When such a call, or a
  member that .NET does not have (`request.serviceWorker()`), is one statement of a test that
  checks other things too, only that statement is left out, with a `// .NET:` comment that
  quotes it and says why. Before you call an API missing, look for it in
  `src/Playwright/API/Generated/` and `src/Playwright/API/Supplements/`.
- A test that changes `process.env` for the driver during the test is not ported: the driver
  of .NET is another process, which is already running.
- A name that C# does not let a nested block declare again (CS0136) takes a number at its end,
  `context2`, with a mark.

### Node.js

| Node.js | .NET |
|---|---|
| `fs.readFileSync(file, 'utf8')`, `fs.existsSync`, `fs.readdirSync(dir)` | `File.ReadAllText(file)`, `File.Exists` (`Directory.Exists` for a directory), `Directory.GetFileSystemEntries(dir)` with `Path.GetFileName` for the names |
| `path.join(a, b)`, `path.join(__dirname, '..', 'assets', 'x')` | `Path.Combine(a, b)`, `Asset("x")` |
| `url.pathToFileURL(file).href` | `new Uri(file).AbsoluteUri` |
| `Buffer.from('text')`, `buffer.toString()`, `buffer.equals(other)` | `Encoding.UTF8.GetBytes("text")`, `Encoding.UTF8.GetString(buffer)`, `a.SequenceEqual(b)` |
| `PNG.sync.read(buffer)`, `utils.decodeWebp(buffer)` | `ImageComparator.Decode("image/png", buffer)` (`Fixtures/ImageComparator.cs`); a test may use `SixLabors.ImageSharp` too |
| `array.sort()` on strings, `array.find(...)` | `.Order(StringComparer.Ordinal)`, `.FirstOrDefault(...)` |
| `Math.round(x)`, `Date.now()`, `String(handle)` | `Math.Round(x, MidpointRounding.AwayFromZero)`, `DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()`, `handle.ToString()` |
| `process.platform`, `os.arch()` | the `platform` fixture, `RuntimeInformation.ProcessArchitecture` |
| `zlib`, a zip archive | `GZipStream`, `BrotliStream`, `ZLibStream`, `ZipFile`, `ZipArchive` |

## 5. Marks

The port has one mark of its own, `// .NET:`, on the line before a statement whose form the
rules above change in a way that is not obvious: a value that C# can not express as upstream
does, a helper that stands for a JavaScript construct, a statement that is left out. Do not
mark a plain rename, a plain `await`, a pattern that this document fixes, or the defaults of
section 2 (`contextOptions`, `launchOptions`). A mark is not a deviation: the test still does
what upstream does.

## 6. Tests that are not ported

`src/Playwright.Tests/upstream_not_ported.txt` lists the upstream tests that the port does not
have, with the reason. One rule per line: a selector, then the reason. A selector is a test
identifier (`CH-017`), a subsystem code (`CG`), or an upstream file glob without `.spec.ts`
(`library/unit/*`). `UpstreamRegistryTests` fails on a rule that matches no test, and on a test
that is ported and also matches a rule.

A test is not ported when it can not exist in .NET: an API, an option or a value that .NET
does not have, Node.js client internals such as `toImpl` and the `__testHook...` options,
tools that run in the driver, `@playwright/test` features, a race that only the .NET
threading makes (section 4). Nothing else is left out. Say in the reason what is missing,
precisely: `The exposeFunctions option of evaluate() is not in the .NET API.`

A reason that starts with `TODO harness:` names a test utility that the harness does not have
yet; such a test is ported once it does. A test that would end the test process (a stack
overflow) is listed this way too, not marked `[Fixme]`: a marked test still runs.

## 7. The harness

`src/Playwright.Tests/Fixtures/` is the port of `tests/config/`:

- `BuiltinFixtures.cs`, `ProxyFixtures.cs`, `ProcessFixtures.cs`: the fixtures of every test
  (`browserTest.ts`, `baseTest.ts`, `platformFixtures.ts`, `serverFixtures.ts`,
  `commonFixtures.ts`, and the fixtures of Playwright Test that they build on).
- `Server.cs`, `ServerObjects.cs`: `testserver/index.ts`. `TestProxy.cs`, `MockSocksServer.cs`:
  `proxy.ts`. `RemoteServer.cs`, `TestChildProcess.cs`, `NodeProcess.cs`: `remoteServer.ts`
  and the child processes.
- `Utils*.cs`: `utils.ts`, the matchers of `expect` that NUnit lacks, and the JavaScript
  constructs that C# lacks. `ImageComparator.cs`, `Formidable.cs`, `VideoPlayer.cs`: the
  libraries and helper files that the tests import.
- `It.cs` and `TestInfo.cs`: `test` and `testInfo`.
- `UpstreamTestAttribute.cs`, `TestRunner.cs`, `TestScope.cs`, `FixtureCatalog.cs`,
  `Worker.cs`: they run a test as Playwright Test does. A worker (driver, browser, servers)
  runs one test at a time, goes on to the next test after a pass, and is closed after a
  failure or a timeout; the contexts that a test leaves open in the browser of the worker are
  closed when the test ends. The driver of a worker runs with `PWTEST_UNDER_TEST`, as upstream.

When a test needs a fixture or a helper of `tests/config/` that the harness does not have, add
it to the harness as a port of the upstream one, in a change of its own, then port the test.
Keep the harness free of the old test helpers and of the internals of `Microsoft.Playwright`.

## 8. Running

```bash
BROWSER=chromium dotnet test ./src/Playwright.Tests/Playwright.Tests.csproj -c Debug -f net8.0 \
  --filter "FullyQualifiedName~Microsoft.Playwright.Tests.Page.PageCheckTests"
```

- `--filter "TestCategory=CH-001"` runs one test by its identifier
  (`"TestCategory=CH-001|TestCategory=CH-002"` for two), `TestCategory=@smoke` the tests with a
  tag, `FullyQualifiedName~Microsoft.Playwright.Tests.Page.` or `.Library.` all the ports of a
  directory.
- The NUnit adapter drops a filter that selects more than 2000 tests: it runs the whole
  project, the old tests too, and reports only the selected ones. `.Page.` selects more than
  that. Add `-- NUnit.AssemblySelectLimit=10000` at the end of the command for such a filter.
- `FullyQualifiedName~UpstreamRegistryTests` runs the registry check.
  `PWTEST_REGISTRY_REPORT=1` prints the coverage per subsystem, and
  `PWTEST_REGISTRY_FILES=page/page-check` limits the check for missing tests to some files.
- `BROWSER` is `chromium`, `firefox` or `webkit`; `HEADED=1` runs headed (the tests of
  `library/headful` run only so); `PWTEST_CHANNEL` sets the channel; `PWTEST_TIMEOUT=0` turns
  the test timeout off.
- `PWTEST_LOG=file` appends a line to the file when a test starts and when it ends. The test
  runner prints the results of a long run late, so this shows which test a run hangs in.
- When `CI` is set, a failed test runs up to 3 more times. The files of a test
  (`testInfo.OutputPath`) are in `bin/Debug/net8.0/test-results/`, which a run empties when it
  starts.
- Run a port in the three browsers before you call it done. Mouse, focus and popup tests can
  fail in WebKit when the screen is locked or the machine is busy: check with the Node.js
  client before you judge.
