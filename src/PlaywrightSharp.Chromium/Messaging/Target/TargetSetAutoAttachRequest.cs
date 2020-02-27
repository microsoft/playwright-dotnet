namespace PlaywrightSharp.Chromium.Messaging.Target
{
    internal class TargetSetAutoAttachRequest
    {
        public bool AutoAttach { get; set; }

        public bool WaitForDebuggerOnStart { get; set; }

        public bool Flatten { get; set; }
    }
}
