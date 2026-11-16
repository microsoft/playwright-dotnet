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

using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Microsoft.Playwright.Tests.Fixtures;

/// <summary><c>TestChildParams</c>: <c>childProcess({ command: [...], env: { ... } })</c>.</summary>
public sealed class TestChildParams
{
    public IReadOnlyList<string> Command { get; set; }

    public string Cwd { get; set; }

    /// <summary>Variables on top of the environment of the test run. A null value removes the variable, as <c>undefined</c> does.</summary>
    public Dictionary<string, string> Env { get; set; }

    public bool Shell { get; set; }

    public Action<string> OnOutput { get; set; }
}

/// <summary>
/// What <c>await child.exited</c> gives. .NET reports the death by a signal as the exit code 128 + signal,
/// so the exit codes 129, 137 and 143 are read as 'SIGHUP', 'SIGKILL' and 'SIGTERM'. 130 stays an exit
/// code: it is the one of a Playwright process that got SIGINT.
/// </summary>
public sealed record ProcessExit(int? ExitCode, string Signal);

/// <summary>The port of <c>TestChildProcess</c> of <c>tests/config/commonFixtures.ts</c>.</summary>
public sealed class TestChildProcess
{
    private static readonly string[] _cleanedEnv =
    [
        // BEGIN: Reserved CI
        "CI",
        "BUILD_URL",
        "CI_COMMIT_SHA",
        "CI_JOB_URL",
        "CI_PROJECT_URL",
        "GITHUB_ACTIONS",
        "GITHUB_REPOSITORY",
        "GITHUB_RUN_ID",
        "GITHUB_SERVER_URL",
        "GITHUB_SHA",
        "GITHUB_EVENT_PATH",
        // END: Reserved CI
        "PW_TEST_HTML_REPORT_OPEN",
        "PLAYWRIGHT_HTML_OPEN",
        "PW_TEST_DEBUG_REPORTERS",
        "PW_TEST_REPORTER",
        "PW_TEST_REPORTER_WS_ENDPOINT",
        "PW_TEST_SOURCE_TRANSFORM",
        "PW_TEST_SOURCE_TRANSFORM_SCOPE",
        "PWTEST_BOT_NAME",
        "PWTEST_SHARD_WEIGHTS",
        "TEST_WORKER_INDEX",
        "TEST_PARALLEL_INDEX",
        "NODE_OPTIONS",
    ];

    private static readonly bool _debug = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PWTEST_DEBUG"));
    private static readonly HashSet<TestChildProcess> _live = [];

    private readonly object _lock = new();
    private readonly StringBuilder _output = new();
    private readonly StringBuilder _stdout = new();
    private readonly StringBuilder _stderr = new();
    private readonly StringBuilder _fullOutput = new();
    private readonly List<(string Substring, int Count, TaskCompletionSource<bool> Done)> _outputCallbacks = [];
    private int _openStreams = 2;

    static TestChildProcess()
    {
        // No child outlives the test run, also when a test did not get to its teardown.
        AppDomain.CurrentDomain.ProcessExit += (_, _) =>
        {
            TestChildProcess[] live;
            lock (_live)
            {
                live = [.. _live];
            }
            foreach (var child in live)
            {
                child.KillProcessTree("SIGKILL");
            }
        };
    }

    public TestChildProcess(TestChildParams @params)
    {
        Params = @params;
        var startInfo = new ProcessStartInfo
        {
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            CreateNoWindow = true,
        };
        if (@params.Shell)
        {
            var command = string.Join(" ", @params.Command);
            if (Env.IsWindows)
            {
                startInfo.FileName = Environment.GetEnvironmentVariable("ComSpec") ?? "cmd.exe";
                startInfo.Arguments = $"/d /s /c \"{command}\"";
            }
            else
            {
                startInfo.FileName = "/bin/sh";
                startInfo.ArgumentList.Add("-c");
                startInfo.ArgumentList.Add(command);
            }
        }
        else
        {
            startInfo.FileName = @params.Command[0];
            foreach (var argument in @params.Command.Skip(1))
            {
                startInfo.ArgumentList.Add(argument);
            }
        }
        if (@params.Cwd != null)
        {
            startInfo.WorkingDirectory = @params.Cwd;
        }
        startInfo.Environment.Clear();
        foreach (var (name, value) in InheritAndCleanEnv(@params.Env))
        {
            startInfo.Environment[name] = value;
        }
        if (_debug)
        {
            Console.Out.Write($"\n\nLaunching {string.Join(" ", @params.Command)}\n");
        }
        OnOutput = @params.OnOutput;
        Process = Process.Start(startInfo);
        lock (_live)
        {
            _live.Add(this);
        }
        Exited = WaitForExitAsync(ReadAsync(Process.StandardOutput, false), ReadAsync(Process.StandardError, true));
    }

