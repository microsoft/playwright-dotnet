using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PlaywrightSharp
{
    internal class FrameManager
    {
        private readonly Page _page;

        public FrameManager(Page page)
        {
            _page = page;
        }

        internal IList<LifecycleWatcher> LifecycleWatchers { get; } = new List<LifecycleWatcher>();

        internal Frame MainFrame { get; set; }

        internal ConcurrentDictionary<string, Frame> Frames { get; } = new ConcurrentDictionary<string, Frame>();

        internal IFrame FrameAttached(string frameId, string parentFrameId)
        {
            Frames.TryGetValue(parentFrameId, out var parentFrame);

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

        internal void FrameCommittedNewDocumentNavigation(string frameId, string url, string name, string documentId, bool initial)
        {
            Frames.TryGetValue(frameId, out var frame);

            foreach (var child in frame.ChildFrames)
            {
                RemoveFramesRecursively(child);
            }

            frame.Url = url;
            frame.Name = name;
            frame.LastDocumentId = documentId;
            ClearFrameLifecycle(frame);
            ClearWebSockets(frame);

            if (!initial)
            {
                foreach (var watcher in LifecycleWatchers)
                {
                    watcher.OnCommittedNewDocumentNavigation(frame);
                }

                _page.OnFrameNavigated(frame);
            }
        }

        internal Task FrameCommittedNewDocumentNavigation(string id, string url, string v, object loaderId)
        {
            throw new NotImplementedException();
        }

        internal void FrameLifecycleEvent(string frameId, string e)
        {
            if (!Frames.TryGetValue(frameId, out var frame))
            {
                return;
            }

            frame.FiredLifecycleEvents.Add(e);

            foreach (var watcher in LifecycleWatchers)
            {
                watcher.OnLifecycleEvent(frame);
            }

            if (frame == MainFrame && e == "load")
            {
                _page.OnLoad();
            }

            if (frame == MainFrame && e == "domcontentloaded")
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

        private void ClearWebSockets(Frame frame)
        {
        }

        private void ClearFrameLifecycle(Frame frame)
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
            _page.OnFrameAttached(frame);
        }

        private void CollectFrames(Frame frame, List<Frame> frames)
        {
            frames.Add(frame);
            foreach (var subframe in frame.ChildFrames)
            {
                CollectFrames(subframe, frames);
            }
        }
    }
}
