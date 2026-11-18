/**
 * Copyright (c) Microsoft Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Microsoft.Playwright.Tests.Fixtures;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Library;

public abstract class PageClockTestsBase : UpstreamBrowserTest
{
    [SetUp]
    public void Skip() => Info.Skip(!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PW_CLOCK")));

    private Task<List<object>> _calls;

    protected Task<List<object>> Calls()
    {
        return _calls ??= Make();

        async Task<List<object>> Make()
        {
            var page = await PageFixture();
            var calls = new List<object>();
            // .NET: a callback of ExposeFunctionAsync has a fixed number of parameters: (...params) is one parameter.
            await page.ExposeFunctionAsync("stub", (object param) =>
            {
                calls.Add(new { @params = new[] { param } });
            });
            return calls;
        }
    }
}

/// <summary>Port of tests/library/page-clock.spec.ts.</summary>
public class PageClockTests : PageClockTestsBase
{
    public class RunFor : PageClockTestsBase
    {
        [SetUp]
        public async Task BeforeEach()
        {
            var page = await PageFixture();
            // .NET: a time in milliseconds is a DateTime: the clock takes a DateTime or a string.
            await page.Clock.InstallAsync(new() { TimeDate = DateTime.UnixEpoch.AddMilliseconds(0) });
            await page.Clock.PauseAtAsync(DateTime.UnixEpoch.AddMilliseconds(1000));
        }

        [UpstreamTest("CK-001 triggers immediately without specified delay")]
        public async Task TriggersImmediatelyWithoutSpecifiedDelay()
        {
            var page = await PageFixture();
            var calls = await Calls();
            await page.EvaluateAsync(@"async () => {
      setTimeout(window.stub);
    }");

            await page.Clock.RunForAsync(0);
            Assert.That(calls, Has.Count.EqualTo(1));
        }

        [UpstreamTest("CK-002 does not trigger without sufficient delay")]
        public async Task DoesNotTriggerWithoutSufficientDelay()
        {
            var page = await PageFixture();
            var calls = await Calls();
            await page.EvaluateAsync(@"async () => {
      setTimeout(window.stub, 100);
    }");
            await page.Clock.RunForAsync(10);
            ExpectToEqual(calls, Array.Empty<object>());
        }

        [UpstreamTest("CK-003 triggers after sufficient delay")]
        public async Task TriggersAfterSufficientDelay()
        {
            var page = await PageFixture();
            var calls = await Calls();
            await page.EvaluateAsync(@"async () => {
      setTimeout(window.stub, 100);
    }");
            await page.Clock.RunForAsync(100);
            Assert.That(calls, Has.Count.EqualTo(1));
        }

        [UpstreamTest("CK-004 triggers simultaneous timers")]
        public async Task TriggersSimultaneousTimers()
        {
            var page = await PageFixture();
            var calls = await Calls();
            await page.EvaluateAsync(@"async () => {
      setTimeout(window.stub, 100);
      setTimeout(window.stub, 100);
    }");
            await page.Clock.RunForAsync(100);
            Assert.That(calls, Has.Count.EqualTo(2));
        }

        [UpstreamTest("CK-005 triggers multiple simultaneous timers")]
        public async Task TriggersMultipleSimultaneousTimers()
        {
            var page = await PageFixture();
            var calls = await Calls();
            await page.EvaluateAsync(@"async () => {
      setTimeout(window.stub, 100);
      setTimeout(window.stub, 100);
      setTimeout(window.stub, 99);
      setTimeout(window.stub, 100);
    }");
            await page.Clock.RunForAsync(100);
            Assert.That(calls.Count, Is.EqualTo(4));
        }

        [UpstreamTest("CK-006 waits after setTimeout was called")]
        public async Task WaitsAfterSetTimeoutWasCalled()
        {
            var page = await PageFixture();
            var calls = await Calls();
            await page.EvaluateAsync(@"async () => {
      setTimeout(window.stub, 150);
    }");
            await page.Clock.RunForAsync(50);
            ExpectToEqual(calls, Array.Empty<object>());
            await page.Clock.RunForAsync(100);
            Assert.That(calls, Has.Count.EqualTo(1));
        }

        [UpstreamTest("CK-007 triggers event when some throw")]
        public async Task TriggersEventWhenSomeThrow()
        {
            var page = await PageFixture();
            var calls = await Calls();
            await page.EvaluateAsync(@"async () => {
      setTimeout(() => { throw new Error(); }, 100);
      setTimeout(window.stub, 120);
    }");
            await ThrowsAsync<PlaywrightException>(() => page.Clock.RunForAsync(120));
            Assert.That(calls, Has.Count.EqualTo(1));
        }

        [UpstreamTest("CK-008 creates updated Date while ticking")]
        public async Task CreatesUpdatedDateWhileTicking()
        {
            var page = await PageFixture();
            var calls = await Calls();
            await page.Clock.SetSystemTimeAsync(DateTime.UnixEpoch.AddMilliseconds(0));
            await page.EvaluateAsync(@"async () => {
      setInterval(() => {
        window.stub(new Date().getTime());
      }, 10);
    }");
            await page.Clock.RunForAsync(100);
            ExpectToEqual(calls, new[]
            {
                new { @params = new[] { 10 } },
                new { @params = new[] { 20 } },
                new { @params = new[] { 30 } },
                new { @params = new[] { 40 } },
                new { @params = new[] { 50 } },
                new { @params = new[] { 60 } },
                new { @params = new[] { 70 } },
                new { @params = new[] { 80 } },
                new { @params = new[] { 90 } },
                new { @params = new[] { 100 } },
            });
        }

        [UpstreamTest("CK-009 passes 8 seconds")]
        public async Task Passes8Seconds()
        {
            var page = await PageFixture();
            var calls = await Calls();
            await page.EvaluateAsync(@"async () => {
      setInterval(window.stub, 4000);
    }");

            await page.Clock.RunForAsync("08");
            Assert.That(calls.Count, Is.EqualTo(2));
        }

        [UpstreamTest("CK-010 passes 1 minute")]
        public async Task Passes1Minute()
        {
            var page = await PageFixture();
            var calls = await Calls();
            await page.EvaluateAsync(@"async () => {
      setInterval(window.stub, 6000);
    }");

            await page.Clock.RunForAsync("01:00");
            Assert.That(calls.Count, Is.EqualTo(10));
        }

        [UpstreamTest("CK-011 passes 2 hours, 34 minutes and 10 seconds")]
        public async Task Passes2Hours34MinutesAnd10Seconds()
        {
            var page = await PageFixture();
            var calls = await Calls();
            await page.EvaluateAsync(@"async () => {
      setInterval(window.stub, 10000);
    }");

            await page.Clock.RunForAsync("02:34:10");
            Assert.That(calls.Count, Is.EqualTo(925));
        }

        [UpstreamTest("CK-012 throws for invalid format")]
        public async Task ThrowsForInvalidFormat()
        {
            var page = await PageFixture();
            var calls = await Calls();
            await page.EvaluateAsync(@"async () => {
      setInterval(window.stub, 10000);
    }");
            await ThrowsAsync<PlaywrightException>(() => page.Clock.RunForAsync("12:02:34:10"));
            // .NET: await expect(page.clock.runFor(NaN)).rejects.toThrow('clock.runFor: Invalid ticks: NaN') is left out: RunForAsync takes a long, which has no NaN.
            // .NET: await expect(page.clock.fastForward(Infinity)).rejects.toThrow('clock.fastForward: Invalid ticks: Infinity') is left out: FastForwardAsync takes a long, which has no Infinity.
            ExpectToEqual(calls, Array.Empty<object>());
            await page.GotoAsync(Server.EmptyPage);
            Assert.That(await page.EvaluateAsync<int>("() => Date.now()"), Is.EqualTo(1000));
        }

        [UpstreamTest("CK-013 returns the current now value")]
        public async Task ReturnsTheCurrentNowValue()
        {
            var page = await PageFixture();
            await page.Clock.SetSystemTimeAsync(DateTime.UnixEpoch.AddMilliseconds(0));
            const int value = 200;
            await page.Clock.RunForAsync(value);
            Assert.That(await page.EvaluateAsync<int>("() => Date.now()"), Is.EqualTo(value));
        }
    }

    public class FastForward : PageClockTestsBase
    {
        [SetUp]
        public async Task BeforeEach()
        {
            var page = await PageFixture();
            await page.Clock.InstallAsync(new() { TimeDate = DateTime.UnixEpoch.AddMilliseconds(0) });
            await page.Clock.PauseAtAsync(DateTime.UnixEpoch.AddMilliseconds(1000));
        }

        [UpstreamTest("CK-014 ignores timers which wouldn't be run")]
        public async Task IgnoresTimersWhichWouldnTBeRun()
        {
            var page = await PageFixture();
            var calls = await Calls();
            await page.EvaluateAsync(@"async () => {
      setTimeout(() => {
        window.stub('should not be logged');
      }, 1000);
    }");
            await page.Clock.FastForwardAsync(500);
            ExpectToEqual(calls, Array.Empty<object>());
        }

        [UpstreamTest("CK-015 pushes back execution time for skipped timers")]
        public async Task PushesBackExecutionTimeForSkippedTimers()
        {
            var page = await PageFixture();
            var calls = await Calls();
            await page.GotoAsync(Server.EmptyPage);
            await page.EvaluateAsync(@"async () => {
      setTimeout(() => {
        window.stub(Date.now());
      }, 1000);
    }");

            const long fiftyDays = 50L * 24 * 60 * 60 * 1000;
            await page.Clock.FastForwardAsync(fiftyDays);
            ExpectToEqual(calls, new[] { new { @params = new[] { 1000 + fiftyDays } } });
            Assert.That(await page.EvaluateAsync<long>("() => Date.now()"), Is.EqualTo(1000 + fiftyDays));
            await page.ReloadAsync();
            Assert.That(await page.EvaluateAsync<long>("() => Date.now()"), Is.EqualTo(1000 + fiftyDays));
        }

        [UpstreamTest("CK-016 supports string time arguments")]
        public async Task SupportsStringTimeArguments()
        {
            var page = await PageFixture();
            var calls = await Calls();
            await page.EvaluateAsync(@"async () => {
      setTimeout(() => {
        window.stub(Date.now());
      }, 100000);  // 100000 = 1:40
    }");
            await page.Clock.FastForwardAsync("01:50");
            ExpectToEqual(calls, new[] { new { @params = new[] { 1000 + 110000 } } });
        }
    }

    public class StubTimers : PageClockTestsBase
    {
        [SetUp]
        public async Task BeforeEach()
        {
            var page = await PageFixture();
            await page.Clock.InstallAsync(new() { TimeDate = DateTime.UnixEpoch.AddMilliseconds(0) });
            await page.Clock.PauseAtAsync(DateTime.UnixEpoch.AddMilliseconds(1000));
        }

        [UpstreamTest("CK-017 sets initial timestamp")]
        public async Task SetsInitialTimestamp()
        {
            var page = await PageFixture();
            var calls = await Calls();
            await page.Clock.SetSystemTimeAsync(DateTime.UnixEpoch.AddMilliseconds(1400));
            Assert.That(await page.EvaluateAsync<int>("() => Date.now()"), Is.EqualTo(1400));
        }

        [UpstreamTest("CK-018 should throw for invalid date")]
        [LocalFixme("dotnet: the message is 'Invalid date: invalid', without the 'clock.setSystemTime: ' prefix of the call")]
        public async Task ShouldThrowForInvalidDate()
        {
            var page = await PageFixture();
            // .NET: await expect(page.clock.setSystemTime(new Date('invalid'))).rejects.toThrow('Invalid date: Invalid Date') is left out: a DateTime is never an invalid date.
            var error = await ThrowsAsync<PlaywrightException>(() => page.Clock.SetSystemTimeAsync("invalid"));
            Assert.That(error.Message, Does.Contain("clock.setSystemTime: Invalid date: invalid"));
        }

        [UpstreamTest("CK-019 replaces global setTimeout")]
        public async Task ReplacesGlobalSetTimeout()
        {
            var page = await PageFixture();
            var calls = await Calls();
            await page.EvaluateAsync(@"async () => {
      setTimeout(window.stub, 1000);
    }");
            await page.Clock.RunForAsync(1000);
            Assert.That(calls.Count, Is.EqualTo(1));
        }

        [UpstreamTest("CK-020 global fake setTimeout should return id")]
        public async Task GlobalFakeSetTimeoutShouldReturnId()
        {
            var page = await PageFixture();
            var calls = await Calls();
            var to = await page.EvaluateAsync<object>("() => setTimeout(window.stub, 1000)");
            // .NET: typeof to is 'number': the client gives a number as a double.
            Assert.That(to, Is.InstanceOf<double>());
        }

        [UpstreamTest("CK-021 replaces global clearTimeout")]
        public async Task ReplacesGlobalClearTimeout()
        {
            var page = await PageFixture();
            var calls = await Calls();
            await page.EvaluateAsync(@"async () => {
      const to = setTimeout(window.stub, 1000);
      clearTimeout(to);
    }");
            await page.Clock.RunForAsync(1000);
            ExpectToEqual(calls, Array.Empty<object>());
        }

        [UpstreamTest("CK-022 replaces global setInterval")]
        public async Task ReplacesGlobalSetInterval()
        {
            var page = await PageFixture();
            var calls = await Calls();
            var interval = await page.EvaluateAsync<long>(@"() => {
      return setInterval(window.stub, 500);
    }");
            await page.Clock.RunForAsync(1000);
            Assert.That(calls.Count, Is.EqualTo(2));

            await page.EvaluateAsync(@"interval => {
      clearInterval(interval);
      setInterval(window.stub);
    }", interval);
            await page.Clock.FastForwardAsync(0);
            Assert.That(calls, Has.Count.EqualTo(3));
            await page.Clock.FastForwardAsync(1000);
            Assert.That(calls, Has.Count.EqualTo(4));
            await page.Clock.RunForAsync(10);
            Assert.That(calls, Has.Count.EqualTo(14));
        }

        [UpstreamTest("CK-023 replaces global clearInterval")]
        public async Task ReplacesGlobalClearInterval()
        {
            var page = await PageFixture();
            var calls = await Calls();
            await page.EvaluateAsync(@"async () => {
      const to = setInterval(window.stub, 500);
      clearInterval(to);
    }");
            await page.Clock.RunForAsync(1000);
            ExpectToEqual(calls, Array.Empty<object>());
        }

        [UpstreamTest("CK-024 replaces global performance.now")]
        public async Task ReplacesGlobalPerformanceNow()
        {
            var page = await PageFixture();
            var promise = page.EvaluateAsync<object>(@"async () => {
      const prev = performance.now();
      await new Promise(f => setTimeout(f, 1000));
      const next = performance.now();
      return { prev, next };
    }");
            await page.Clock.RunForAsync(1000);
            ExpectToEqual(await promise, new { prev = 1000, next = 2000 });
        }

        [UpstreamTest("CK-025 fakes Date constructor")]
        public async Task FakesDateConstructor()
        {
            var page = await PageFixture();
            var now = await page.EvaluateAsync<int>("() => new Date().getTime()");
            Assert.That(now, Is.EqualTo(1000));
        }
    }

    public class StubTimersDescribe : PageClockTestsBase
    {
        [UpstreamTest("CK-026 replaces global performance.timeOrigin")]
        public async Task ReplacesGlobalPerformanceTimeOrigin()
        {
            var page = await PageFixture();
            await page.Clock.InstallAsync(new() { TimeDate = DateTime.UnixEpoch.AddMilliseconds(1000) });
            await page.Clock.PauseAtAsync(DateTime.UnixEpoch.AddMilliseconds(2000));
            var promise = page.EvaluateAsync<object>(@"async () => {
      const prev = performance.now();
      await new Promise(f => setTimeout(f, 1000));
      const next = performance.now();
      return { prev, next };
    }");
            await page.Clock.RunForAsync(1000);
            Assert.That(await page.EvaluateAsync<int>("() => performance.timeOrigin"), Is.EqualTo(1000));
            ExpectToEqual(await promise, new { prev = 1000, next = 2000 });
        }
    }

    public class Popup : PageClockTestsBase
    {
        [UpstreamTest("CK-027 should tick after popup")]
        public async Task ShouldTickAfterPopup()
        {
            var page = await PageFixture();
            await page.Clock.InstallAsync(new() { TimeDate = DateTime.UnixEpoch.AddMilliseconds(0) });
            var now = new DateTime(2015, 9, 25, 0, 0, 0, DateTimeKind.Utc);
            await page.Clock.PauseAtAsync(now);
            var (popup, _) = await PromiseAll(
                page.WaitForPopupAsync(),
                page.EvaluateAsync("() => window.open('about:blank')"));
            var popupTime = await popup.EvaluateAsync<long>("() => Date.now()");
            Assert.That(popupTime, Is.EqualTo(new DateTimeOffset(now).ToUnixTimeMilliseconds()));
            await page.Clock.RunForAsync(1000);
            var popupTimeAfter = await popup.EvaluateAsync<long>("() => Date.now()");
            Assert.That(popupTimeAfter, Is.EqualTo(new DateTimeOffset(now).ToUnixTimeMilliseconds() + 1000));
        }

        [UpstreamTest("CK-028 should tick before popup")]
        public async Task ShouldTickBeforePopup()
        {
            var page = await PageFixture();
            await page.Clock.InstallAsync(new() { TimeDate = DateTime.UnixEpoch.AddMilliseconds(0) });
            var now = new DateTime(2015, 9, 25, 0, 0, 0, DateTimeKind.Utc);
            await page.Clock.PauseAtAsync(now);
            await page.Clock.RunForAsync(1000);

            var (popup, _) = await PromiseAll(
                page.WaitForPopupAsync(),
                page.EvaluateAsync("() => window.open('about:blank')"));
            var popupTime = await popup.EvaluateAsync<long>("() => Date.now()");
            Assert.That(popupTime, Is.EqualTo(new DateTimeOffset(now).ToUnixTimeMilliseconds() + 1000));
        }

        [UpstreamTest("CK-029 should run time before popup")]
        public async Task ShouldRunTimeBeforePopup()
        {
            var page = await PageFixture();
            Server.SetRoute("/popup.html", (req, res) =>
            {
                res.SetHeader("Content-Type", "text/html");
                res.End("<script>window.time = Date.now()</script>");
            });
            await page.GotoAsync(Server.EmptyPage);
            // Wait for 2 second in real life to check that it is past in popup.
            await page.WaitForTimeoutAsync(2000);
            var (popup, _) = await PromiseAll(
                page.WaitForPopupAsync(),
                page.EvaluateAsync("url => window.open(url)", Server.Prefix + "/popup.html"));
            var popupTime = await popup.EvaluateAsync<long>("time");
            Assert.That(popupTime, Is.GreaterThanOrEqualTo(2000));
        }

        [UpstreamTest("CK-030 should not run time before popup on pause")]
        public async Task ShouldNotRunTimeBeforePopupOnPause()
        {
            var page = await PageFixture();
            Server.SetRoute("/popup.html", (req, res) =>
            {
                res.SetHeader("Content-Type", "text/html");
                res.End("<script>window.time = Date.now()</script>");
            });
            await page.Clock.InstallAsync(new() { TimeDate = DateTime.UnixEpoch.AddMilliseconds(0) });
            await page.Clock.PauseAtAsync(DateTime.UnixEpoch.AddMilliseconds(1000));
            await page.GotoAsync(Server.EmptyPage);
            // Wait for 2 second in real life to check that it is past in popup.
            await page.WaitForTimeoutAsync(2000);
            var (popup, _) = await PromiseAll(
                page.WaitForPopupAsync(),
                page.EvaluateAsync("url => window.open(url)", Server.Prefix + "/popup.html"));
            await popup.WaitForLoadStateAsync();
            var popupTime = await popup.EvaluateAsync<long>("time");
            Assert.That(popupTime, Is.EqualTo(1000));
        }
    }

    public class SetFixedTime : PageClockTestsBase
    {
        [UpstreamTest("CK-031 does not fake methods")]
        public async Task DoesNotFakeMethods()
        {
            var page = await PageFixture();
            await page.Clock.SetFixedTimeAsync(DateTime.UnixEpoch.AddMilliseconds(0));

            // Should not stall.
            await page.EvaluateAsync(@"() => {
      return new Promise(f => setTimeout(f, 1));
    }");
        }

        [UpstreamTest("CK-032 allows setting time multiple times")]
        public async Task AllowsSettingTimeMultipleTimes()
        {
            var page = await PageFixture();
            await page.Clock.SetFixedTimeAsync(DateTime.UnixEpoch.AddMilliseconds(100));
            Assert.That(await page.EvaluateAsync<int>("() => Date.now()"), Is.EqualTo(100));
            await page.Clock.SetFixedTimeAsync(DateTime.UnixEpoch.AddMilliseconds(200));
            Assert.That(await page.EvaluateAsync<int>("() => Date.now()"), Is.EqualTo(200));
        }

        [UpstreamTest("CK-033 fixed time is not affected by clock manipulation")]
        public async Task FixedTimeIsNotAffectedByClockManipulation()
        {
            var page = await PageFixture();
            await page.Clock.SetFixedTimeAsync(DateTime.UnixEpoch.AddMilliseconds(100));
            Assert.That(await page.EvaluateAsync<int>("() => Date.now()"), Is.EqualTo(100));
            await page.Clock.FastForwardAsync(20);
            Assert.That(await page.EvaluateAsync<int>("() => Date.now()"), Is.EqualTo(100));
        }

        [UpstreamTest("CK-034 allows installing fake timers after settings time")]
        public async Task AllowsInstallingFakeTimersAfterSettingsTime()
        {
            var page = await PageFixture();
            var calls = await Calls();
            await page.Clock.SetFixedTimeAsync(DateTime.UnixEpoch.AddMilliseconds(100));
            Assert.That(await page.EvaluateAsync<int>("() => Date.now()"), Is.EqualTo(100));
            await page.Clock.SetFixedTimeAsync(DateTime.UnixEpoch.AddMilliseconds(200));
            await page.EvaluateAsync(@"async () => {
      setTimeout(() => window.stub(Date.now()));
    }");
            await page.Clock.RunForAsync(0);
            ExpectToEqual(calls, new[] { new { @params = new[] { 200 } } });
        }
    }

    public class WhileRunning : PageClockTestsBase
    {
        [UpstreamTest("CK-035 should progress time")]
        public async Task ShouldProgressTime()
        {
            var page = await PageFixture();
            await page.Clock.InstallAsync(new() { TimeDate = DateTime.UnixEpoch.AddMilliseconds(0) });
            var startRealTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            await page.GotoAsync("data:text/html,");
            await page.WaitForTimeoutAsync(1000);
            var now = await page.EvaluateAsync<long>("() => Date.now()");
            var realElapsed = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - startRealTime;
            Assert.That(now, Is.GreaterThanOrEqualTo(1000));
            Assert.That(now, Is.LessThanOrEqualTo(realElapsed + 1000));
        }

        [UpstreamTest("CK-036 should runFor")]
        public async Task ShouldRunFor()
        {
            var page = await PageFixture();
            await page.Clock.InstallAsync(new() { TimeDate = DateTime.UnixEpoch.AddMilliseconds(0) });
            var startRealTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            await page.GotoAsync("data:text/html,");
            await page.Clock.RunForAsync(10000);
            var now = await page.EvaluateAsync<long>("() => Date.now()");
            var realElapsed = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - startRealTime;
            Assert.That(now, Is.GreaterThanOrEqualTo(10000));
            Assert.That(now, Is.LessThanOrEqualTo(10000 + realElapsed + 1000));
        }

        [UpstreamTest("CK-037 should fastForward")]
        public async Task ShouldFastForward()
        {
            var page = await PageFixture();
            await page.Clock.InstallAsync(new() { TimeDate = DateTime.UnixEpoch.AddMilliseconds(0) });
            var startRealTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            await page.GotoAsync("data:text/html,");
            await page.Clock.FastForwardAsync(10000);
            var now = await page.EvaluateAsync<long>("() => Date.now()");
            var realElapsed = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - startRealTime;
            Assert.That(now, Is.GreaterThanOrEqualTo(10000));
            Assert.That(now, Is.LessThanOrEqualTo(10000 + realElapsed + 1000));
        }

        [UpstreamTest("CK-038 should pause")]
        public async Task ShouldPause()
        {
            var page = await PageFixture();
            await page.Clock.InstallAsync(new() { TimeDate = DateTime.UnixEpoch.AddMilliseconds(0) });
            await page.GotoAsync("data:text/html,");
            await page.Clock.PauseAtAsync(DateTime.UnixEpoch.AddMilliseconds(60000));
            // Internally wait to make sure the clock is paused and not running.
            await page.WaitForTimeoutAsync(1111);
            var now = await page.EvaluateAsync<int>("() => Date.now()");
            Assert.That(now, Is.EqualTo(60000));
        }

        [UpstreamTest("CK-040 should pause and fastForward")]
        public async Task ShouldPauseAndFastForward()
        {
            var page = await PageFixture();
            await page.Clock.InstallAsync(new() { TimeDate = DateTime.UnixEpoch.AddMilliseconds(0) });
            await page.GotoAsync("data:text/html,");
            await page.Clock.PauseAtAsync(DateTime.UnixEpoch.AddMilliseconds(60000));
            await page.Clock.FastForwardAsync(1000);
            var now = await page.EvaluateAsync<int>("() => Date.now()");
            Assert.That(now, Is.EqualTo(61000));
        }

        [UpstreamTest("CK-041 should set system time on pause")]
        public async Task ShouldSetSystemTimeOnPause()
        {
            var page = await PageFixture();
            await page.Clock.InstallAsync(new() { TimeDate = DateTime.UnixEpoch.AddMilliseconds(0) });
            await page.GotoAsync("data:text/html,");
            await page.Clock.PauseAtAsync(DateTime.UnixEpoch.AddMilliseconds(60000));
            var now = await page.EvaluateAsync<int>("() => Date.now()");
            Assert.That(now, Is.EqualTo(60000));
        }
    }

    public class WhileOnPause : PageClockTestsBase
    {
        [UpstreamTest("CK-042 fastForward should not run nested immediate")]
        public async Task FastForwardShouldNotRunNestedImmediate()
        {
            var page = await PageFixture();
            var calls = await Calls();
            await page.Clock.InstallAsync(new() { TimeDate = DateTime.UnixEpoch.AddMilliseconds(0) });
            await page.GotoAsync("data:text/html,");
            await page.Clock.PauseAtAsync(DateTime.UnixEpoch.AddMilliseconds(1000));
            await page.EvaluateAsync(@"() => {
      setTimeout(() => {
        window.stub('outer');
        setTimeout(() => window.stub('inner'), 0);
      }, 1000);
    }");
            await page.Clock.FastForwardAsync(1000);
            ExpectToEqual(calls, new[] { new { @params = new[] { "outer" } } });
            await page.Clock.FastForwardAsync(1);
            ExpectToEqual(calls, new[] { new { @params = new[] { "outer" } }, new { @params = new[] { "inner" } } });
        }

        [UpstreamTest("CK-043 runFor should not run nested immediate")]
        public async Task RunForShouldNotRunNestedImmediate()
        {
            var page = await PageFixture();
            var calls = await Calls();
            await page.Clock.InstallAsync(new() { TimeDate = DateTime.UnixEpoch.AddMilliseconds(0) });
            await page.GotoAsync("data:text/html,");
            await page.Clock.PauseAtAsync(DateTime.UnixEpoch.AddMilliseconds(1000));
            await page.EvaluateAsync(@"() => {
      setTimeout(() => {
        window.stub('outer');
        setTimeout(() => window.stub('inner'), 0);
      }, 1000);
    }");
            await page.Clock.RunForAsync(1000);
            ExpectToEqual(calls, new[] { new { @params = new[] { "outer" } } });
            await page.Clock.RunForAsync(1);
            ExpectToEqual(calls, new[] { new { @params = new[] { "outer" } }, new { @params = new[] { "inner" } } });
        }

        [UpstreamTest("CK-044 runFor should not run nested immediate from microtask")]
        public async Task RunForShouldNotRunNestedImmediateFromMicrotask()
        {
            var page = await PageFixture();
            var calls = await Calls();
            await page.Clock.InstallAsync(new() { TimeDate = DateTime.UnixEpoch.AddMilliseconds(0) });
            await page.GotoAsync("data:text/html,");
            await page.Clock.PauseAtAsync(DateTime.UnixEpoch.AddMilliseconds(1000));
            await page.EvaluateAsync(@"() => {
      setTimeout(() => {
        window.stub('outer');
        void Promise.resolve().then(() => setTimeout(() => window.stub('inner'), 0));
      }, 1000);
    }");
            await page.Clock.RunForAsync(1000);
            ExpectToEqual(calls, new[] { new { @params = new[] { "outer" } } });
            await page.Clock.RunForAsync(1);
            ExpectToEqual(calls, new[] { new { @params = new[] { "outer" } }, new { @params = new[] { "inner" } } });
        }

        [UpstreamTest("CK-045 should stay paused on the next page when an init script reads the time")]
        public async Task ShouldStayPausedOnTheNextPageWhenAnInitScriptReadsTheTime()
        {
            var page = await PageFixture();
            Server.SetRoute("/timer.html", (req, res) =>
            {
                res.SetHeader("Content-Type", "text/html");
                res.End("<script>setTimeout(() => document.documentElement.dataset.fired = 'true', 0);</script>");
            });
            await page.Clock.InstallAsync(new() { TimeDate = DateTime.UnixEpoch.AddMilliseconds(0) });
            await page.Clock.PauseAtAsync(DateTime.UnixEpoch.AddMilliseconds(1000));
            await page.AddInitScriptAsync("(() => Date.now())()");
            await page.GotoAsync(Server.EmptyPage);
            await page.Clock.RunForAsync(1000);
            await page.GotoAsync(Server.Prefix + "/timer.html");
            Assert.That(await page.EvaluateAsync<int>("() => Date.now()"), Is.EqualTo(2000));
            await page.WaitForTimeoutAsync(500);
            Assert.That(await page.EvaluateAsync<int>("() => Date.now()"), Is.EqualTo(2000));
            Assert.That(await page.EvaluateAsync<string>("() => document.documentElement.dataset.fired"), Is.Null);
            await page.Clock.RunForAsync(0);
            Assert.That(await page.EvaluateAsync<string>("() => document.documentElement.dataset.fired"), Is.EqualTo("true"));
        }

        [UpstreamTest("CK-046 should not advance performance.now() before the first clock call on a new page")]
        public async Task ShouldNotAdvancePerformanceNowBeforeTheFirstClockCallOnANewPage()
        {
            var page = await PageFixture();
            await page.Clock.PauseAtAsync(DateTime.UnixEpoch.AddMilliseconds(1000));
            await page.GotoAsync(Server.EmptyPage);
            await page.WaitForTimeoutAsync(500);
            ExpectToEqual(await page.EvaluateAsync<object>("() => [performance.now(), Date.now()]"), new[] { 0, 1000 });
        }

        [UpstreamTest("CK-220 should keep timers scheduled during load pending when paused")]
        public async Task ShouldKeepTimersScheduledDuringLoadPendingWhenPaused()
        {
            var page = await PageFixture();
            Server.SetRoute("/timer.html", (req, res) =>
            {
                res.SetHeader("Content-Type", "text/html");
                res.End("<script>setTimeout(() => document.documentElement.dataset.fired = 'true', 0);</script>");
            });
            await page.Clock.InstallAsync(new() { TimeDate = DateTime.UnixEpoch.AddMilliseconds(0) });
            await page.Clock.PauseAtAsync(DateTime.UnixEpoch.AddMilliseconds(1000));
            await page.GotoAsync(Server.Prefix + "/timer.html");
            await page.WaitForTimeoutAsync(500);
            Assert.That(await page.EvaluateAsync<string>("() => document.documentElement.dataset.fired"), Is.Null);
            await page.Clock.RunForAsync(0);
            Assert.That(await page.EvaluateAsync<string>("() => document.documentElement.dataset.fired"), Is.EqualTo("true"));
        }
    }

    public class DateNow : PageClockTestsBase
    {
        [UpstreamTest("CK-047 check Date.now is an integer")]
        public async Task CheckDateNowIsAnInteger()
        {
            var page = await PageFixture();
            await page.Clock.InstallAsync();
            await page.GotoAsync("data:text/html,");
            await page.WaitForTimeoutAsync(1000);
            var dateValue = await page.EvaluateAsync<double>("Date.now()");
            Assert.That(double.IsInteger(dateValue), Is.True);
            await page.WaitForTimeoutAsync(1000);
            var dateValue2 = await page.EvaluateAsync<double>("Date.now()");
            Assert.That(double.IsInteger(dateValue2), Is.True);
        }
    }

    public class TemporalNow : PageClockTestsBase
    {
        [SetUp]
        public async Task BeforeEach()
        {
            var page = await PageFixture();
            Info.Skip(!await page.EvaluateAsync<bool>("() => 'Temporal' in globalThis"), "Temporal is not supported");
        }

        [UpstreamTest("CK-049 should follow fixed time")]
        public async Task ShouldFollowFixedTime()
        {
            var page = await PageFixture();
            await page.Clock.SetFixedTimeAsync(new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            await page.GotoAsync("data:text/html,");
            ExpectToEqual(await page.EvaluateAsync<object>(@"() => {
      const Temporal = globalThis.Temporal;
      return {
        instant: Temporal.Now.instant().toString(),
        zonedDateTime: Temporal.Now.zonedDateTimeISO('UTC').toString(),
        plainDateTime: Temporal.Now.plainDateTimeISO('UTC').toString(),
        plainDate: Temporal.Now.plainDateISO('UTC').toString(),
        plainTime: Temporal.Now.plainTimeISO('UTC').toString(),
      };
    }"), new
            {
                instant = "2020-01-01T00:00:00Z",
                zonedDateTime = "2020-01-01T00:00:00+00:00[UTC]",
                plainDateTime = "2020-01-01T00:00:00",
                plainDate = "2020-01-01",
                plainTime = "00:00:00",
            });
        }

        [UpstreamTest("CK-050 should advance with the clock")]
        public async Task ShouldAdvanceWithTheClock()
        {
            var page = await PageFixture();
            await page.Clock.InstallAsync(new() { TimeDate = DateTime.UnixEpoch.AddMilliseconds(0) });
            await page.GotoAsync("data:text/html,");
            await page.Clock.PauseAtAsync(DateTime.UnixEpoch.AddMilliseconds(1000));
            await page.Clock.RunForAsync(2000);
            Assert.That(await page.EvaluateAsync<int>("() => globalThis.Temporal.Now.instant().epochMilliseconds"), Is.EqualTo(3000));
        }

        [UpstreamTest("CK-051 should use the system time zone by default")]
        public async Task ShouldUseTheSystemTimeZoneByDefault()
        {
            var context = await Browser.NewContextAsync(new(ContextOptions()) { TimezoneId = "America/Los_Angeles" });
            var page = await context.NewPageAsync();
            await page.Clock.SetFixedTimeAsync(new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            await page.GotoAsync("data:text/html,");
            ExpectToEqual(await page.EvaluateAsync<object>(@"() => {
      const Temporal = globalThis.Temporal;
      return {
        timeZoneId: Temporal.Now.timeZoneId(),
        zonedDateTime: Temporal.Now.zonedDateTimeISO().toString(),
        plainDateTime: Temporal.Now.plainDateTimeISO().toString(),
      };
    }"), new
            {
                timeZoneId = "America/Los_Angeles",
                zonedDateTime = "2019-12-31T16:00:00-08:00[America/Los_Angeles]",
                plainDateTime = "2019-12-31T16:00:00",
            });
            await context.CloseAsync();
        }

        [UpstreamTest("CK-052 should keep the rest of Temporal intact")]
        public async Task ShouldKeepTheRestOfTemporalIntact()
        {
            var page = await PageFixture();
            await page.Clock.InstallAsync(new() { TimeDate = DateTime.UnixEpoch.AddMilliseconds(0) });
            await page.GotoAsync("data:text/html,");
            ExpectToEqual(await page.EvaluateAsync<object>(@"() => {
      const Temporal = globalThis.Temporal;
      return {
        tag: Object.prototype.toString.call(Temporal.Now),
        enumerable: Object.keys(Temporal).concat(Object.keys(Temporal.Now)),
        isInstant: Temporal.Now.instant() instanceof Temporal.Instant,
        duration: Temporal.Duration.from({ hours: 1 }).toString(),
      };
    }"), new
            {
                tag = "[object Temporal.Now]",
                enumerable = Array.Empty<object>(),
                isInstant = true,
                duration = "PT1H",
            });
        }
    }

    [UpstreamTest("CK-053 AbortSignal.timeout")]
    public async Task AbortSignalTimeout()
    {
        var page = await PageFixture();
        await page.Clock.InstallAsync(new() { TimeDate = DateTime.UnixEpoch.AddMilliseconds(0) });
        var controller = await page.EvaluateHandleAsync(@"() => {
    const signal = AbortSignal.any([
      AbortSignal.timeout(100)
    ]);
    const handle = {
      signal,
      event: false,
      handler: false,
    };
    signal.addEventListener('abort', () => handle.event = true);
    signal.onabort = () => handle.handler = true;
    return handle;
  }");
        ExpectToEqual(await controller.EvaluateAsync<object>(@"handle => ({
    signal: handle.signal.aborted,
    event: handle.event,
    handler: handle.handler,
  })"), new
        {
            signal = false,
            @event = false,
            handler = false,
        });
        await page.Clock.RunForAsync(200);
        ExpectToEqual(await controller.EvaluateAsync<object>(@"handle => ({
    signal: handle.signal.aborted,
    event: handle.event,
    handler: handle.handler,
    reason: {
      name: handle.signal.reason.name,
      message: handle.signal.reason.message,
      code: handle.signal.reason.code,
    },
  })"), new
        {
            signal = true,
            @event = true,
            handler = true,
            reason = new
            {
                name = "TimeoutError",
                message = BrowserName == "chromium" ? "signal timed out" : "The operation timed out.",
                code = 23,
            },
        });
        Assert.That(await page.EvaluateAsync<bool>("() => AbortSignal.abort().aborted"), Is.EqualTo(true));
    }

    [UpstreamTest("CK-054 correctly increments Date.now()/performance.now() during blocking execution")]
    public async Task CorrectlyIncrementsDateNowPerformanceNowDuringBlockingExecution()
    {
        var page = await PageFixture();
        await page.Clock.SetSystemTimeAsync(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        Server.SetRoute("/repro.html", (req, res) =>
        {
            res.WriteHead(200, new() { ["Content-Type"] = "text/html" });
            res.End(@"
      <html>
        <body>
          <script>
          {
            const start = performance.now();
            while (performance.now() - start < 100) { }
          }
          {
            const start = Date.now();
            while (Date.now() - start < 100) { }
          }
          console.log('done');
          </script>
        </body>
      </html>
    ");
        });
        var waitForDone = page.WaitForConsoleMessageAsync(new() { Predicate = msg => msg.Text == "done" });
        await page.GotoAsync(Server.Prefix + "/repro.html");
        await waitForDone;
    }
}
