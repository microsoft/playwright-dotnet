using System;

namespace PlaywrightSharp
{
    internal class LifecycleWatcher
    {
        private readonly Frame _frame;
        private readonly GoToOptions _options;
        private readonly bool _supportUrlMatch;

        public LifecycleWatcher(Frame frame, GoToOptions options, bool supportUrlMatch)
        {
            _frame = frame;
            _options = options;
            _supportUrlMatch = supportUrlMatch;
        }

        internal void OnCommittedNewDocumentNavigation(Frame frame)
        {
            throw new NotImplementedException();
        }
    }
}