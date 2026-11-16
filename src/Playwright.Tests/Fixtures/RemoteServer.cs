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
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Microsoft.Playwright.Tests.Fixtures;

/// <summary>
/// What <c>startRemoteServer(kind)</c> gives, the port of <c>tests/config/remoteServer.ts</c>. Upstream
/// types the server of the 'launchServer' kind as <c>RemoteServer</c> with an overload, which a delegate
/// does not have: the members of <see cref="RemoteServer"/> are here, and fail for the other kind.
/// </summary>
public abstract class PlaywrightServer
{
    public abstract string WsEndpoint();

    public abstract Task Close();

    /// <summary>The value of <c>(key=>value)</c> in the output of the server, when it comes.</summary>
    public virtual Task<string> Out(string key) => throw LaunchServerOnly();

    /// <summary><c>remoteServer.child().pid</c> is <c>remoteServer.Child().Id</c>.</summary>
    public virtual Process Child() => throw LaunchServerOnly();

    /// <summary>Null when a signal ended the process, see <see cref="ProcessExit"/>.</summary>
    public virtual Task<int?> ChildExitCode() => throw LaunchServerOnly();

    public virtual Task<string> ChildSignal() => throw LaunchServerOnly();

    private static InvalidOperationException LaunchServerOnly() => new("Only the server of the 'launchServer' kind has this member");
}

/// <summary>The options of <c>RunServer.start()</c>.</summary>
public sealed class RunServerOptions
{
    /// <summary>'extension' or 'default'.</summary>
    public string Mode { get; set; }

    /// <summary>A null value removes the variable.</summary>
    public Dictionary<string, string> Env { get; set; }

    public string ArtifactsDir { get; set; }

    public bool Unsafe { get; set; }
}

/// <summary>The 'run-server' kind: <c>cli.js run-server</c> of the driver.</summary>
public sealed class RunServer : PlaywrightServer
{
    private TestChildProcess _process;
    private string _wsEndpoint;

    public async Task Start(ChildProcess childProcess, RunServerOptions options = null)
    {
        var command = new List<string> { NodeProcess.ExecPath, NodeProcess.CliPath, "run-server" };
        if (options?.Mode == "extension")
        {
            command.Add("--mode=extension");
        }
        if (!string.IsNullOrEmpty(options?.ArtifactsDir))
        {
            command.Add($"--artifacts-dir={options.ArtifactsDir}");
        }
        if (options?.Unsafe == true)
        {
            command.Add("--unsafe");
        }
        var env = new Dictionary<string, string> { ["NODE_OPTIONS"] = Environment.GetEnvironmentVariable("NODE_OPTIONS") };
        if (options?.Env != null)
        {
            foreach (var (name, value) in options.Env)
            {
                env[name] = value;
            }
        }

        var wsEndpointPromise = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var output = string.Empty;
        _process = childProcess(new()
        {
            Command = command,
            Env = env,
            // Upstream takes a chunk for a line. A chunk is any part of the output here.
            OnOutput = data =>
            {
                output += data;
                var match = Regex.Match(output, @"^Listening on (.*)\n", RegexOptions.Multiline);
                if (match.Success)
                {
                    wsEndpointPromise.TrySetResult(match.Groups[1].Value.Trim());
                }
            },
        });

        await Task.WhenAny(wsEndpointPromise.Task, _process.Exited).ConfigureAwait(false);
        if (!wsEndpointPromise.Task.IsCompleted)
        {
            throw new InvalidOperationException($"run-server exited before it listened. Output:\n{_process.Output}");
        }
        _wsEndpoint = await wsEndpointPromise.Task.ConfigureAwait(false);
    }

    public override string WsEndpoint() => _wsEndpoint;

    public override async Task Close()
    {
        await _process.Kill("SIGINT").ConfigureAwait(false);
    }
}

public sealed class RemoteServerOptions
{
    public bool StallOnClose { get; set; }

