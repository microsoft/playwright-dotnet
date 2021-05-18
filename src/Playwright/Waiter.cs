using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Playwright.Helpers;
using Microsoft.Playwright.Transport.Channels;

namespace Microsoft.Playwright
{
    internal class Waiter : IDisposable
    {
        private readonly List<string> _logs = new();
        private readonly List<Task> _failures = new();
        private readonly List<Action> _dispose = new();
        private readonly CancellationTokenSource _cts = new();
        private readonly string _waitId = Guid.NewGuid().ToString();
        private readonly ChannelBase _channel;

        private bool _disposed;
        private string _error;

        internal Waiter(ChannelBase channel, string apiName)
        {
            _channel = channel;
            var beforeArgs = new { info = new { apiName = apiName, waitId = _waitId, phase = "before" } };
            var connection = _channel.Connection;

            _ = connection.SendMessageToServerAsync(channel.Guid, "waitForEventInfo", beforeArgs, waitForResponse: false);
            _dispose.Add(() =>
            {
                var afterArgs = new { info = new { waitId = _waitId, phase = "after", error = _error } };

                _ = _channel.Connection.SendMessageToServerAsync(channel.Guid, "waitForEventInfo", afterArgs, waitForResponse: false);
            });
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                foreach (var dispose in _dispose)
                {
                    dispose();
                }

                _cts.Cancel();
                _cts.Dispose();
            }
        }

        internal void Log(string log)
        {
            _logs.Add(log);
            var logArgs = new { info = new { waitId = _waitId, phase = "log", message = log } };
            _ = _channel.Connection.SendMessageToServerAsync(_channel.Guid, "waitForEventInfo", logArgs);
        }

        internal void RejectOnEvent<T>(
            object eventSource,
            string e,
            PlaywrightException navigationException,
            Func<T, bool> predicate = null)
        {
            if (eventSource == null)
            {
                return;
            }

            var (task, dispose) = GetWaitForEventTask(eventSource, e, predicate);
            RejectOn(
                task.ContinueWith(_ => throw navigationException, _cts.Token, TaskContinuationOptions.RunContinuationsAsynchronously, TaskScheduler.Current),
                dispose);
        }

        internal void RejectOnTimeout(int? timeout, string message)
        {
            if (timeout == null)
            {
                return;
            }

#pragma warning disable CA2000 // Dispose objects before losing scope
            var cts = new CancellationTokenSource();
#pragma warning restore CA2000 // Dispose objects before losing scope
            RejectOn(
                new TaskCompletionSource<bool>().Task.WithTimeout(timeout.Value, _ => new TimeoutException(message), cts.Token),
                () => cts.Cancel());
        }

        internal Task<T> WaitForEventAsync<T>(object eventSource, string e, Func<T, bool> predicate)
        {
            var (task, dispose) = GetWaitForEventTask(eventSource, e, predicate);
            return WaitForPromiseAsync(task, dispose);
        }

        internal Task<object> WaitForEventAsync(object eventSource, string e)
        {
            var (task, dispose) = GetWaitForEventTask<object>(eventSource, e, null);
            return WaitForPromiseAsync(task, dispose);
        }

        internal (Task<T> Task, Action Dispose) GetWaitForEventTask<T>(object eventSource, string e, Func<T, bool> predicate)
        {
            var info = eventSource.GetType().GetEvent(e) ?? eventSource.GetType().BaseType.GetEvent(e);

            var eventTsc = new TaskCompletionSource<T>();
            void EventHandler(object sender, T e)
            {
                try
                {
                    if (predicate == null || predicate(e))
                    {
                        eventTsc.TrySetResult(e);
                    }
                    else
                    {
                        return;
                    }
                }
                catch (Exception ex)
                {
                    eventTsc.TrySetException(ex);
                }

                info.RemoveEventHandler(eventSource, (EventHandler<T>)EventHandler);
            }

            info.AddEventHandler(eventSource, (EventHandler<T>)EventHandler);
            return (eventTsc.Task, () => info.RemoveEventHandler(eventSource, (EventHandler<T>)EventHandler));
        }

        internal async Task<T> WaitForPromiseAsync<T>(Task<T> task, Action dispose = null)
        {
            try
            {
                var firstTask = await Task.WhenAny(_failures.Prepend(task)).ConfigureAwait(false);
                dispose?.Invoke();
                await firstTask.ConfigureAwait(false);
                return await task.ConfigureAwait(false);
            }
            catch (TimeoutException ex)
            {
                dispose?.Invoke();
                _error = ex.ToString();
                Dispose();
                throw new TimeoutException(ex.Message + FormatLogRecording(_logs), ex);
            }
            catch (Exception ex)
            {
                dispose?.Invoke();
                _error = ex.ToString();
                Dispose();
                throw new PlaywrightException(ex.Message + FormatLogRecording(_logs), ex);
            }
        }

        private string FormatLogRecording(List<string> logs)
        {
            if (logs.Count == 0)
            {
                return string.Empty;
            }

            const string header = " logs ";
            const int headerLength = 60;
            int leftLength = (headerLength - header.Length) / 2;
            int rightLength = headerLength - header.Length - leftLength;
            string log = string.Join("\n", logs);

            return $"\n{new string('=', leftLength)}{header}{new string('=', rightLength)}\n{log}\n{new string('=', headerLength)}";
        }

        private void RejectOn(Task task, Action dispose)
        {
            _failures.Add(task);
            if (dispose != null)
            {
                _dispose.Add(dispose);
            }
        }
    }
}
