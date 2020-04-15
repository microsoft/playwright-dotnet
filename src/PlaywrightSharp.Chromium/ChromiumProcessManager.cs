using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using PlaywrightSharp.Helpers;

namespace PlaywrightSharp.Chromium
{
    internal class ChromiumProcessManager : IProcessManager
    {
        private static int _processCount;

        private readonly TempDirectory _tempUserDataDir;
        private readonly Func<Task> _attemptToGracefullyCloseFunc;
        private readonly Action<int> _onKill;
        private readonly TaskCompletionSource<string> _startCompletionSource = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<bool> _exitCompletionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        private State _currentState = State.Initial;

        public ChromiumProcessManager(
            string chromiumExecutable,
            List<string> chromiumArgs,
            TempDirectory tempUserDataDir,
            int timeout,
            Func<Task> attemptToGracefullyCloseFunc,
            Action<int> onKill)
        {
            _tempUserDataDir = tempUserDataDir;

            Timeout = timeout;
            _attemptToGracefullyCloseFunc = attemptToGracefullyCloseFunc;
            _onKill = onKill;
            Process = new Process
            {
                EnableRaisingEvents = true,
            };
            Process.StartInfo.UseShellExecute = false;
            Process.StartInfo.FileName = chromiumExecutable;
            Process.StartInfo.Arguments = string.Join(" ", chromiumArgs);
            Process.StartInfo.RedirectStandardError = true;
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="ChromiumProcessManager"/> class.
        /// </summary>
        ~ChromiumProcessManager()
        {
            Dispose(false);
        }

        public Process Process { get; }

        public int Timeout { get; }

        /// <summary>
        /// Gets Chromium endpoint.
        /// </summary>
        public string Endpoint => _startCompletionSource.Task.IsCompleted ? _startCompletionSource.Task.Result : null;

        /// <inheritdoc />
        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }

        /// <summary>
        /// Asynchronously starts Chromium process.
        /// </summary>
        /// <returns>A <see cref="Task"/> that completes when the start action is done.</returns>
        public Task StartAsync() => _currentState.StartAsync(this);

        /// <summary>
        /// Asynchronously waits for graceful Chromium process exit within a given timeout period.
        /// Kills the Chromium process if it has not exited within this period.
        /// </summary>
        /// <param name="timeout">The maximum waiting time for a graceful process exit.</param>
        /// <returns>A <see cref="Task"/> that completes when the exit action is done.</returns>
        public Task EnsureExitAsync(TimeSpan? timeout) => timeout.HasValue
            ? _currentState.ExitAsync(this, timeout.Value)
            : _currentState.KillAsync(this);

        /// <summary>
        /// Asynchronously kills Chromium process.
        /// </summary>
        /// <returns>A <see cref="Task"/> that completes when the kill action is done.</returns>
        public Task KillAsync() => _currentState.KillAsync(this);

        /// <summary>
        /// Disposes Chromium process and any temporary user directory.
        /// </summary>
        /// <param name="disposing">Indicates whether disposal was initiated by <see cref="Dispose()"/> operation.</param>
        protected virtual void Dispose(bool disposing) => _currentState.Dispose(this);

