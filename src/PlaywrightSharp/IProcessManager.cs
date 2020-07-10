using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace PlaywrightSharp
{
    /// <summary>
    /// Manage the browser process lifecycle.
    /// </summary>
    internal interface IProcessManager : IDisposable
    {
        /// <summary>
        /// Browser process.
        /// </summary>
        Process Process { get; }

        /// <summary>
        /// Kills the browser process.
        /// </summary>
        /// <returns>A <see cref="Task"/> that completes when the process was killed.</returns>
        Task KillAsync();
    }
}
