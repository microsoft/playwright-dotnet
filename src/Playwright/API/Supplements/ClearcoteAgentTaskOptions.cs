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
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

using System.Collections.Generic;
using System.Text.Json;

namespace Microsoft.Playwright;

/// <summary>
/// Options for running a Clearcote in-browser AI agent task.
/// </summary>
public class ClearcoteAgentTaskOptions
{
    /// <summary><para>Maximum observe-think-act iterations before the task stops.</para></summary>
    public int? MaxSteps { get; set; }

    /// <summary><para>Override the launch-time agent model for this task.</para></summary>
    public string? Model { get; set; }

    /// <summary><para>Optional JSON plan or hint string passed to the agent planner.</para></summary>
    public string? PlanJson { get; set; }
}

/// <summary>
/// One recorded step from a Clearcote agent run.
/// </summary>
public sealed class ClearcoteAgentStep
{
    /// <summary><para>Agent action name, when the engine reports one.</para></summary>
    public string? Action { get; init; }

    /// <summary><para>Agent action status, when the engine reports one.</para></summary>
    public string? Status { get; init; }

    /// <summary><para>Raw step JSON for fields not represented by strongly-typed properties.</para></summary>
    public JsonElement Raw { get; init; }
}

/// <summary>
/// Result from a Clearcote in-browser AI agent task.
/// </summary>
public sealed class ClearcoteAgentTaskResult
{
    /// <summary><para>Whether the agent reported the goal as completed.</para></summary>
    public bool Success { get; init; }

    /// <summary><para>The agent's final completion summary or error text.</para></summary>
    public string FinalText { get; init; } = string.Empty;

    /// <summary><para>Parsed per-step journal.</para></summary>
    public IReadOnlyList<ClearcoteAgentStep> Steps { get; init; } = System.Array.Empty<ClearcoteAgentStep>();

    /// <summary><para>The raw <c>stepsJson</c> payload returned by the engine.</para></summary>
    public string StepsJson { get; init; } = "[]";
}
