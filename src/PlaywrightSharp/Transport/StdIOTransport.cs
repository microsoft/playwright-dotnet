using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace PlaywrightSharp.Transport
{
    internal class StdIOTransport : IConnectionTransport
    {
        private Process _process;

        internal StdIOTransport(Process process)
        {
            _process = process;
            _process.OutputDataReceived += (sender, args) =>
            {
                Debug.WriteLine($"◀ RECV {args.Data}");
                MessageReceived?.Invoke(this, new MessageReceivedEventArgs(args.Data));
            };

            _process.BeginOutputReadLine();
        }

        public event EventHandler<MessageReceivedEventArgs> MessageReceived;

        public Task SendAsync(string message)
        {
            Debug.WriteLine($"SEND ► {message}");
            return _process.StandardInput.WriteAsync(message);
        }
    }
}