        /// <summary>
        /// Represents state machine for Chromium process instances. The happy path runs along the
        /// following state transitions: <see cref="Initial"/>
        /// -> <see cref="Starting"/>
        /// -> <see cref="Started"/>
        /// -> <see cref="Exiting"/>
        /// -> <see cref="Exited"/>.
        /// -> <see cref="Disposed"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This state machine implements the following state transitions:
        /// <code>
        /// State     Event              Target State Action
        /// ======== =================== ============ ==========================================================
        /// Initial  --StartAsync------> Starting     Start process and wait for endpoint
        /// Initial  --ExitAsync-------> Exited       Cleanup temp user data
        /// Initial  --KillAsync-------> Exited       Cleanup temp user data
        /// Initial  --Dispose---------> Disposed     Cleanup temp user data
        /// Starting --StartAsync------> Starting     -
        /// Starting --ExitAsync-------> Exiting      Wait for process exit
        /// Starting --KillAsync-------> Killing      Kill process
        /// Starting --Dispose---------> Disposed     Kill process; Cleanup temp user data;  throw ObjectDisposedException on outstanding async operations;
        /// Starting --endpoint ready--> Started      Complete StartAsync successfully; Log process start
        /// Starting --process exit----> Exited       Complete StartAsync with exception; Cleanup temp user data
        /// Started  --StartAsync------> Started      -
        /// Started  --EnsureExitAsync-> Exiting      Start exit timer; Log process exit
        /// Started  --KillAsync-------> Killing      Kill process; Log process exit
        /// Started  --Dispose---------> Disposed     Kill process; Log process exit; Cleanup temp user data; throw ObjectDisposedException on outstanding async operations;
        /// Started  --process exit----> Exited       Log process exit; Cleanup temp user data
        /// Exiting  --StartAsync------> Exiting      - (StartAsync throws InvalidOperationException)
        /// Exiting  --ExitAsync-------> Exiting      -
        /// Exiting  --KillAsync-------> Killing      Kill process
        /// Exiting  --Dispose---------> Disposed     Kill process; Cleanup temp user data; throw ObjectDisposedException on outstanding async operations;
        /// Exiting  --exit timeout----> Killing      Kill process
        /// Exiting  --process exit----> Exited       Cleanup temp user data; complete outstanding async operations;
        /// Killing  --StartAsync------> Killing      - (StartAsync throws InvalidOperationException)
        /// Killing  --KillAsync-------> Killing      -
        /// Killing  --Dispose---------> Disposed     Cleanup temp user data; throw ObjectDisposedException on outstanding async operations;
        /// Killing  --process exit----> Exited       Cleanup temp user data; complete outstanding async operations;
        /// Exited   --StartAsync------> Killing      - (StartAsync throws InvalidOperationException)
        /// Exited   --KillAsync-------> Exited       -
        /// Exited   --Dispose---------> Disposed     -
        /// Disposed --StartAsync------> Disposed     -
        /// Disposed --KillAsync-------> Disposed     -
        /// Disposed --Dispose---------> Disposed     -
        /// </code>
        /// </para>
        /// </remarks>
        private abstract class State
        {
            public static readonly State Initial = new InitialState();
            private static readonly StartingState Starting = new StartingState();
            private static readonly StartedState Started = new StartedState();
            private static readonly ExitingState Exiting = new ExitingState();
            private static readonly KillingState Killing = new KillingState();
            private static readonly ExitedState Exited = new ExitedState();
            private static readonly DisposedState Disposed = new DisposedState();

            public bool IsExiting => this == Killing || this == Exiting;

            public bool IsExited => this == Exited || this == Disposed;

            /// <summary>
            /// Handles process start request.
            /// </summary>
            /// <param name="p">The Chromium process.</param>
            /// <returns>A <see cref="Task"/> that completes when the start action is done.</returns>
            public virtual Task StartAsync(ChromiumProcessManager p) => Task.FromException(InvalidOperation("start"));

            /// <summary>
            /// Handles process exit request.
            /// </summary>
            /// <param name="p">The Chromium process.</param>
            /// <param name="timeout">The maximum waiting time for a graceful process exit.</param>
            /// <returns>A <see cref="Task"/> that completes when the exit action is done.</returns>
            public virtual Task ExitAsync(ChromiumProcessManager p, TimeSpan timeout) => Task.FromException(InvalidOperation("exit"));

            /// <summary>
            /// Handles process kill request.
            /// </summary>
            /// <param name="p">The Chromium process.</param>
            /// <returns>A <see cref="Task"/> that completes when the kill action is done.</returns>
            public virtual Task KillAsync(ChromiumProcessManager p) => Task.FromException(InvalidOperation("kill"));

            /// <summary>
            /// Handles wait for process exit request.
            /// </summary>
            /// <param name="p">The Chromium process.</param>
            /// <returns>A <see cref="Task"/> that completes when the wait finishes.</returns>
            public virtual Task WaitForExitAsync(ChromiumProcessManager p) => p._exitCompletionSource.Task;

            /// <summary>
            /// Handles disposal of process and temporary user directory.
            /// </summary>
            /// <param name="p">Process.</param>
            public virtual void Dispose(ChromiumProcessManager p) => Disposed.EnterFrom(p, this);

            public override string ToString()
            {
                string name = GetType().Name;
                return name.Substring(0, name.Length - "State".Length);
            }

            /// <summary>
            /// Attempts thread-safe transitions from a given state to this state.
            /// </summary>
            /// <param name="p">The Chromium process.</param>
            /// <param name="fromState">The state from which state transition takes place.</param>
            /// <returns>Returns <c>true</c> if transition is successful, or <c>false</c> if transition
            /// cannot be made because current state does not equal <paramref name="fromState"/>.</returns>
            protected bool TryEnter(ChromiumProcessManager p, State fromState)
            {
                if (Interlocked.CompareExchange(ref p._currentState, this, fromState) == fromState)
                {
                    fromState.Leave(p);
                    return true;
                }

                return false;
            }

