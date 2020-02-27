namespace PlaywrightSharp.Chromium.Messaging.Target
{
    internal class TargetAttachToTargetResponse
    {
        public string SessionId { get; set; }

        public TargetInfo TargetInfo { get; set; }
    }
}
