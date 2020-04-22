using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PlaywrightSharp.Helpers;

namespace PlaywrightSharp
{
    internal class FrameManager
    {
        private readonly Page _page;

        public FrameManager(Page page)
        {
            _page = page;
        }

        internal ConcurrentDictionary<string, Action> ConsoleMessageTags { get; } = new ConcurrentDictionary<string, Action>();

        internal List<LifecycleWatcher> LifecycleWatchers { get; } = new List<LifecycleWatcher>();

        internal Frame MainFrame { get; set; }

        internal ConcurrentDictionary<string, Frame> Frames { get; } = new ConcurrentDictionary<string, Frame>();

        internal IFrame FrameAttached(string frameId, string parentFrameId)
        {
            Frame parentFrame = null;
            if (parentFrameId != null)
            {
                Frames.TryGetValue(parentFrameId, out parentFrame);
            }

            if (parentFrame == null)
            {
                if (MainFrame != null)
                {
                    // Update frame id to retain frame identity on cross-process navigation.
                    Frames.TryRemove(MainFrame.Id, out _);
                    MainFrame.Id = frameId;
                }
                else
                {
                    MainFrame = new Frame(_page, frameId, parentFrame);
                }

                Frames.TryAdd(frameId, MainFrame);
                return MainFrame;
            }
            else
            {
                var frame = new Frame(_page, frameId, parentFrame);
                Frames.TryAdd(frameId, frame);
                _page.OnFrameAttached(frame);
                return frame;
            }
        }

        internal void FrameDetached(string frameId)
        {
            if (frameId != null && Frames.TryGetValue(frameId, out var frame))
            {
                RemoveFramesRecursively(frame);
            }
        }

        internal void FrameCommittedNewDocumentNavigation(string frameId, string url, string name, string documentId, bool initial)
        {
            if (!Frames.TryGetValue(frameId, out var frame))
            {
                return;
            }

            while (frame.ChildFrames.Count > 0)
            {
                RemoveFramesRecursively(frame.ChildFrames[0]);
            }

            frame.Url = url;
            frame.Name = name;
            frame.LastDocumentId = documentId;
            ClearFrameLifecycle(frame);
            ClearWebSockets(frame);

            if (!initial)
            {
                foreach (var watcher in LifecycleWatchers.ToArray())
                {
                    watcher.OnCommittedNewDocumentNavigation(frame);
                }

                _page.OnFrameNavigated(frame);
            }
        }

        internal void FrameCommittedSameDocumentNavigation(string frameId, string url)
        {
            if (!Frames.TryGetValue(frameId, out var frame))
            {
                return;
            }

            frame.Url = url;
            foreach (var watcher in LifecycleWatchers)
            {
                watcher.OnNavigatedWithinDocument(frame);
            }

            _page.OnFrameNavigated(frame);
        }

        internal void FrameLifecycleEvent(string frameId, WaitUntilNavigation name)
        {
            if (!Frames.TryGetValue(frameId, out var frame))
            {
                return;
            }

            frame.FiredLifecycleEvents.Add(name);

            foreach (var watcher in LifecycleWatchers.ToArray())
            {
                watcher.OnLifecycleEvent(frame);
            }

            if (frame == MainFrame && name == WaitUntilNavigation.Load)
            {
                _page.OnLoad();
            }

            if (frame == MainFrame && name == WaitUntilNavigation.DOMContentLoaded)
            {
                _page.OnDOMContentLoaded();
            }
        }

        internal IFrame[] GetFrames()
        {
            List<Frame> frames = new List<Frame>();
            CollectFrames(MainFrame, frames);
            return frames.ToArray();
        }

        internal bool InterceptConsoleMessage(ConsoleMessage message)
        {
            if (message.Type != ConsoleType.Debug)
            {
                return false;
            }

            string tag = message.GetText();
            if (ConsoleMessageTags.TryRemove(tag, out var handler))
            {
                handler();
                return true;
            }

            return false;
        }

        internal void ClearFrameLifecycle(Frame frame)
        {
            frame.FiredLifecycleEvents.Clear();

            // Keep the current navigation request if any.
            frame.InflightRequests = frame.InflightRequests.FindAll(request => request.DocumentId == frame.LastDocumentId);
            StopNetworkIdleTimer(frame, WaitUntilNavigation.Networkidle0);
            if (frame.InflightRequests.Count == 0)
            {
                StartNetworkIdleTimer(frame, WaitUntilNavigation.Networkidle0);
            }

            StopNetworkIdleTimer(frame, WaitUntilNavigation.Networkidle2);
            if (frame.InflightRequests.Count <= 2)
            {
                StartNetworkIdleTimer(frame, WaitUntilNavigation.Networkidle2);
            }
        }

