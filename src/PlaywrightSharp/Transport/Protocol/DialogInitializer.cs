namespace PlaywrightSharp.Transport.Protocol
{
    internal class DialogInitializer
    {
        public DialogType Type { get; set; }

        public string DefaultValue { get; set; }

        public string Message { get; set; }
    }
}