            /// <summary>
            /// Notifies that state machine is about to transition to another state.
            /// </summary>
            /// <param name="p">The Chromium process.</param>
            protected virtual void Leave(ChromiumProcessManager p)
            {
            }

            /// <summary>
            /// Kills process if it is still alive.
            /// </summary>
            /// <param name="p">Process to kill.</param>
            private static void Kill(ChromiumProcessManager p)
            {
                try
                {
                    if (!p.Process.HasExited)
                    {
                        p.Process.Kill();
                    }
                }
                catch (InvalidOperationException)
                {
                    // Ignore
                }
            }

            private Exception InvalidOperation(string operationName)
                => new InvalidOperationException($"Cannot {operationName} in state {this}");

            private class InitialState : State
            {
                public override Task StartAsync(ChromiumProcessManager p) => Starting.EnterFromAsync(p, this);

                public override Task ExitAsync(ChromiumProcessManager p, TimeSpan timeout)
                {
                    Exited.EnterFrom(p, this);
                    return Task.CompletedTask;
                }

                public override Task KillAsync(ChromiumProcessManager p)
                {
                    Exited.EnterFrom(p, this);
                    return Task.CompletedTask;
                }

                public override Task WaitForExitAsync(ChromiumProcessManager p) => Task.FromException(InvalidOperation("wait for exit"));
            }

            private class StartingState : State
            {
                public Task EnterFromAsync(ChromiumProcessManager p, State fromState)
                {
                    if (!TryEnter(p, fromState))
                    {
                        // Delegate StartAsync to current state, because it has already changed since
                        // transition to this state was initiated.
                        return p._currentState.StartAsync(p);
                    }

                    return StartCoreAsync(p);
                }

                public override Task StartAsync(ChromiumProcessManager p) => p._startCompletionSource.Task;

                public override Task ExitAsync(ChromiumProcessManager p, TimeSpan timeout) => Exiting.EnterFromAsync(p, this, timeout);

                public override Task KillAsync(ChromiumProcessManager p) => Killing.EnterFromAsync(p, this);

                public override void Dispose(ChromiumProcessManager p)
                {
                    p._startCompletionSource.TrySetException(new ObjectDisposedException(p.ToString()));
                    base.Dispose(p);
                }

                private async Task StartCoreAsync(ChromiumProcessManager p)
                {
                    var output = new StringBuilder();

                    void OnProcessDataReceivedWhileStarting(object sender, DataReceivedEventArgs e)
                    {
                        if (e.Data != null)
                        {
                            output.AppendLine(e.Data);
                            var match = Regex.Match(e.Data, "^DevTools listening on (ws:\\/\\/.*)");
                            if (match.Success)
                            {
                                p._startCompletionSource.TrySetResult(match.Groups[1].Value);
                            }
                        }
                    }

                    void OnProcessExitedWhileStarting(object sender, EventArgs e)
                        => p._startCompletionSource.TrySetException(new MessageException($"Failed to launch Chromium! {output}"));
                    void OnProcessExited(object sender, EventArgs e) => Exited.EnterFrom(p, p._currentState);

                    p.Process.ErrorDataReceived += OnProcessDataReceivedWhileStarting;
                    p.Process.Exited += OnProcessExitedWhileStarting;
                    p.Process.Exited += OnProcessExited;
                    CancellationTokenSource cts = null;
                    try
                    {
                        p.Process.Start();
                        await Started.EnterFromAsync(p, this).ConfigureAwait(false);

                        p.Process.BeginErrorReadLine();

                        int timeout = p.Timeout;
                        if (timeout > 0)
                        {
                            cts = new CancellationTokenSource(timeout);
                            cts.Token.Register(() => p._startCompletionSource.TrySetException(
                                new MessageException($"Timed out after {timeout} ms while trying to connect to Chromium!")));
                        }

                        try
                        {
                            await p._startCompletionSource.Task.ConfigureAwait(false);
                            await Started.EnterFromAsync(p, this).ConfigureAwait(false);
                        }
                        catch
                        {
                            await Killing.EnterFromAsync(p, this).ConfigureAwait(false);
                            throw;
                        }
                    }
                    finally
                    {
                        cts?.Dispose();
                        p.Process.Exited -= OnProcessExitedWhileStarting;
                        p.Process.ErrorDataReceived -= OnProcessDataReceivedWhileStarting;
                    }
                }
            }

            private class StartedState : State
            {
                public Task EnterFromAsync(ChromiumProcessManager p, State fromState)
                {
                    if (TryEnter(p, fromState))
                    {
                        // Process has not exited or been killed since transition to this state was initiated
                        LogProcessCount(p, Interlocked.Increment(ref _processCount));
                    }

                    return Task.CompletedTask;
                }

