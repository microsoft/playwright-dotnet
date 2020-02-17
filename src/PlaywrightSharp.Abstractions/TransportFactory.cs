using System;
using System.Threading;
using System.Threading.Tasks;

namespace PlaywrightSharp
{
    /// <summary>
    /// Delegate for creation of <see cref="IConnectionTransport"/> instances.
    /// </summary>
    /// <param name="url">Browser URL.</param>
    /// <param name="options">Connection options.</param>
    /// <returns>A <see cref="Task{IConnectionTransport}"/> instance for the asynchronous socket create and connect operation.</returns>
    public delegate Task<IConnectionTransport> TransportFactory(Uri url, ConnectOptions options);
}
