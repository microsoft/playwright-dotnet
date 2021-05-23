using System;

namespace Microsoft.Playwright
{
    internal class WorkerEventArgs : EventArgs
    {
        public WorkerEventArgs(IWorker worker) => Worker = worker;

        public IWorker Worker { get; set; }
    }
}
