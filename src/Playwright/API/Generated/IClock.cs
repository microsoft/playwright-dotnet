/*
 * MIT License
 *
 * Copyright (c) Microsoft Corporation.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
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
using System.Threading.Tasks;

#nullable enable

namespace Microsoft.Playwright;

/// <summary>
/// <para>
/// Accurately simulating time-dependent behavior is essential for verifying the correctness
/// of applications. Learn more about <a href="https://playwright.dev/dotnet/docs/clock">clock
/// emulation</a>.
/// </para>
/// <para>
/// Note that clock is installed for the entire <see cref="IBrowserContext"/>, so the
/// time in all the pages and iframes is controlled by the same clock.
/// </para>
/// </summary>
public partial interface IClock
{
    /// <summary>
    /// <para>
    /// Advance the clock by jumping forward in time. Only fires due timers at most once.
    /// This is equivalent to user closing the laptop lid for a while and reopening it later,
    /// after given time.
    /// </para>
    /// <para>**Usage**</para>
    /// <code>
    /// await page.Clock.FastForwardAsync(1000);<br/>
    /// await page.Clock.FastForwardAsync("30:00");
    /// </code>
    /// </summary>
    /// <param name="ticks">
    /// Time may be the number of milliseconds to advance the clock by or a human-readable
    /// string. Valid string formats are "08" for eight seconds, "01:00" for one minute
    /// and "02:34:10" for two hours, 34 minutes and ten seconds.
    /// </param>
    Task FastForwardAsync(long ticks);

    /// <summary>
    /// <para>
    /// Advance the clock by jumping forward in time. Only fires due timers at most once.
    /// This is equivalent to user closing the laptop lid for a while and reopening it later,
    /// after given time.
    /// </para>
    /// <para>**Usage**</para>
    /// <code>
    /// await page.Clock.FastForwardAsync(1000);<br/>
    /// await page.Clock.FastForwardAsync("30:00");
    /// </code>
    /// </summary>
    /// <param name="ticks">
    /// Time may be the number of milliseconds to advance the clock by or a human-readable
    /// string. Valid string formats are "08" for eight seconds, "01:00" for one minute
    /// and "02:34:10" for two hours, 34 minutes and ten seconds.
    /// </param>
    Task FastForwardAsync(string ticks);

    /// <summary>
    /// <para>Install fake implementations for the following time-related functions:</para>
    /// <list type="bullet">
    /// <item><description><c>Date</c></description></item>
    /// <item><description><c>setTimeout</c></description></item>
    /// <item><description><c>clearTimeout</c></description></item>
    /// <item><description><c>setInterval</c></description></item>
    /// <item><description><c>clearInterval</c></description></item>
    /// <item><description><c>requestAnimationFrame</c></description></item>
    /// <item><description><c>cancelAnimationFrame</c></description></item>
    /// <item><description><c>requestIdleCallback</c></description></item>
    /// <item><description><c>cancelIdleCallback</c></description></item>
    /// <item><description><c>performance</c></description></item>
    /// </list>
    /// <para>
    /// Fake timers are used to manually control the flow of time in tests. They allow you
    /// to advance time, fire timers, and control the behavior of time-dependent functions.
    /// See <see cref="IClock.RunForAsync"/> and <see cref="IClock.FastForwardAsync"/> for
    /// more information.
    /// </para>
    /// </summary>
    /// <param name="options">Call options</param>
    Task InstallAsync(ClockInstallOptions? options = default);

    /// <summary>
    /// <para>Advance the clock, firing all the time-related callbacks.</para>
    /// <para>**Usage**</para>
    /// <code>
    /// await page.Clock.RunForAsync(1000);<br/>
    /// await page.Clock.RunForAsync("30:00");
    /// </code>
    /// </summary>
    /// <param name="ticks">
    /// Time may be the number of milliseconds to advance the clock by or a human-readable
    /// string. Valid string formats are "08" for eight seconds, "01:00" for one minute
    /// and "02:34:10" for two hours, 34 minutes and ten seconds.
    /// </param>
    Task RunForAsync(long ticks);

    /// <summary>
    /// <para>Advance the clock, firing all the time-related callbacks.</para>
    /// <para>**Usage**</para>
    /// <code>
    /// await page.Clock.RunForAsync(1000);<br/>
    /// await page.Clock.RunForAsync("30:00");
    /// </code>
    /// </summary>
    /// <param name="ticks">
    /// Time may be the number of milliseconds to advance the clock by or a human-readable
    /// string. Valid string formats are "08" for eight seconds, "01:00" for one minute
    /// and "02:34:10" for two hours, 34 minutes and ten seconds.
    /// </param>
    Task RunForAsync(string ticks);

    /// <summary>
    /// <para>
    /// Advance the clock by jumping forward in time and pause the time. Once this method
    /// is called, no timers are fired unless <see cref="IClock.RunForAsync"/>, <see cref="IClock.FastForwardAsync"/>,
    /// <see cref="IClock.PauseAtAsync"/> or <see cref="IClock.ResumeAsync"/> is called.
    /// </para>
    /// <para>
    /// Only fires due timers at most once. This is equivalent to user closing the laptop
    /// lid for a while and reopening it at the specified time and pausing.
    /// </para>
    /// <para>**Usage**</para>
    /// <code>
    /// await page.Clock.PauseAtAsync(DateTime.Parse("2020-02-02"));<br/>
    /// await page.Clock.PauseAtAsync("2020-02-02");
    /// </code>
    /// </summary>
    /// <param name="time">
    /// </param>
    Task PauseAtAsync(long time);

    /// <summary>
    /// <para>
    /// Advance the clock by jumping forward in time and pause the time. Once this method
    /// is called, no timers are fired unless <see cref="IClock.RunForAsync"/>, <see cref="IClock.FastForwardAsync"/>,
    /// <see cref="IClock.PauseAtAsync"/> or <see cref="IClock.ResumeAsync"/> is called.
    /// </para>
    /// <para>
    /// Only fires due timers at most once. This is equivalent to user closing the laptop
    /// lid for a while and reopening it at the specified time and pausing.
    /// </para>
    /// <para>**Usage**</para>
    /// <code>
    /// await page.Clock.PauseAtAsync(DateTime.Parse("2020-02-02"));<br/>
    /// await page.Clock.PauseAtAsync("2020-02-02");
    /// </code>
    /// </summary>
    /// <param name="time">
    /// </param>
    Task PauseAtAsync(string time);

    /// <summary>
    /// <para>
    /// Advance the clock by jumping forward in time and pause the time. Once this method
    /// is called, no timers are fired unless <see cref="IClock.RunForAsync"/>, <see cref="IClock.FastForwardAsync"/>,
    /// <see cref="IClock.PauseAtAsync"/> or <see cref="IClock.ResumeAsync"/> is called.
    /// </para>
    /// <para>
    /// Only fires due timers at most once. This is equivalent to user closing the laptop
    /// lid for a while and reopening it at the specified time and pausing.
    /// </para>
    /// <para>**Usage**</para>
    /// <code>
    /// await page.Clock.PauseAtAsync(DateTime.Parse("2020-02-02"));<br/>
    /// await page.Clock.PauseAtAsync("2020-02-02");
    /// </code>
    /// </summary>
    /// <param name="time">
    /// </param>
    Task PauseAtAsync(DateTime time);

    /// <summary>
    /// <para>
    /// Resumes timers. Once this method is called, time resumes flowing, timers are fired
    /// as usual.
    /// </para>
    /// </summary>
    Task ResumeAsync();

    /// <summary>
    /// <para>
    /// Makes <c>Date.now</c> and <c>new Date()</c> return fixed fake time at all times,
    /// keeps all the timers running.
    /// </para>
    /// <para>**Usage**</para>
    /// <code>
    /// await page.Clock.SetFixedTimeAsync(DateTime.Now);<br/>
    /// await page.Clock.SetFixedTimeAsync(new DateTime(2020, 2, 2));<br/>
    /// await page.Clock.SetFixedTimeAsync("2020-02-02");
    /// </code>
    /// </summary>
    /// <param name="time">Time to be set.</param>
    Task SetFixedTimeAsync(long time);

    /// <summary>
    /// <para>
    /// Makes <c>Date.now</c> and <c>new Date()</c> return fixed fake time at all times,
    /// keeps all the timers running.
    /// </para>
    /// <para>**Usage**</para>
    /// <code>
    /// await page.Clock.SetFixedTimeAsync(DateTime.Now);<br/>
    /// await page.Clock.SetFixedTimeAsync(new DateTime(2020, 2, 2));<br/>
    /// await page.Clock.SetFixedTimeAsync("2020-02-02");
    /// </code>
    /// </summary>
    /// <param name="time">Time to be set.</param>
    Task SetFixedTimeAsync(string time);

    /// <summary>
    /// <para>
    /// Makes <c>Date.now</c> and <c>new Date()</c> return fixed fake time at all times,
    /// keeps all the timers running.
    /// </para>
    /// <para>**Usage**</para>
    /// <code>
    /// await page.Clock.SetFixedTimeAsync(DateTime.Now);<br/>
    /// await page.Clock.SetFixedTimeAsync(new DateTime(2020, 2, 2));<br/>
    /// await page.Clock.SetFixedTimeAsync("2020-02-02");
    /// </code>
    /// </summary>
    /// <param name="time">Time to be set.</param>
    Task SetFixedTimeAsync(DateTime time);

    /// <summary>
    /// <para>Sets current system time but does not trigger any timers.</para>
    /// <para>**Usage**</para>
    /// <code>
    /// await page.Clock.SetSystemTimeAsync(DateTime.Now);<br/>
    /// await page.Clock.SetSystemTimeAsync(new DateTime(2020, 2, 2));<br/>
    /// await page.Clock.SetSystemTimeAsync("2020-02-02");
    /// </code>
    /// </summary>
    /// <param name="time">
    /// </param>
    Task SetSystemTimeAsync(long time);

    /// <summary>
    /// <para>Sets current system time but does not trigger any timers.</para>
    /// <para>**Usage**</para>
    /// <code>
    /// await page.Clock.SetSystemTimeAsync(DateTime.Now);<br/>
    /// await page.Clock.SetSystemTimeAsync(new DateTime(2020, 2, 2));<br/>
    /// await page.Clock.SetSystemTimeAsync("2020-02-02");
    /// </code>
    /// </summary>
    /// <param name="time">
    /// </param>
    Task SetSystemTimeAsync(string time);

    /// <summary>
    /// <para>Sets current system time but does not trigger any timers.</para>
    /// <para>**Usage**</para>
    /// <code>
    /// await page.Clock.SetSystemTimeAsync(DateTime.Now);<br/>
    /// await page.Clock.SetSystemTimeAsync(new DateTime(2020, 2, 2));<br/>
    /// await page.Clock.SetSystemTimeAsync("2020-02-02");
    /// </code>
    /// </summary>
    /// <param name="time">
    /// </param>
    Task SetSystemTimeAsync(DateTime time);
}

#nullable disable