    public bool DisconnectOnSIGHUP { get; set; }

    public string ExitOnFile { get; set; }

    public bool ExitOnWarning { get; set; }

    public bool InCluster { get; set; }

    public string Url { get; set; }

    public bool StartStopAndRunHttp { get; set; }

    public bool SharedBrowser { get; set; }

    public string ArtifactsDir { get; set; }

    /// <summary>Only supported by the 'run-server' kind.</summary>
    public bool Unsafe { get; set; }

    /// <summary>Only supported by the 'run-server' kind. A null value removes the variable.</summary>
    public Dictionary<string, string> Env { get; set; }
}

/// <summary>
/// The 'launchServer' kind: <c>tests/config/remote-server-impl.js</c> of the upstream checkout, which calls
/// <c>browserType.launchServer()</c> of its <c>playwright-core</c>, in the Node.js of the driver.
/// </summary>
public sealed class RemoteServer : PlaywrightServer
{
    private static readonly Regex _outputRegex = new(@"\(([^()]+)=>([^()]+)\)", RegexOptions.Compiled);

    private readonly Dictionary<string, string> _output = [];
    private readonly Dictionary<string, TaskCompletionSource<string>> _outputCallback = [];
    private TestChildProcess _process;
    private IBrowserType _browserType;
    private IBrowser _browser;
    private string _wsEndpoint;
    private string _pendingOutput = string.Empty;
    private string _outputWhenExited;

    /// <param name="childProcess">The <c>childProcess</c> fixture.</param>
    /// <param name="browserType">The browser to launch.</param>
    /// <param name="channel">The <c>channel</c> fixture.</param>
    /// <param name="browserOptions">The <c>launchOptions</c> fixture, <c>playwright._defaultLaunchOptions</c> upstream.</param>
    /// <param name="remoteServerOptions">What the server does.</param>
    public async Task Start(ChildProcess childProcess, IBrowserType browserType, string channel, BrowserTypeLaunchOptions browserOptions, RemoteServerOptions remoteServerOptions = null)
    {
        remoteServerOptions ??= new();
        _browserType = browserType;
        // Copy options to prevent a large JSON string when launching subprocess.
        // Otherwise, we get `Error: spawn ENAMETOOLONG` on Windows.
        var launchOptions = new Dictionary<string, object>();
        AddIfSet(launchOptions, "args", browserOptions?.Args);
        AddIfSet(launchOptions, "headless", browserOptions?.Headless);
        AddIfSet(launchOptions, "channel", browserOptions?.Channel);
        AddIfSet(launchOptions, "executablePath", browserOptions?.ExecutablePath);
        launchOptions["handleSIGINT"] = true;
        launchOptions["handleSIGTERM"] = true;
        launchOptions["handleSIGHUP"] = true;
        if (remoteServerOptions.SharedBrowser)
        {
            launchOptions["_sharedBrowser"] = true;
        }
        AddIfSet(launchOptions, "artifactsDir", remoteServerOptions.ArtifactsDir);
        var options = new Dictionary<string, object> { ["browserTypeName"] = browserType.Name };
        AddIfSet(options, "channel", channel);
        options["launchOptions"] = launchOptions;
        AddIfSet(options, "stallOnClose", remoteServerOptions.StallOnClose);
        AddIfSet(options, "disconnectOnSIGHUP", remoteServerOptions.DisconnectOnSIGHUP);
        AddIfSet(options, "exitOnFile", remoteServerOptions.ExitOnFile);
        AddIfSet(options, "exitOnWarning", remoteServerOptions.ExitOnWarning);
        AddIfSet(options, "inCluster", remoteServerOptions.InCluster);
        AddIfSet(options, "url", remoteServerOptions.Url);
        AddIfSet(options, "startStopAndRunHttp", remoteServerOptions.StartStopAndRunHttp);
        AddIfSet(options, "sharedBrowser", remoteServerOptions.SharedBrowser);
        AddIfSet(options, "artifactsDir", remoteServerOptions.ArtifactsDir);
        _process = childProcess(new()
        {
            Command = [NodeProcess.ExecPath, Path.Combine(Env.UpstreamTestsDir, "config", "remote-server-impl.js"), JsonSerializer.Serialize(options)],
            Env = new() { ["NODE_OPTIONS"] = Environment.GetEnvironmentVariable("NODE_OPTIONS") },
            OnOutput = data =>
            {
                lock (_output)
                {
                    _pendingOutput += data;
                    Match match;
                    while ((match = _outputRegex.Match(_pendingOutput)).Success)
                    {
                        var key = match.Groups[1].Value;
                        var value = match.Groups[2].Value;
                        AddOutput(key, value);
                        _pendingOutput = _pendingOutput.Substring(match.Index + match.Length);
                    }
                }
            },
        });
        _ = FailOutputWhenExitedAsync();

        _wsEndpoint = await Out("wsEndpoint").ConfigureAwait(false);

        if (remoteServerOptions.Url != null)
        {
            _browser = await _browserType.ConnectAsync(_wsEndpoint).ConfigureAwait(false);
            var page = await _browser.NewPageAsync().ConfigureAwait(false);
            await page.GotoAsync(remoteServerOptions.Url).ConfigureAwait(false);
        }
    }