    public TestChildParams Params { get; }

    /// <summary><c>child.process</c>: <c>child.process.pid</c> is <c>child.Process.Id</c>.</summary>
    public Process Process { get; }

    public string Output
    {
        get
        {
            lock (_lock)
            {
                return _output.ToString();
            }
        }
    }

    public string Stdout
    {
        get
        {
            lock (_lock)
            {
                return _stdout.ToString();
            }
        }
    }

    public string Stderr
    {
        get
        {
            lock (_lock)
            {
                return _stderr.ToString();
            }
        }
    }

    public string FullOutput
    {
        get
        {
            lock (_lock)
            {
                return _fullOutput.ToString();
            }
        }
    }

    /// <summary>Called with every chunk of the output, on the thread that reads it.</summary>
    public Action<string> OnOutput { get; set; }

    public Task<ProcessExit> Exited { get; }

    /// <summary>Null when a signal ended the process.</summary>
    public Task<int?> ExitCode => ExitCodeAsync();

    /// <summary>
    /// <c>inheritAndCleanEnv(env)</c> of <c>tests/config/utils.ts</c>: the environment of the test run
    /// without the variables of the CI and of the test runner, plus <paramref name="env"/>.
    /// </summary>
    public static Dictionary<string, string> InheritAndCleanEnv(IDictionary<string, string> env)
    {
        var result = new Dictionary<string, string>(Env.IsWindows ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
        foreach (DictionaryEntry entry in Environment.GetEnvironmentVariables())
        {
            result[(string)entry.Key] = (string)entry.Value;
        }
        // tests/library/playwright.config.ts sets it in the process of the test run.
        result["PWTEST_UNDER_TEST"] = "1";
        foreach (var name in _cleanedEnv)
        {
            result.Remove(name);
        }
        if (env != null)
        {
            foreach (var (name, value) in env)
            {
                if (value == null)
                {
                    result.Remove(name);
                }
                else
                {
                    result[name] = value;
                }
            }
        }
        return result;
    }

    /// <summary>
    /// <c>killProcessGroup(pid, signal)</c>. Upstream starts a child as the leader of a new process group and
    /// signals the group. .NET can not start one, so the group of a process that does not lead one is the
    /// process and its descendants that are still in its group.
    /// </summary>
    public static void KillProcessGroup(int pid, string signal = "SIGKILL")
    {
        // On Windows, we always call `taskkill` no matter signal.
        if (Env.IsWindows)
        {
            try
            {
                using var taskkill = Process.Start(new ProcessStartInfo("taskkill", $"/pid {pid.ToString(CultureInfo.InvariantCulture)} /T /F /FI \"MEMUSAGE gt 0\"")
                {
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                });
                taskkill.OutputDataReceived += (_, _) => { };
                taskkill.ErrorDataReceived += (_, _) => { };
                taskkill.BeginOutputReadLine();
                taskkill.BeginErrorReadLine();
                taskkill.WaitForExit();
            }
            catch (Exception)
            {
                // the process might have already stopped
            }
            return;
        }

        var processes = new Dictionary<int, ProcessData>();
        foreach (var (id, ppid, pgrp) in Env.IsMac ? ReadAllProcessesMacOS() : ReadAllProcessesLinux())
        {
            processes[id] = new ProcessData { Pid = id, Ppid = ppid, Pgrp = pgrp };
        }
        foreach (var process in processes.Values)
        {
            // On POSIX, certain processes might not have parent (e.g. PID=1 and occasionally PID=2)
            // or we might not have access to it proc info.
            if (processes.TryGetValue(process.Ppid, out var parent))
            {
                parent.Children.Add(process);
            }
        }
        if (!processes.TryGetValue(pid, out var rootProcess))
        {
            return;
        }
        var tree = new List<ProcessData>();
        Flatten(rootProcess, tree);

        // In case of POSIX and `SIGINT` signal, send it to the main process group only.
        if (signal == "SIGINT")
        {
            if (rootProcess.Pgrp == pid)
            {
                TryKill(-pid, "SIGINT");
                return;
            }
            foreach (var process in tree.Where(process => process.Pgrp == rootProcess.Pgrp))
            {
                TryKill(process.Pid, "SIGINT");
            }
            return;
        }

        // In case of POSIX and `SIGKILL` signal, we should send it to all descendant process groups.
        var ownGroup = processes.TryGetValue(Environment.ProcessId, out var self) ? self.Pgrp : -1;
        var descendantProcessGroups = new HashSet<int>();
        foreach (var process in tree)
        {
            // Process can nullify its own process group with `setpgid`. Use its PID instead.
            var group = process.Pgrp != 0 ? process.Pgrp : process.Pid;
            if (group == ownGroup)
            {
                // The group of the test run itself: only the descendant goes.
                TryKill(process.Pid, "SIGKILL");
            }
            else
            {
                descendantProcessGroups.Add(group);
            }
        }
        foreach (var pgrp in descendantProcessGroups)
        {
            TryKill(-pgrp, "SIGKILL");
        }
    }

    public string[] OutputLines()
    {
        var strippedOutput = Utils.StripAnsi(Output);
        return strippedOutput.Split('\n').Where(line => line.StartsWith("%%", StringComparison.Ordinal)).Select(line => line.Substring(2).Trim()).ToArray();
    }

    public Task<ProcessExit> Kill(string signal = "SIGKILL")
    {
        KillProcessTree(signal);
        return Exited;
    }

    public async Task CleanExit()
    {
        var r = await Exited.ConfigureAwait(false);
        if (r.ExitCode is not null and not 0)
        {
            throw new InvalidOperationException($"Process failed with exit code {r.ExitCode}. Output:\n{Output}");
        }
        if (r.Signal != null)
        {
            throw new InvalidOperationException($"Process received signal: {r.Signal}. Output:\n{Output}");
        }
    }

    /// <summary>Fails when the output ends without the text, where upstream waits until the test times out.</summary>
    public Task WaitForOutput(string substring, int count = 1)
    {
        lock (_lock)
        {
            if (CountTimes(Utils.StripAnsi(_output.ToString()), substring) >= count)
            {
                return Task.CompletedTask;
            }
            if (_openStreams == 0)
            {
                return Task.FromException(OutputEnded(substring));
            }
            var done = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            _outputCallbacks.Add((substring, count, done));
            return done.Task;
        }
    }

    public void ClearOutput()
    {
        lock (_lock)
        {
            _output.Clear();
        }
    }

    public void Write(string chars)
    {
        Process.StandardInput.Write(chars);
        Process.StandardInput.Flush();
    }

    internal static int CountTimes(string s, string sub)
    {
        var result = 0;
        for (var index = 0; index != -1;)
        {
            index = s.IndexOf(sub, index, StringComparison.Ordinal);
            if (index != -1)
            {
                result++;
                index += sub.Length;
            }
        }
        return result;
    }

    private static List<(int Pid, int Ppid, int Pgrp)> ReadAllProcessesLinux()
    {
        var result = new List<(int Pid, int Ppid, int Pgrp)>();
        foreach (var dir in Directory.GetDirectories("/proc"))
        {
            if (!int.TryParse(Path.GetFileName(dir), NumberStyles.None, CultureInfo.InvariantCulture, out var pid))
            {
                continue;
            }
            try
            {
                var statFile = File.ReadAllText($"/proc/{pid.ToString(CultureInfo.InvariantCulture)}/stat");
                // Format of /proc/*/stat is described https://man7.org/linux/man-pages/man5/proc.5.html
                var match = Regex.Match(statFile, @"^(?<pid>\d+)\s+\((?<comm>.*)\)\s+(?<state>R|S|D|Z|T|t|W|X|x|K|W|P)\s+(?<ppid>\d+)\s+(?<pgrp>\d+)");
                if (match.Success)
                {
                    result.Add((
                        int.Parse(match.Groups["pid"].Value, CultureInfo.InvariantCulture),
                        int.Parse(match.Groups["ppid"].Value, CultureInfo.InvariantCulture),
                        int.Parse(match.Groups["pgrp"].Value, CultureInfo.InvariantCulture)));
                }
            }
            catch (Exception)
            {
                // We don't have access to some /proc/<pid>/stat file.
            }
        }
        return result;
    }

    private static List<(int Pid, int Ppid, int Pgrp)> ReadAllProcessesMacOS()
    {
        var result = new List<(int Pid, int Ppid, int Pgrp)>();
        using var ps = Process.Start(new ProcessStartInfo("ps")
        {
            ArgumentList = { "-eo", "pid,pgid,ppid" },
            UseShellExecute = false,
            RedirectStandardOutput = true,
        });
        var lines = ps.StandardOutput.ReadToEnd().Trim().Split('\n');
        ps.WaitForExit();
        foreach (var line in lines)
        {
            var tokens = line.Trim().Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            // On linux, the very first line of `ps` is the header with "PID PGID PPID".
            if (tokens.Length != 3 ||
                !int.TryParse(tokens[0], NumberStyles.None, CultureInfo.InvariantCulture, out var pid) ||
                !int.TryParse(tokens[1], NumberStyles.None, CultureInfo.InvariantCulture, out var pgrp) ||
                !int.TryParse(tokens[2], NumberStyles.None, CultureInfo.InvariantCulture, out var ppid))
            {
                continue;
            }
            result.Add((pid, ppid, pgrp));
        }
        return result;
    }

    private static void Flatten(ProcessData processData, List<ProcessData> result)
    {
        if (result.Contains(processData))
        {
            return;
        }
        result.Add(processData);
        foreach (var child in processData.Children)
        {
            Flatten(child, result);
        }
    }

    private static void TryKill(int pid, string signal)
    {
        try
        {
            NodeProcess.Kill(pid, signal);
        }
        catch (Exception)
        {
            // the process might have already stopped
        }
    }

    private void KillProcessTree(string signal)
    {
        if (Process.HasExited)
        {
            return;
        }
        KillProcessGroup(Process.Id, signal);
    }

    private async Task<int?> ExitCodeAsync() => (await Exited.ConfigureAwait(false)).ExitCode;

    private async Task<ProcessExit> WaitForExitAsync(Task stdout, Task stderr)
    {
        await Process.WaitForExitAsync().ConfigureAwait(false);
        // What the process wrote before it exited is in the output when the exit is reported.
        // A descendant that outlives the process may hold the pipes, so the wait has a limit.
        await Task.WhenAny(Task.WhenAll(stdout, stderr), Task.Delay(1000)).ConfigureAwait(false);
        lock (_live)
        {
            _live.Remove(this);
        }
        var exitCode = Process.ExitCode;
        var signal = Env.IsWindows ? null : exitCode switch
        {
            128 + 1 => "SIGHUP",
            128 + 9 => "SIGKILL",
            128 + 15 => "SIGTERM",
            _ => null,
        };
        return signal == null ? new ProcessExit(exitCode, null) : new ProcessExit(null, signal);
    }

    private async Task ReadAsync(StreamReader reader, bool isStderr)
    {
        var buffer = new char[8192];
        try
        {
            while (true)
            {
                var read = await reader.ReadAsync(buffer.AsMemory()).ConfigureAwait(false);
                if (read == 0)
                {
                    break;
                }
                AppendChunk(isStderr, new string(buffer, 0, read));
            }
        }
        catch (Exception e) when (e is IOException or ObjectDisposedException)
        {
            // The pipe is closed.
        }
        finally
        {
            lock (_lock)
            {
                if (--_openStreams == 0)
                {
                    foreach (var (substring, _, done) in _outputCallbacks)
                    {
                        done.TrySetException(OutputEnded(substring));
                    }
                    _outputCallbacks.Clear();
                }
            }
        }
    }

    private void AppendChunk(bool isStderr, string chunk)
    {
        // The chunks of the two streams come from two threads: one at a time, as in Node.js.
        lock (_lock)
        {
            _output.Append(chunk);
            if (isStderr)
            {
                _stderr.Append(chunk);
            }
            else
            {
                _stdout.Append(chunk);
            }
            if (_debug)
            {
                Console.Out.Write(chunk);
            }
            else
            {
                _fullOutput.Append(chunk);
            }
            OnOutput?.Invoke(chunk);
            if (_outputCallbacks.Count == 0)
            {
                return;
            }
            var output = Utils.StripAnsi(_output.ToString());
            _outputCallbacks.RemoveAll(callback =>
            {
                if (CountTimes(output, callback.Substring) < callback.Count)
                {
                    return false;
                }
                callback.Done.TrySetResult(true);
                return true;
            });
        }
    }

    private InvalidOperationException OutputEnded(string substring) =>
        new($"The output of the process ended without \"{substring}\". Output:\n{_output}");

    private sealed class ProcessData
    {
        public int Pid { get; init; }

        public int Ppid { get; init; }

        public int Pgrp { get; init; }

        public List<ProcessData> Children { get; } = [];
    }
}
