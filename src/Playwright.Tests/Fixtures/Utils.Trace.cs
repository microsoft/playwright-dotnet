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

using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace Microsoft.Playwright.Tests.Fixtures;

/// <summary>A frame of <c>stacks</c> of <see cref="Utils.ParseTraceRaw"/>: <c>StackFrame</c> of the trace.</summary>
public sealed record TraceStackFrame(string File, int Line, int Column, string Function);

/// <summary>
/// The result of <see cref="Utils.ParseTraceRaw"/>. <c>const { events, actions } = await parseTraceRaw(file)</c>
/// is <c>var (events, _, actions, _, _) = await ParseTraceRaw(file)</c>: the order is events, resources,
/// actions, actionObjects, stacks.
/// </summary>
/// <param name="Events">The events of the <c>.trace</c> files, then of the <c>.network</c> files.</param>
/// <param name="Resources">The entries of the zip, in the order of the zip.</param>
/// <param name="Actions">The titles of the actions, as the trace viewer shows them.</param>
/// <param name="ActionObjects">The actions by start time: the 'before' event with <c>type: 'action'</c>, and <c>endTime</c>, <c>error</c>, <c>result</c>, <c>point</c> of the events that follow it.</param>
/// <param name="Stacks">The client stacks by call id.</param>
public sealed record TraceRaw(
    List<JsonElement> Events,
    Dictionary<string, byte[]> Resources,
    List<string> Actions,
    List<Dictionary<string, JsonElement>> ActionObjects,
    Dictionary<string, List<TraceStackFrame>> Stacks);

public static partial class Utils
{
    /// <summary><c>parseTraceRaw(file)</c>.</summary>
    public static async Task<TraceRaw> ParseTraceRaw(string file)
    {
        var names = new List<string>();
        var resources = new Dictionary<string, byte[]>();
        using (var zipFS = ZipFile.OpenRead(file))
        {
            foreach (var entry in zipFS.Entries)
            {
                using var stream = entry.Open();
                using var buffer = new MemoryStream();
                await stream.CopyToAsync(buffer).ConfigureAwait(false);
                names.Add(entry.FullName);
                resources[entry.FullName] = buffer.ToArray();
            }
        }

        // A Map keeps the place of a key that is set again.
        var actionMap = new Dictionary<string, Dictionary<string, JsonElement>>();
        var actionObjects = new List<Dictionary<string, JsonElement>>();
        var events = new List<JsonElement>();
        foreach (var traceFile in names.Where(name => name.EndsWith(".trace", StringComparison.Ordinal)))
        {
            foreach (var line in Encoding.UTF8.GetString(resources[traceFile]).Split('\n'))
            {
                if (line.Length == 0)
                {
                    continue;
                }
                var @event = JsonSerializer.Deserialize<JsonElement>(line);
                events.Add(@event);

                var type = @event.GetProperty("type").GetString();
                if (type == "before")
                {
                    var action = @event.EnumerateObject().ToDictionary(property => property.Name, property => property.Value);
                    action["type"] = JsonSerializer.SerializeToElement("action");
                    action["endTime"] = JsonSerializer.SerializeToElement(0);
                    var callId = @event.GetProperty("callId").GetString();
                    if (actionMap.TryGetValue(callId, out var previous))
                    {
                        actionObjects[actionObjects.IndexOf(previous)] = action;
                    }
                    else
                    {
                        actionObjects.Add(action);
                    }
                    actionMap[callId] = action;
                }
                else if (type == "input")
                {
                    var existing = actionMap[@event.GetProperty("callId").GetString()];
                    Assign(existing, @event, "point");
                }
                else if (type == "after")
                {
                    var existing = actionMap[@event.GetProperty("callId").GetString()];
                    Assign(existing, @event, "endTime");
                    Assign(existing, @event, "error");
                    Assign(existing, @event, "result");
                }
            }
        }

        foreach (var networkFile in names.Where(name => name.EndsWith(".network", StringComparison.Ordinal)))
        {
            foreach (var line in Encoding.UTF8.GetString(resources[networkFile]).Split('\n'))
            {
                if (line.Length > 0)
                {
                    events.Add(JsonSerializer.Deserialize<JsonElement>(line));
                }
            }
        }

        var stacks = new Dictionary<string, List<TraceStackFrame>>();
        foreach (var stacksFile in names.Where(name => name.EndsWith(".stacks", StringComparison.Ordinal)))
        {
            // iso.parseClientSideCallMetadata()
            var data = JsonSerializer.Deserialize<JsonElement>(Encoding.UTF8.GetString(resources[stacksFile]));
            var files = data.GetProperty("files").EnumerateArray().Select(f => f.GetString()).ToArray();
            foreach (var s in data.GetProperty("stacks").EnumerateArray())
            {
                stacks[s[0].GetString()] = s[1].EnumerateArray()
                    .Select(f => new TraceStackFrame(files[f[0].GetInt32()], f[1].GetInt32(), f[2].GetInt32(), f.GetArrayLength() > 3 ? f[3].GetString() : null))
                    .ToList();
            }
        }

        actionObjects = actionObjects.OrderBy(a => a["startTime"].GetDouble()).ToList();
        var actions = await RenderFullTitlesForCalls(actionObjects).ConfigureAwait(false);
        return new TraceRaw(events, resources, actions, actionObjects, stacks);

        static void Assign(Dictionary<string, JsonElement> target, JsonElement source, string name)
        {
            if (source.TryGetProperty(name, out var value))
            {
                target[name] = value;
            }
            else
            {
                target.Remove(name);
            }
        }
    }

    // actionObjects.map(a => iso.renderFullTitleForCall({ ...a, type: a.class })), in the Node.js of the driver
    // with the bundle of the driver.
    private static async Task<List<string>> RenderFullTitlesForCalls(List<Dictionary<string, JsonElement>> actionObjects)
    {
        if (actionObjects.Count == 0)
        {
            return [];
        }
        const string Script = @"
const { iso } = require(process.argv[1]);
let data = '';
process.stdin.setEncoding('utf8');
process.stdin.on('data', chunk => data += chunk);
process.stdin.on('end', () => {
  process.stdout.write(JSON.stringify(JSON.parse(data).map(a => iso.renderFullTitleForCall({ ...a, type: a.class }))));
});";
        var startInfo = new ProcessStartInfo(NodeProcess.ExecPath)
        {
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            StandardInputEncoding = new UTF8Encoding(false),
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        startInfo.ArgumentList.Add("-e");
        startInfo.ArgumentList.Add(Script);
        startInfo.ArgumentList.Add(Path.Combine(Path.GetDirectoryName(NodeProcess.CliPath), "lib", "coreBundle.js"));
        using var process = Process.Start(startInfo);
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        await process.StandardInput.WriteAsync(JsonSerializer.Serialize(actionObjects)).ConfigureAwait(false);
        process.StandardInput.Close();
        var output = await stdout.ConfigureAwait(false);
        var errors = await stderr.ConfigureAwait(false);
        await process.WaitForExitAsync().ConfigureAwait(false);
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"renderFullTitleForCall failed with the exit code {process.ExitCode}:\n{errors}");
        }
        return JsonSerializer.Deserialize<List<string>>(output);
    }
}