    /// <summary>Fails when the server exits without the key, where upstream waits until the test times out.</summary>
    public override Task<string> Out(string key)
    {
        lock (_output)
        {
            if (_output.TryGetValue(key, out var value))
            {
                return Task.FromResult(value);
            }
            if (_outputWhenExited != null)
            {
                return Task.FromException<string>(ExitedWithout(key));
            }
            if (!_outputCallback.TryGetValue(key, out var callback))
            {
                _outputCallback[key] = callback = new(TaskCreationOptions.RunContinuationsAsynchronously);
            }
            return callback.Task;
        }
    }

    public override string WsEndpoint() => _wsEndpoint;

    public override Process Child() => _process.Process;

    public override Task<int?> ChildExitCode() => _process.ExitCode;

    public override async Task<string> ChildSignal() => (await _process.Exited.ConfigureAwait(false)).Signal;

    public override async Task Close()
    {
        if (_browser != null)
        {
            await _browser.CloseAsync().ConfigureAwait(false);
            _browser = null;
        }
        _ = _process.Kill("SIGINT");
        using var killTimer = new CancellationTokenSource();
        _ = KillAfterAsync(30000, killTimer.Token);
        try
        {
            await ChildExitCode().ConfigureAwait(false);
        }
        finally
        {
            await killTimer.CancelAsync().ConfigureAwait(false);
        }
    }

    private static void AddIfSet(Dictionary<string, object> options, string name, object value)
    {
        if (value != null)
        {
            options[name] = value;
        }
    }

    private static void AddIfSet(Dictionary<string, object> options, string name, bool value)
    {
        if (value)
        {
            options[name] = true;
        }
    }

    private void AddOutput(string key, string value)
    {
        _output[key] = value;
        if (_outputCallback.Remove(key, out var callback))
        {
            callback.TrySetResult(value);
        }
    }

    private async Task KillAfterAsync(int timeout, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(timeout, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return;
        }
        _ = _process.Kill("SIGINT");
    }

    private async Task FailOutputWhenExitedAsync()
    {
        await _process.Exited.ConfigureAwait(false);
        var output = _process.Output;
        lock (_output)
        {
            _outputWhenExited = output;
            foreach (var (key, callback) in _outputCallback)
            {
                callback.TrySetException(ExitedWithout(key));
            }
            _outputCallback.Clear();
        }
    }

    private InvalidOperationException ExitedWithout(string key) =>
        new($"The remote server exited without ({key}=>...) in its output. Output:\n{_outputWhenExited}");
}
