namespace PlaywrightSharp.Chromium.Messaging.Target
{
    internal class TargetAttachToTargetRequest
    {
        public string TargetId { get; set; }

        public bool Flatten { get; set; }
    }
}
