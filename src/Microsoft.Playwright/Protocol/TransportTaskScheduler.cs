using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Playwright.Protocol
{
    /// <summary>
    /// Delegate for scheduling of long-running transport tasks.
    /// </summary>
    /// <param name="func">Reader func.</param>
    /// <param name="cancellationToken">Cancellation token for the task to be scheduled.</param>
    public delegate void TransportTaskScheduler(Func<CancellationToken, Task> func, CancellationToken cancellationToken);
}