                public override Task StartAsync(ChromiumProcessManager p) => Task.CompletedTask;

                public override Task ExitAsync(ChromiumProcessManager p, TimeSpan timeout) => Exiting.EnterFromAsync(p, this, timeout);

                public override Task KillAsync(ChromiumProcessManager p) => Killing.EnterFromAsync(p, this);

                protected override void Leave(ChromiumProcessManager p)
                    => LogProcessCount(p, Interlocked.Decrement(ref _processCount));

                private static void LogProcessCount(ChromiumProcessManager p, int processCount)
                {
                    // TODO Add logger
                    Console.WriteLine(p.ToString() + processCount);
                    /*
                    try
                    {
                        // p._logger?.LogInformation("Process Count: {ProcessCount}", processCount);
                    }
                    catch
                    {
                        // Prevent logging exception from causing havoc
                    }*/
                }
            }

            private class ExitingState : State
            {
                public Task EnterFromAsync(ChromiumProcessManager p, State fromState, TimeSpan timeout)
                    => !TryEnter(p, fromState) ? p._currentState.ExitAsync(p, timeout) : ExitAsync(p, timeout);

                public override async Task ExitAsync(ChromiumProcessManager p, TimeSpan timeout)
                {
                    var waitForExitTask = WaitForExitAsync(p);
                    await waitForExitTask.WithTimeout(
                        async () =>
                        {
                            await Killing.EnterFromAsync(p, this).ConfigureAwait(false);
                            await waitForExitTask.ConfigureAwait(false);
                        },
                        timeout,
                        CancellationToken.None).ConfigureAwait(false);
                }

                public override Task KillAsync(ChromiumProcessManager p) => Killing.EnterFromAsync(p, this);
            }

            private class KillingState : State
            {
                public async Task EnterFromAsync(ChromiumProcessManager p, State fromState)
                {
                    if (!TryEnter(p, fromState))
                    {
                        // Delegate KillAsync to current state, because it has already changed since
                        // transition to this state was initiated.
                        await p._currentState.KillAsync(p).ConfigureAwait(false);
                    }

                    try
                    {
                        await p._attemptToGracefullyCloseFunc().ConfigureAwait(false);
                        if (!p.Process.HasExited)
                        {
                            p.Process.Kill();
                        }
                    }
                    catch (InvalidOperationException)
                    {
                        // Ignore
                        return;
                    }

                    await WaitForExitAsync(p).ConfigureAwait(false);
                }

                public override Task ExitAsync(ChromiumProcessManager p, TimeSpan timeout) => WaitForExitAsync(p);

                public override Task KillAsync(ChromiumProcessManager p) => WaitForExitAsync(p);
            }

            private class ExitedState : State
            {
                public void EnterFrom(ChromiumProcessManager p, State fromState)
                {
                    while (!TryEnter(p, fromState))
                    {
                        // Current state has changed since transition to this state was requested.
                        // Therefore retry transition to this state from the current state. This ensures
                        // that Leave() operation of current state is properly called.
                        fromState = p._currentState;
                        if (fromState == this)
                        {
                            return;
                        }
                    }

                    p._onKill(p.Process?.ExitCode ?? 0);
                    p._exitCompletionSource.TrySetResult(true);
                    p._tempUserDataDir?.Dispose();
                }

                public override Task ExitAsync(ChromiumProcessManager p, TimeSpan timeout) => Task.CompletedTask;

                public override Task KillAsync(ChromiumProcessManager p) => Task.CompletedTask;

                public override Task WaitForExitAsync(ChromiumProcessManager p) => Task.CompletedTask;
            }

            private class DisposedState : State
            {
                public void EnterFrom(ChromiumProcessManager p, State fromState)
                {
                    if (!TryEnter(p, fromState))
                    {
                        // Delegate Dispose to current state, because it has already changed since
                        // transition to this state was initiated.
                        p._currentState.Dispose(p);
                    }
                    else if (fromState != Exited)
                    {
                        Kill(p);

                        p._exitCompletionSource.TrySetException(new ObjectDisposedException(p.ToString()));
                        p._tempUserDataDir?.Dispose();
                    }
                }

                public override Task StartAsync(ChromiumProcessManager p) => throw new ObjectDisposedException(p.ToString());

                public override Task ExitAsync(ChromiumProcessManager p, TimeSpan timeout) => throw new ObjectDisposedException(p.ToString());

                public override Task KillAsync(ChromiumProcessManager p) => throw new ObjectDisposedException(p.ToString());

                public override void Dispose(ChromiumProcessManager p)
                {
                    // Nothing to do
                }
            }
        }
    }
}
