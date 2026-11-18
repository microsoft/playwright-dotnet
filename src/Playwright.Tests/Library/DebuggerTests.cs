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

using System.Diagnostics;
using Microsoft.Playwright.Tests.Fixtures;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Library;

/// <summary>Port of tests/library/debugger.spec.ts.</summary>
public class DebuggerTests : UpstreamBrowserTest
{
    [UpstreamTest("DB-001 should pause at next and resume")]
    public async Task ShouldPauseAtNextAndResume()
    {
        var context = await ContextFixture();
        var page = await context.NewPageAsync();
        await page.SetContentAsync("<div>click me</div>");
        var dbg = context.Debugger;
        Assert.That(dbg.PausedDetails, Is.Null);

        await dbg.RequestPauseAsync();
        var clickPromise = page.ClickAsync("div");
        await Once(h => dbg.PausedStateChanged += h, h => dbg.PausedStateChanged -= h);

        ExpectToEqual(
            dbg.PausedDetails,
            ObjectContaining(new
            {
                title = StringContaining("Click"),
                location = ObjectContaining(new
                {
                    // .NET: the file of this test is DebuggerTests.cs, upstream has 'debugger.spec'.
                    file = StringContaining("DebuggerTests"),
                    line = AnyNumber,
                    column = AnyNumber,
                }),
            }));

        await PromiseAll(
            dbg.ResumeAsync(),
            Once(h => dbg.PausedStateChanged += h, h => dbg.PausedStateChanged -= h),
            clickPromise);
        Assert.That(dbg.PausedDetails, Is.Null);
    }

    [UpstreamTest("DB-002 should pause at pause call")]
    public async Task ShouldPauseAtPauseCall()
    {
        var context = await ContextFixture();
        var page = await context.NewPageAsync();
        await page.SetContentAsync("<div>click me</div>");
        var dbg = context.Debugger;
        Assert.That(dbg.PausedDetails, Is.Null);

        await dbg.RequestPauseAsync();
        var pausePromise = page.PauseAsync();
        await Once(h => dbg.PausedStateChanged += h, h => dbg.PausedStateChanged -= h);

        ExpectToEqual(
            dbg.PausedDetails,
            ObjectContaining(new
            {
                title = StringContaining("Pause"),
            }));

        await dbg.ResumeAsync();
        await pausePromise;
    }

    [UpstreamTest("DB-003 should run to location")]
    public async Task ShouldRunToLocation()
    {
        var context = await ContextFixture();
        var page = await context.NewPageAsync();
        await page.SetContentAsync("<div>click me</div>");
        var dbg = context.Debugger;
        Assert.That(dbg.PausedDetails, Is.Null);

        // First, pause on next action.
        await dbg.RequestPauseAsync();
        IgnoreErrors(page.ClickAsync("div"));
        await Once(h => dbg.PausedStateChanged += h, h => dbg.PausedStateChanged -= h);

        // Now run to a specific location.
        // .NET: the line of this statement comes from a StackFrame, and the file of this test is DebuggerTests.cs.
        var line = new StackFrame(true).GetFileLineNumber();
        // Note: careful with the line offset below.
        await dbg.RunToAsync(new() { File = "DebuggerTests", Line = line + 4 });
        await page.ContentAsync(); // should not pause here
        var clickPromise = page.ClickAsync("div"); // should pause here
        await Once(h => dbg.PausedStateChanged += h, h => dbg.PausedStateChanged -= h);

        ExpectToEqual(
            dbg.PausedDetails,
            ObjectContaining(new
            {
                title = StringContaining("Click"),
            }));

        await dbg.ResumeAsync();
        await clickPromise;
    }
}
