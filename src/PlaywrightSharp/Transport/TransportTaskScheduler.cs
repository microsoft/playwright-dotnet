using System;
using System.Threading;
using System.Threading.Tasks;

namespace PlaywrightSharp.Transport
{
    /// <summary>
    /// Delegate for scheduling of long-running transport tasks.
    /// </summary>
    /// <param name="action">Reader action.</param>
    /// <param name="cancellationToken">Cancellation token for the task to be scheduled.</param>
    public delegate void TransportTaskScheduler(Action action, CancellationToken cancellationToken);
}