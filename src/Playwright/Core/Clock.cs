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
        var timeIsGiven = options?.Time != null || options?.TimeInt64 != null || options?.TimeString != null || options?.TimeDate != null;
        await browserContext.SendMessageToServerAsync("clockInstall", timeIsGiven ? ParseTime(options.Time ?? options.TimeString, options?.TimeDate, options?.TimeInt64) : null).ConfigureAwait(false);
    }

    private Dictionary<string, object> ParseTime(string timeString, DateTime? timeDate, long? timeInt64)
    {
        var options = new Dictionary<string, object>();
        if (!string.IsNullOrEmpty(timeString))
        {
            options["timeString"] = timeString;
        }
        else if (timeDate != null)
        {
            options["timeNumber"] = ((DateTimeOffset)timeDate.Value).ToUnixTimeMilliseconds();
        }
        else if (timeInt64 != null)
        {
            options["timeNumber"] = timeInt64.Value;
        }
        return options;
    }

    private Dictionary<string, object> ParseTicks(long ticks)
        => new() { ["ticksNumber"] = ticks };

    private Dictionary<string, object> ParseTicks(string ticks)
        => new() { ["ticksString"] = ticks };

    public Task FastForwardAsync(long ticks)
        => browserContext.SendMessageToServerAsync("clockFastForward", ParseTicks(ticks));

    public Task FastForwardAsync(string ticks)
        => browserContext.SendMessageToServerAsync("clockFastForward", ParseTicks(ticks));

    public Task PauseAtAsync(long time)
        => browserContext.SendMessageToServerAsync("clockPauseAt", ParseTime(null, null, time));

    public Task PauseAtAsync(string time)
        => browserContext.SendMessageToServerAsync("clockPauseAt", ParseTime(time, null, null));

    public Task PauseAtAsync(DateTime time)
        => browserContext.SendMessageToServerAsync("clockPauseAt", ParseTime(null, time, null));

    public Task ResumeAsync()
        => browserContext.SendMessageToServerAsync("clockResume");

    public Task RunForAsync(long ticks)
        => browserContext.SendMessageToServerAsync("clockRunFor", ParseTicks(ticks));

    public Task RunForAsync(string ticks)
        => browserContext.SendMessageToServerAsync("clockRunFor", ParseTicks(ticks));

    public Task SetFixedTimeAsync(long time)
        => browserContext.SendMessageToServerAsync("clockSetFixedTime", ParseTime(null, null, time));

    public Task SetFixedTimeAsync(string time)
        => browserContext.SendMessageToServerAsync("clockSetFixedTime", ParseTime(time, null, null));

    public Task SetFixedTimeAsync(DateTime time)
        => browserContext.SendMessageToServerAsync("clockSetFixedTime", ParseTime(null, time, null));

    public Task SetSystemTimeAsync(long time)
        => browserContext.SendMessageToServerAsync("clockSetSystemTime", ParseTime(null, null, time));

    public Task SetSystemTimeAsync(string time)
        => browserContext.SendMessageToServerAsync("clockSetSystemTime", ParseTime(time, null, null));

    public Task SetSystemTimeAsync(DateTime time)
        => browserContext.SendMessageToServerAsync("clockSetSystemTime", ParseTime(null, time, null));
}