        internal void RequestStarted(Request request)
        {
            InflightRequestStarted(request);
            var frame = request.Frame;
            if (!string.IsNullOrEmpty(request.DocumentId) && frame != null && request.RedirectChain.Count == 0)
            {
                foreach (var watcher in LifecycleWatchers.ToArray())
                {
                    watcher.OnNavigationRequest(frame, request);
                }
            }

            if (!request.IsFavicon)
            {
                _page.OnRequest(request);
            }
        }

        internal void RequestReceivedResponse(Response response)
        {
            if (!response.Request.IsFavicon)
            {
                _page.OnResponse(response);
            }
        }

        internal void RequestFinished(Request request)
        {
            InflightRequestFinished(request);
            if (!request.IsFavicon)
            {
                _page.OnRequestFinished(request);
            }
        }

        internal void RequestFailed(Request request, bool canceled)
        {
            InflightRequestFinished(request);
            var frame = request.Frame;
            if (!string.IsNullOrEmpty(request.DocumentId) && frame != null)
            {
                bool isCurrentDocument = frame.LastDocumentId == request.DocumentId;
                if (!isCurrentDocument)
                {
                    string errorText = request.Failure;
                    if (canceled)
                    {
                        errorText += "; maybe frame was detached?";
                    }

                    foreach (var watcher in LifecycleWatchers.ToArray())
                    {
                        watcher.OnAbortedNewDocumentNavigation(frame, request.DocumentId, errorText);
                    }
                }
            }

            if (!request.IsFavicon)
            {
                _page.OnRequestFailed(request);
            }
        }

        private void StartNetworkIdleTimer(Frame frame, WaitUntilNavigation lifecycleEvent)
        {
            if (frame.FiredLifecycleEvents.Contains(lifecycleEvent))
            {
                return;
            }

            frame.NetworkIdleTimers[lifecycleEvent] = NetworkIdleTimer();
            CancellationTokenSource NetworkIdleTimer()
            {
                var cts = new CancellationTokenSource();
                _ = Task.Delay(500, cts.Token).ContinueWith(
                    t => FrameLifecycleEvent(frame.Id, lifecycleEvent),
                    CancellationToken.None,
                    TaskContinuationOptions.NotOnCanceled,
                    TaskScheduler.Current);
                return cts;
            }
        }

        private void StopNetworkIdleTimer(Frame frame, WaitUntilNavigation lifecycleEvent)
        {
            if (frame.NetworkIdleTimers.TryRemove(lifecycleEvent, out var cts))
            {
                cts.Cancel();
                cts.Dispose();
            }
        }

        private void ClearWebSockets(Frame frame)
        {
        }

        private void RemoveFramesRecursively(Frame frame)
        {
            while (frame.ChildFrames.Count > 0)
            {
                RemoveFramesRecursively(frame.ChildFrames[0]);
            }

            frame.OnDetached();
            Frames.TryRemove(frame.Id, out _);
            foreach (var watcher in LifecycleWatchers)
            {
                watcher.OnFrameDetached(frame);
            }

            _page.OnFrameDetached(frame);
        }

        private void CollectFrames(Frame frame, List<Frame> frames)
        {
            frames.Add(frame);
            foreach (var subframe in frame.ChildFrames)
            {
                CollectFrames(subframe, frames);
            }
        }

        private void InflightRequestStarted(Request request)
        {
            var frame = request.Frame;
            if (frame == null || request.IsFavicon)
            {
                return;
            }

            frame.InflightRequests.Add(request);
            if (frame.InflightRequests.Count == 1)
            {
                StopNetworkIdleTimer(frame, WaitUntilNavigation.Networkidle0);
            }

            if (frame.InflightRequests.Count == 3)
            {
                StopNetworkIdleTimer(frame, WaitUntilNavigation.Networkidle2);
            }
        }

        private void InflightRequestFinished(Request request)
        {
            var frame = request.Frame;
            if (frame == null || request.IsFavicon)
            {
                return;
            }

            if (!frame.InflightRequests.Remove(request))
            {
                return;
            }

            if (frame.InflightRequests.Count == 0)
            {
                StopNetworkIdleTimer(frame, WaitUntilNavigation.Networkidle0);
            }

            if (frame.InflightRequests.Count == 2)
            {
                StopNetworkIdleTimer(frame, WaitUntilNavigation.Networkidle2);
            }
        }
    }
}
