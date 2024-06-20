/*
 * MIT License
 *
 * Copyright (c) Microsoft Corporation.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;

namespace Microsoft.Playwright.Tests;

public class PageClockTests : PageTestEx
{
    private readonly List<object[]> _calls = [];

    [SetUp]
    public async Task SetUp()
    {
        _calls.Clear();
        await Page.ExposeFunctionAsync("stub", () =>
        {
            _calls.Add([]);
        });
        await Page.ExposeFunctionAsync("stubWithNumberValue", (int value) =>
        {
            _calls.Add([value]);
        });
        await Page.ExposeFunctionAsync("stubWithStringValue", (string value) =>
        {
            _calls.Add([value]);
        });
    }

    public class RunForTests : PageClockTests
    {
        [SetUp]
        public async Task RunForSetUp()
        {
            await Page.Clock.InstallAsync(new() { TimeInt64 = 0 });
            await Page.Clock.PauseAtAsync(1000);
        }

        [PlaywrightTest("page-clock.spec.ts", "triggers immediately without specified delay")]
        public async Task RunForTriggersImmediatelyWithoutSpecifiedDelay()
        {
            await Page.EvaluateAsync("() => { setTimeout(window.stub); }");

            await Page.Clock.RunForAsync(0);
            Assert.AreEqual(1, _calls.Count);
        }

        [PlaywrightTest("page-clock.spec.ts", "does not trigger without sufficient delay")]
        public async Task DoesNotTriggerWithoutSufficientDelay()
        {
            await Page.EvaluateAsync("() => { setTimeout(window.stub, 100); }");
            await Page.Clock.RunForAsync(10);
            Assert.IsEmpty(_calls);
        }

        [PlaywrightTest("page-clock.spec.ts", "triggers after sufficient delay")]
        public async Task TriggersAfterSufficientDelay()
        {
            await Page.EvaluateAsync("() => { setTimeout(window.stub, 100); }");
            await Page.Clock.RunForAsync(100);
            Assert.AreEqual(1, _calls.Count);
        }

        [PlaywrightTest("page-clock.spec.ts", "triggers simultaneous timers")]
        public async Task TriggersSimultaneousTimers()
        {
            await Page.EvaluateAsync("() => { setTimeout(window.stub, 100); setTimeout(window.stub, 100); }");
            await Page.Clock.RunForAsync(100);
            Assert.AreEqual(2, _calls.Count);
        }

        [PlaywrightTest("page-clock.spec.ts", "triggers multiple simultaneous timers")]
        public async Task TriggersMultipleSimultaneousTimers()
        {
            await Page.EvaluateAsync("() => { setTimeout(window.stub, 100); setTimeout(window.stub, 100); setTimeout(window.stub, 99); setTimeout(window.stub, 100); }");
            await Page.Clock.RunForAsync(100);
            Assert.AreEqual(4, _calls.Count);
        }

        [PlaywrightTest("page-clock.spec.ts", "waits after setTimeout was called")]
        public async Task WaitsAfterSetTimeoutWasCalled()
        {
            await Page.EvaluateAsync("() => { setTimeout(window.stub, 150); }");
            await Page.Clock.RunForAsync(50);
            Assert.IsEmpty(_calls);
            await Page.Clock.RunForAsync(100);
            Assert.AreEqual(1, _calls.Count);
        }

        [PlaywrightTest("page-clock.spec.ts", "triggers when some throw")]
        public async Task TriggersWhenSomeThrow()
        {
            await Page.EvaluateAsync("() => { setTimeout(() => { throw new Error(); }, 100); setTimeout(window.stub, 120); }");
            await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => Page.Clock.RunForAsync(120));
            Assert.AreEqual(1, _calls.Count);
        }

        [PlaywrightTest("page-clock.spec.ts", "creates updated Date while ticking")]
        public async Task CreatesUpdatedDateWhileTicking()
        {
            await Page.Clock.SetSystemTimeAsync(0);
            await Page.EvaluateAsync("() => { setInterval(() => { window.stubWithNumberValue(new Date().getTime()); }, 10); }");
            await Page.Clock.RunForAsync(100);
            Assert.AreEqual(new[] { 10, 20, 30, 40, 50, 60, 70, 80, 90, 100 }, _calls.Select(c => c[0]));
        }

        [PlaywrightTest("page-clock.spec.ts", "passes 8 seconds")]
        public async Task Passes8Seconds()
        {
            await Page.EvaluateAsync("() => { setInterval(window.stub, 4000); }");
            await Page.Clock.RunForAsync("08");
            Assert.AreEqual(2, _calls.Count);
        }

        [PlaywrightTest("page-clock.spec.ts", "passes 1 minute")]
        public async Task Passes1Minute()
        {
            await Page.EvaluateAsync("() => { setInterval(window.stub, 6000); }");
            await Page.Clock.RunForAsync("01:00");
            Assert.AreEqual(10, _calls.Count);
        }

        [PlaywrightTest("page-clock.spec.ts", "passes 2 hours, 34 minutes and 10 seconds")]
        public async Task Passes2Hours34MinutesAnd10Seconds()
        {
            await Page.EvaluateAsync("() => { setInterval(window.stub, 10000); }");
            await Page.Clock.RunForAsync("02:34:10");
            Assert.AreEqual(925, _calls.Count);
        }

        [PlaywrightTest("page-clock.spec.ts", "throws for invalid format")]
        public async Task ThrowsForInvalidFormat()
        {
            await Page.EvaluateAsync("() => { setInterval(window.stub, 10000); }");
            await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => Page.Clock.RunForAsync("12:02:34:10"));
            Assert.IsEmpty(_calls);
        }

        [PlaywrightTest("page-clock.spec.ts", "returns the current now value")]
        public async Task ReturnsTheCurrentNowValue()
        {
            await Page.Clock.SetSystemTimeAsync(0);
            var value = 200;
            await Page.Clock.RunForAsync(value);
            Assert.AreEqual(value, await Page.EvaluateAsync<int>("Date.now()"));
        }
    }

    public class FastForwardTests : PageClockTests
    {
        [SetUp]
        public async Task FastForwardSetUp()
        {
            await Page.Clock.InstallAsync(new() { TimeInt64 = 0 });
            await Page.Clock.PauseAtAsync(1000);
        }

        [PlaywrightTest("page-clock.spec.ts", "ignores timers which wouldn't be run")]
        public async Task IgnoresTimersWhichWouldntBeRun()
        {
            await Page.EvaluateAsync("() => { setTimeout(() => { window.stubWithStringValue('should not be logged'); }, 1000); }");
            await Page.Clock.FastForwardAsync(500);
            Assert.IsEmpty(_calls);
        }

        [PlaywrightTest("page-clock.spec.ts", "pushes back execution time for skipped timers")]
        public async Task PushesBackExecutionTimeForSkippedTimers()
        {
            await Page.EvaluateAsync("() => { setTimeout(() => { window.stubWithNumberValue(Date.now()); }, 1000); }");
            await Page.Clock.FastForwardAsync(2000);
            Assert.AreEqual(1, _calls.Count);
            Assert.AreEqual(1000 + 2000, _calls[0][0]);
        }

        [PlaywrightTest("page-clock.spec.ts", "supports string time arguments")]
        public async Task SupportsStringTimeArguments()
        {
            await Page.EvaluateAsync("() => { setTimeout(() => { window.stubWithNumberValue(Date.now()); }, 100000); }");  // 100000 = 1:40
            await Page.Clock.FastForwardAsync("01:50");
            Assert.AreEqual(1, _calls.Count);
            Assert.AreEqual(111000, _calls[0][0]);
        }
    }

    public class StubTimersTests : PageClockTests
    {
        [SetUp]
        public async Task StubTimersSetUp()
        {
            await Page.Clock.InstallAsync(new() { TimeInt64 = 0 });
            await Page.Clock.PauseAtAsync(1000);
        }

        [PlaywrightTest("page-clock.spec.ts", "sets initial timestamp")]
        public async Task SetsInitialTimestamp()
        {
            await Page.Clock.SetSystemTimeAsync(1400);
            Assert.AreEqual(1400, await Page.EvaluateAsync<int>("Date.now()"));
        }

        [PlaywrightTest("page-clock.spec.ts", "should throw for invalid date")]
        public async Task ShouldThrowForInvalidDate()
        {
            await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => Page.Clock.SetSystemTimeAsync("invalid"));
        }

        [PlaywrightTest("page-clock.spec.ts", "replaces global setTimeout")]
        public async Task ReplacesGlobalSetTimeout()
        {
            await Page.EvaluateAsync("() => { setTimeout(window.stub, 1000); }");
            await Page.Clock.RunForAsync(1000);
            Assert.AreEqual(1, _calls.Count);
        }

        [PlaywrightTest("page-clock.spec.ts", "global fake setTimeout should return id")]
        public async Task GlobalFakeSetTimeoutShouldReturnId()
        {
            var id = await Page.EvaluateAsync<long>("() => setTimeout(window.stub, 1000)");
            Assert.IsTrue(id > 0);
        }

        [PlaywrightTest("page-clock.spec.ts", "replaces global clearTimeout")]
        public async Task ReplacesGlobalClearTimeout()
        {
            await Page.EvaluateAsync("() => { const id = setTimeout(window.stub, 1000); clearTimeout(id); }");
            await Page.Clock.RunForAsync(1000);
            Assert.IsEmpty(_calls);
        }

        [PlaywrightTest("page-clock.spec.ts", "replaces global setInterval")]
        public async Task ReplacesGlobalSetInterval()
        {
            await Page.EvaluateAsync("() => { setInterval(window.stub, 500); }");
            await Page.Clock.RunForAsync(1000);
            Assert.AreEqual(2, _calls.Count);
        }

        [PlaywrightTest("page-clock.spec.ts", "replaces global clearInterval")]
        public async Task ReplacesGlobalClearInterval()
        {
            await Page.EvaluateAsync("() => { const id = setInterval(window.stub, 500); clearInterval(id); }");
            await Page.Clock.RunForAsync(1000);
            Assert.IsEmpty(_calls);
        }

        [PlaywrightTest("page-clock.spec.ts", "replaces global performance.now")]
        public async Task ReplacesGlobalPerformanceNow()
        {
            var task = Page.EvaluateAsync<JsonElement>("async () => { const prev = performance.now(); await new Promise(f => setTimeout(f, 1000)); const next = performance.now(); return { prev, next }; }");
            await Page.Clock.RunForAsync(1000);
            var result = await task;
            Assert.AreEqual(1000, result.GetProperty("prev").GetInt32());
            Assert.AreEqual(2000, result.GetProperty("next").GetInt32());
        }

        [PlaywrightTest("page-clock.spec.ts", "fakes Date constructor")]
        public async Task FakesDateConstructor()
        {
            var now = await Page.EvaluateAsync<int>("() => new Date().getTime()");
            Assert.AreEqual(1000, now);
        }
    }

    public class StubTimerTests: PageClockTests
    {
        [PlaywrightTest("page-clock.spec.ts", "replaces global performance.timeOrigin")]
        public async Task ReplacesGlobalPerformanceTimeOrigin()
        {
            await Page.Clock.InstallAsync(new() { TimeInt64 = 1000 });
            await Page.Clock.PauseAtAsync(2000);
            var promise = Page.EvaluateAsync<JsonElement>("async () => { const prev = performance.now(); await new Promise(f => setTimeout(f, 1000)); const next = performance.now(); return { prev, next }; }");
            await Page.Clock.RunForAsync(1000);
            Assert.AreEqual(1000, await Page.EvaluateAsync<int>("performance.timeOrigin"));
            var result = await promise;
            Assert.AreEqual(1000, result.GetProperty("prev").GetInt32());
            Assert.AreEqual(2000, result.GetProperty("next").GetInt32());
        }
    }

    public class PopupTests : PageClockTests
    {
        [PlaywrightTest("page-clock.spec.ts", "should tick after popup")]
        public async Task ShouldTickAfterPopup()
        {
            await Page.Clock.InstallAsync(new() { TimeInt64 = 0 });
            var now = new DateTime(2015, 9, 25);
            await Page.Clock.PauseAtAsync(now);
            var popupTask = Page.WaitForPopupAsync();
            await Page.EvaluateAsync("() => window.open('about:blank')");
            var popup = await popupTask;
            var popupTime = await popup.EvaluateAsync<long>("() => Date.now()");
            Assert.AreEqual(((DateTimeOffset)now).ToUnixTimeMilliseconds(), popupTime);
            await Page.Clock.RunForAsync(1000);
            var popupTimeAfter = await popup.EvaluateAsync<long>("() => Date.now()");
            Assert.AreEqual(((DateTimeOffset)now).ToUnixTimeMilliseconds() + 1000, popupTimeAfter);
        }

        [PlaywrightTest("page-clock.spec.ts", "should tick before popup")]
        public async Task ShouldTickBeforePopup()
        {
            await Page.Clock.InstallAsync(new() { TimeInt64 = 0 });
            var now = new DateTime(2015, 9, 25);
            await Page.Clock.PauseAtAsync(now);
            await Page.Clock.RunForAsync(1000);
            var popupTask = Page.WaitForPopupAsync();
            await Page.EvaluateAsync("() => window.open('about:blank')");
            var popup = await popupTask;
            var popupTime = await popup.EvaluateAsync<long>("() => Date.now()");
            Assert.AreEqual(((DateTimeOffset)now).ToUnixTimeMilliseconds() + 1000, popupTime);
        }

        [PlaywrightTest("page-clock.spec.ts", "should run time before popup")]
        public async Task ShouldRunTimeBeforePopup()
        {
            Server.SetRoute("/popup.html", context => {
                context.Response.Headers["Content-Type"] = "text/html";
                return context.Response.WriteAsync("<script>window.time = Date.now();</script>");
            });
            await Page.GotoAsync(Server.EmptyPage);
            // Wait for 2 seconds in real life to check that it is past in popup.
            await Page.WaitForTimeoutAsync(2000);
            var popupTask = Page.WaitForPopupAsync();
            await Page.EvaluateAsync("url => window.open(url)", Server.Prefix + "/popup.html");
            var popup = await popupTask;
            var popupTime = await popup.EvaluateAsync<long>("window.time");
            Assert.GreaterOrEqual(popupTime, 2000);
        }

        [PlaywrightTest("page-clock.spec.ts", "should not run time before popup on pause")]
        public async Task ShouldNotRunTimeBeforePopupOnPause()
        {
            Server.SetRoute("/popup.html", context => {
                context.Response.Headers["Content-Type"] = "text/html";
                return context.Response.WriteAsync("<script>window.time = Date.now();</script>");
            });
            await Page.Clock.InstallAsync(new() { TimeInt64 = 0 });
            await Page.Clock.PauseAtAsync(1000);
            await Page.GotoAsync(Server.EmptyPage);
            // Wait for 2 seconds in real life to check that it is past in popup.
            await Page.WaitForTimeoutAsync(2000);
            var popupTask = Page.WaitForPopupAsync();
            await Page.EvaluateAsync("url => window.open(url)", Server.Prefix + "/popup.html");
            var popup = await popupTask;
            var popupTime = await popup.EvaluateAsync<long>("window.time");
            Assert.AreEqual(1000, popupTime);
        }
    }

    public class SetFixedTimeTests : PageClockTests
    {
        [PlaywrightTest("page-clock.spec.ts", "does not fake methods")]
        public async Task DoesNotFakeMethods()
        {
            await Page.Clock.SetFixedTimeAsync(0);
            // Should not stall.
            await Page.EvaluateAsync("() => new Promise(f => setTimeout(f, 1))");
        }

        [PlaywrightTest("page-clock.spec.ts", "allows setting time multiple times")]
        public async Task AllowsSettingTimeMultipleTimes()
        {
            await Page.Clock.SetFixedTimeAsync(100);
            Assert.AreEqual(100, await Page.EvaluateAsync<long>("Date.now()"));
            await Page.Clock.SetFixedTimeAsync(200);
            Assert.AreEqual(200, await Page.EvaluateAsync<long>("Date.now()"));
        }

        [PlaywrightTest("page-clock.spec.ts", "fixed time is not affected by clock manipulation")]
        public async Task FixedTimeIsNotAffectedByClockManipulation()
        {
            await Page.Clock.SetFixedTimeAsync(100);
            Assert.AreEqual(100, await Page.EvaluateAsync<long>("Date.now()"));
            await Page.Clock.FastForwardAsync(20);
            Assert.AreEqual(100, await Page.EvaluateAsync<long>("Date.now()"));
        }

        [PlaywrightTest("page-clock.spec.ts", "allows installing fake timers after setting time")]
        public async Task AllowsInstallingFakeTimersAfterSettingTime()
        {
            await Page.Clock.SetFixedTimeAsync(100);
            Assert.AreEqual(100, await Page.EvaluateAsync<long>("Date.now()"));
            await Page.Clock.SetFixedTimeAsync(200);
            await Page.EvaluateAsync("() => { setTimeout(() => window.stubWithNumberValue(Date.now()), 0); }");
            await Page.Clock.RunForAsync(0);
            Assert.AreEqual(1, _calls.Count);
            Assert.AreEqual(200, _calls[0][0]);
        }
    }

    public class WhileRunningTests : PageClockTests
    {
        [PlaywrightTest("page-clock.spec.ts", "should progress time")]
        public async Task ShouldProgressTime()
        {
            await Page.Clock.InstallAsync(new() { TimeInt64 = 0 });
            await Page.GotoAsync("data:text/html,");
            await Page.WaitForTimeoutAsync(1000);
            var now = await Page.EvaluateAsync<long>("Date.now()");
            Assert.GreaterOrEqual(now, 1000);
            Assert.LessOrEqual(now, 2000);
        }

        [PlaywrightTest("page-clock.spec.ts", "should runFor")]
        public async Task ShouldRunFor()
        {
            await Page.Clock.InstallAsync(new() { TimeInt64 = 0 });
            await Page.GotoAsync("data:text/html,");
            await Page.Clock.RunForAsync(10000);
            var now = await Page.EvaluateAsync<long>("Date.now()");
            Assert.GreaterOrEqual(now, 10000);
            Assert.LessOrEqual(now, 11000);
        }

        [PlaywrightTest("page-clock.spec.ts", "should fastForward")]
        public async Task ShouldFastForward()
        {
            await Page.Clock.InstallAsync(new() { TimeInt64 = 0 });
            await Page.GotoAsync("data:text/html,");
            await Page.Clock.FastForwardAsync(10000);
            var now = await Page.EvaluateAsync<long>("Date.now()");
            Assert.GreaterOrEqual(now, 10000);
            Assert.LessOrEqual(now, 11000);
        }

        [PlaywrightTest("page-clock.spec.ts", "should fastForwardTo")]
        public async Task ShouldFastForwardTo()
        {
            await Page.Clock.InstallAsync(new() { TimeInt64 = 0 });
            await Page.GotoAsync("data:text/html,");
            await Page.Clock.FastForwardAsync(10000);
            var now = await Page.EvaluateAsync<long>("Date.now()");
            Assert.GreaterOrEqual(now, 10000);
            Assert.LessOrEqual(now, 11000);
        }

        [PlaywrightTest("page-clock.spec.ts", "should pause")]
        public async Task ShouldPause()
        {
            await Page.Clock.InstallAsync(new() { TimeInt64 = 0 });
            await Page.GotoAsync("data:text/html,");
            await Page.Clock.PauseAtAsync(1000);
            await Page.WaitForTimeoutAsync(1000);
            await Page.Clock.ResumeAsync();
            var now = await Page.EvaluateAsync<long>("Date.now()");
            Assert.GreaterOrEqual(now, 0);
            Assert.LessOrEqual(now, 1000);
        }

        [PlaywrightTest("page-clock.spec.ts", "should pause and fastForward")]
        public async Task ShouldPauseAndFastForward()
        {
            await Page.Clock.InstallAsync(new() { TimeInt64 = 0 });
            await Page.GotoAsync("data:text/html,");
            await Page.Clock.PauseAtAsync(1000);
            await Page.Clock.FastForwardAsync(1000);
            var now = await Page.EvaluateAsync<long>("Date.now()");
            Assert.AreEqual(2000, now);
        }

        [PlaywrightTest("page-clock.spec.ts", "should set system time on pause")]
        public async Task ShouldSetSystemTimeOnPause()
        {
            await Page.Clock.InstallAsync(new() { TimeInt64 = 0 });
            await Page.GotoAsync("data:text/html,");
            await Page.Clock.PauseAtAsync(1000);
            var now = await Page.EvaluateAsync<long>("Date.now()");
            Assert.AreEqual(1000, now);
        }
    }

    public class WhileOnPauseTests : PageClockTests
    {
        [PlaywrightTest("page-clock.spec.ts", "fastForward should not run nested immediate")]
        public async Task FastForwardShouldNotRunNestedImmediate()
        {
            await Page.Clock.InstallAsync(new() { TimeInt64 = 0 });
            await Page.GotoAsync("data:text/html,");
            await Page.Clock.PauseAtAsync(1000);
            await Page.EvaluateAsync("() => { setTimeout(() => { window.stubWithStringValue('outer'); setTimeout(() => window.stubWithStringValue('inner'), 0); }, 1000); }");
            await Page.Clock.FastForwardAsync(1000);
            Assert.AreEqual(1, _calls.Count);
            Assert.AreEqual("outer", _calls[0][0]);
            await Page.Clock.FastForwardAsync(1);
            Assert.AreEqual(2, _calls.Count);
            Assert.AreEqual("inner", _calls[1][0]);
        }

        [PlaywrightTest("page-clock.spec.ts", "runFor should not run nested immediate")]
        public async Task RunForShouldNotRunNestedImmediate()
        {
            await Page.Clock.InstallAsync(new() { TimeInt64 = 0 });
            await Page.GotoAsync("data:text/html,");
            await Page.Clock.PauseAtAsync(1000);
            await Page.EvaluateAsync("() => { setTimeout(() => { window.stubWithStringValue('outer'); setTimeout(() => window.stubWithStringValue('inner'), 0); }, 1000); }");
            await Page.Clock.RunForAsync(1000);
            Assert.AreEqual(1, _calls.Count);
            Assert.AreEqual("outer", _calls[0][0]);
            await Page.Clock.RunForAsync(1);
            Assert.AreEqual(2, _calls.Count);
            Assert.AreEqual("inner", _calls[1][0]);
        }

        [PlaywrightTest("page-clock.spec.ts", "runFor should not run nested immediate from microtask")]
        public async Task RunForShouldNotRunNestedImmediateFromMicrotask()
        {
            await Page.Clock.InstallAsync(new() { TimeInt64 = 0 });
            await Page.GotoAsync("data:text/html,");
            await Page.Clock.PauseAtAsync(1000);
            await Page.EvaluateAsync("() => { setTimeout(() => { window.stubWithStringValue('outer'); Promise.resolve().then(() => setTimeout(() => window.stubWithStringValue('inner'), 0)); }, 1000); }");
            await Page.Clock.RunForAsync(1000);
            Assert.AreEqual(1, _calls.Count);
            Assert.AreEqual("outer", _calls[0][0]);
            await Page.Clock.RunForAsync(1);
            Assert.AreEqual(2, _calls.Count);
            Assert.AreEqual("inner", _calls[1][0]);
        }
    }
}
