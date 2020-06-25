using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PlaywrightSharp.Server
{
    internal class LaunchProcessOptions
    {
        public string ExecutablePath { get; set; }

        public List<string> Args { get; set; }

        public Dictionary<string, string> Env { get; set; }

        public bool Pipe { get; set; }

        public TaskProgress Progress { get; set; }

        public List<string> TempDirectories { get; set; }

        public Func<Task> AttemptToGracefullyClose { get; set; }

        public Action<int> OnExit { get; set; }
    }
}
