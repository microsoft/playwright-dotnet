using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PlaywrightSharp.Helpers;

namespace PlaywrightSharp
{
    internal class LifecycleWatcher : IDisposable
    {
        private static readonly WaitUntilNavigation[] _defaultWaitUntil = { WaitUntilNavigation.Load };
        private static int _lifecycleCounter;

        private readonly Frame _frame;
        private readonly WaitUntilNavigation[] _expectedLifecycle;
        private readonly int _timeout;
        private readonly WaitForNavigationOptions _options;
        private readonly TaskCompletionSource<bool> _newDocumentNavigationTaskWrapper;
        private readonly TaskCompletionSource<bool> _sameDocumentNavigationTaskWrapper;
        private readonly TaskCompletionSource<bool> _lifecycleTaskWrapper;
        private readonly TaskCompletionSource<bool> _terminationTaskWrapper;
        private readonly CancellationToken _token;
        private readonly int _lifecycleId = Interlocked.Increment(ref _lifecycleCounter);
        private Request _navigationRequest;
        private bool _hasSameDocumentNavigation;
        private string _expectedDocumentId;
        private string _targetUrl;

        public LifecycleWatcher(Frame frame, NavigationOptions options, CancellationToken token = default)
        {
            _options = options != null ? options as WaitForNavigationOptions ?? new WaitForNavigationOptions(options) : new WaitForNavigationOptions();
            _expectedLifecycle = _options.WaitUntil ?? _defaultWaitUntil;
            _timeout = _options?.Timeout ?? frame.Page.DefaultNavigationTimeout;
            _frame = frame;
            _sameDocumentNavigationTaskWrapper = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            _lifecycleTaskWrapper = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            _newDocumentNavigationTaskWrapper = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            _terminationTaskWrapper = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            _frame.Page.FrameManager.LifecycleWatchers.Add(this);
            _token = token;
            _frame.Page.ClientDisconnected += (sender, args) => _terminationTaskWrapper.TrySetException(new PlaywrightSharpException("Client disconnected"));
            CheckLifecycleComplete();
        }

        public Task<Task> NavigationTask { get; internal set; }

        public Task<bool> SameDocumentNavigationTask => _sameDocumentNavigationTaskWrapper.Task;

        public Task<bool> NewDocumentNavigationTask => _newDocumentNavigationTaskWrapper.Task;

        public Task<Response> NavigationResponseTask => _navigationRequest?.FinalRequest?.WaitForFinished ?? Task.FromResult<Response>(null);

        public Task TimeoutOrTerminationTask => _terminationTaskWrapper.Task.WithTimeout(_timeout, cancellationToken: _token);

        public Task LifecycleTask => _lifecycleTaskWrapper.Task;

        internal int LifecycleId => _lifecycleId;

        /// <inheritdoc cref="IDisposable"/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        internal void SetExpectedDocumentId(string documentId, string url)
        {
            if (
                !string.IsNullOrEmpty(_options.Url) ||
                _options.UrlPredicate != null ||
                _options.UrlRegEx != null)
            {
                throw new PlaywrightSharpException("Should not have url match when expecting a particular navigation");
            }

            _expectedDocumentId = documentId;
            _targetUrl = url;

            if (_navigationRequest != null && _navigationRequest.DocumentId != documentId)
            {
                _navigationRequest = null;
            }
        }

        internal void OnFrameDetached(Frame frame)
        {
            if (_frame == frame)
            {
                Terminate(new PlaywrightSharpException("Navigating frame was detached"));
                return;
            }

            CheckLifecycleComplete();
        }

        internal void OnNavigatedWithinDocument(Frame frame)
        {
            if (frame != _frame)
            {
                return;
            }

            _hasSameDocumentNavigation = true;
            CheckLifecycleComplete();
        }

        internal void OnNavigationRequest(Frame frame, Request request)
        {
            if (frame != _frame || !UrlMatches(request.Url))
            {
                return;
            }

            if (_expectedDocumentId == null || _expectedDocumentId == request.DocumentId)
            {
                _navigationRequest = request;
                _expectedDocumentId = request.DocumentId;
                _targetUrl = request.Url;
            }
        }

        internal void OnCommittedNewDocumentNavigation(Frame frame)
        {
            if (frame == _frame && _expectedDocumentId != null && _navigationRequest != null && frame.LastDocumentId != _expectedDocumentId)
            {
                _terminationTaskWrapper.TrySetException(new PlaywrightSharpException($"Navigation to {_targetUrl} was canceled by another one"));
                return;
            }

            if (frame == _frame && _expectedDocumentId == null && UrlMatches(frame.Url))
            {
                _expectedDocumentId = frame.LastDocumentId;
                _targetUrl = frame.Url;
            }
        }

        internal void OnAbortedNewDocumentNavigation(Frame frame, string documentId, string errorText)
        {
            if (frame == _frame && documentId == _expectedDocumentId)
            {
                if (!string.IsNullOrEmpty(_targetUrl))
                {
                    _terminationTaskWrapper.TrySetException(new PlaywrightSharpException($"Navigation to {_targetUrl} failed: {errorText}"));
                }
                else
                {
                    _terminationTaskWrapper.TrySetException(new PlaywrightSharpException($"Navigation failed: {errorText}"));
                }
            }
        }

        internal void OnProvisionalLoadFailed(string documentId, string error) => OnAbortedNewDocumentNavigation(_frame, documentId, error);

        internal void OnLifecycleEvent(Frame frame) => CheckLifecycleComplete();

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                /*TODO Fix Concurrency issues**/
                try
                {
                    _frame?.Page?.FrameManager?.LifecycleWatchers?.Remove(this);
                }
                catch (ArgumentOutOfRangeException ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex);
                }

                _terminationTaskWrapper?.TrySetResult(false);
            }
        }

        private void OnClientDisconnected(object sender, EventArgs e)
            => Terminate(new PlaywrightSharpException("Navigation failed because browser has disconnected!"));

        private bool UrlMatches(string url)
        {
            if (
                string.IsNullOrEmpty(_options.Url) &&
                _options.UrlPredicate == null &&
                _options.UrlRegEx == null)
            {
                return true;
            }

            if (_options.Url == url)
            {
                return true;
            }

            if (_options.UrlRegEx?.IsMatch(url) == true)
            {
                return true;
            }

            if (_options.UrlPredicate?.Invoke(url) == true)
            {
                return true;
            }

            return false;
        }

        private void CheckLifecycleComplete()
        {
            // We expect navigation to commit.
            if (!CheckLifecycleRecursively(_frame, _expectedLifecycle))
            {
                return;
            }

            if (UrlMatches(_frame.Url))
            {
                _lifecycleTaskWrapper.TrySetResult(true);

                if (_hasSameDocumentNavigation)
                {
                    _sameDocumentNavigationTaskWrapper.TrySetResult(true);
                }
            }

            if (_frame.LastDocumentId == _expectedDocumentId)
            {
                _newDocumentNavigationTaskWrapper.TrySetResult(true);
            }
        }

        private void Terminate(PlaywrightSharpException ex) => _terminationTaskWrapper.TrySetException(ex);

        private bool CheckLifecycleRecursively(Frame frame, IEnumerable<WaitUntilNavigation> expectedLifecycle)
        {
            var expectedLifecycleArray = expectedLifecycle.ToArray();
            if (expectedLifecycleArray.Any(item => !frame.FiredLifecycleEvents.Contains(item)))
            {
                return false;
            }

            foreach (var child in frame.ChildFrames)
            {
                if (!CheckLifecycleRecursively(child, expectedLifecycleArray))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
