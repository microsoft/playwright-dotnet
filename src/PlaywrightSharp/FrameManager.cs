using System;
using System.Collections.Concurrent;

namespace PlaywrightSharp
{
    internal class FrameManager
    {
        private readonly PageBase _page;
        private readonly ConcurrentDictionary<string, IFrame> _frames = new ConcurrentDictionary<string, IFrame>();

        public FrameManager(PageBase page)
        {
            _page = page;
        }

        internal IFrame MainFrame { get; set; }

        internal IFrame FrameAttached(string frameId, string parentFrameId)
        {
            _frames.TryGetValue(parentFrameId, out var parentFrame);

            if (parentFrame == null)
            {
                if (MainFrame != null)
                {
                    // Update frame id to retain frame identity on cross-process navigation.
                    _frames.TryRemove(MainFrame.Id, out _);
                    MainFrame.Id = frameId;
                }
                else
                {
                    MainFrame = new Frame(_page, frameId, parentFrame);
                }
                _frames.TryAdd(frameId, MainFrame);
                return MainFrame;
            }
            else
            {
                var frame = new Frame(_page, frameId, parentFrame);
                _frames.TryAdd(frameId, frame);
                _page.OnFrameAttached(frame);
                return frame;
            }
        }
    }
}