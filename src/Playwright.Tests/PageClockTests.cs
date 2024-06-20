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

namespace Microsoft.Playwright.Tests;

public class PageClockTests : PageTestEx
{
    private readonly List<int[]> _calls = [];

    [SetUp]
    public async Task SetUp()
    {
        _calls.Clear();
        await Page.ExposeFunctionAsync("stub", () =>
        {
            _calls.Add([]);
        });
        await Page.ExposeFunctionAsync("stubWithValue", (int value) =>
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
            await Page.EvaluateAsync("() => { setInterval(() => { window.stubWithValue(new Date().getTime()); }, 10); }");
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
}
