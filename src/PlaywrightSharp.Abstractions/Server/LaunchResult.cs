using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace PlaywrightSharp.Server
{
    internal class LaunchResult
    {
        public Process Process { get; set; }

        public string WebSocketEndpoint { get; set; }

        public Func<Task> GracefullyCloseFunction { get; set; }

        public Func<Task> KillFunction { get; set; }
    }
}
