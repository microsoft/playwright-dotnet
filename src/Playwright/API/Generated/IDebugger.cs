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
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Playwright;

/// <summary>
/// <para>
/// API for controlling the Playwright debugger. The debugger allows pausing script
/// execution and inspecting the page. Obtain the debugger instance via <see cref="IBrowserContext.Debugger"/>.
/// </para>
/// </summary>
public partial interface IDebugger
{
    /// <summary><para>Emitted when the debugger pauses or resumes.</para></summary>
    event EventHandler PausedStateChanged;

    /// <summary>
    /// <para>
    /// Returns details about the currently paused calls. Returns an empty array if the
    /// debugger is not paused.
    /// </para>
    /// </summary>
    IReadOnlyList<PausedDetail> PausedDetails { get; }

    /// <summary>
    /// <para>Configures the debugger to pause before the next action is executed.</para>
    /// <para>
    /// Throws if the debugger is already paused. Use <see cref="IDebugger.NextAsync"/>
    /// or <see cref="IDebugger.RunToAsync"/> to step while paused.
    /// </para>
    /// <para>
    /// Note that <see cref="IPage.PauseAsync"/> is equivalent to a "debugger" statement
    /// — it pauses execution at the call site immediately. On the contrary, <see cref="IDebugger.PauseAsync"/>
    /// is equivalent to "pause on next statement" — it configures the debugger to pause
    /// before the next action is executed.
    /// </para>
    /// </summary>
    Task PauseAsync();

    /// <summary><para>Resumes script execution. Throws if the debugger is not paused.</para></summary>
    Task ResumeAsync();

    /// <summary>
    /// <para>
    /// Resumes script execution and pauses again before the next action. Throws if the
    /// debugger is not paused.
    /// </para>
    /// </summary>
    Task NextAsync();

    /// <summary>
    /// <para>
    /// Resumes script execution and pauses when an action originates from the given source
    /// location. Throws if the debugger is not paused.
    /// </para>
    /// </summary>
    /// <param name="location">The source location to pause at.</param>
    Task RunToAsync(Location location);
}
