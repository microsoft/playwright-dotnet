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

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Playwright.Core;

internal class Clock(BrowserContext browserContext) : IClock
{
    public async Task InstallAsync(ClockInstallOptions options = null)
    {
        Dictionary<string, object> args = null;
        if ((options.Time ?? options.TimeString) != null)
        {
            args = ParseTime(options.Time ?? options.TimeString);
        }
        else if (options.TimeDate != null)
        {
            args = ParseTime(options.TimeDate.Value);
        }
        await browserContext.SendMessageToServerAsync("clockInstall", args).ConfigureAwait(false);
    }

    private static Dictionary<string, object> ParseTime(string timeString)
        => new() { ["timeString"] = timeString };

    private static Dictionary<string, object> ParseTime(DateTime? timeDate)
        => new() { ["timeNumber"] = ((DateTimeOffset)timeDate.Value).ToUnixTimeMilliseconds() };

    private Dictionary<string, object> ParseTicks(long ticks)
        => new() { ["ticksNumber"] = ticks };

    private Dictionary<string, object> ParseTicks(string ticks)
        => new() { ["ticksString"] = ticks };

    public Task FastForwardAsync(long ticks)
        => browserContext.SendMessageToServerAsync("clockFastForward", ParseTicks(ticks));

    public Task FastForwardAsync(string ticks)
        => browserContext.SendMessageToServerAsync("clockFastForward", ParseTicks(ticks));

    public Task PauseAtAsync(string time)
        => browserContext.SendMessageToServerAsync("clockPauseAt", ParseTime(time));

    public Task PauseAtAsync(DateTime time)
        => browserContext.SendMessageToServerAsync("clockPauseAt", ParseTime(time));

    public Task ResumeAsync()
        => browserContext.SendMessageToServerAsync("clockResume");

    public Task RunForAsync(long ticks)
        => browserContext.SendMessageToServerAsync("clockRunFor", ParseTicks(ticks));

    public Task RunForAsync(string ticks)
        => browserContext.SendMessageToServerAsync("clockRunFor", ParseTicks(ticks));

    public Task SetFixedTimeAsync(string time)
        => browserContext.SendMessageToServerAsync("clockSetFixedTime", ParseTime(time));

    public Task SetFixedTimeAsync(DateTime time)
        => browserContext.SendMessageToServerAsync("clockSetFixedTime", ParseTime(time));

    public Task SetSystemTimeAsync(string time)
        => browserContext.SendMessageToServerAsync("clockSetSystemTime", ParseTime(time));

    public Task SetSystemTimeAsync(DateTime time)
        => browserContext.SendMessageToServerAsync("clockSetSystemTime", ParseTime(time));
}
