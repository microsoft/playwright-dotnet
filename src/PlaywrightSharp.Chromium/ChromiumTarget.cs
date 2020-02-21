namespace PlaywrightSharp.Chromium
{
    /// <inheritdoc cref="ITarget"/>
    public class ChromiumTarget : ITarget
    {
        /// <inheritdoc cref="ITarget"/>
        public string Url => null;

        /// <inheritdoc cref="ITarget"/>
        public TargetType Type => TargetType.BackgroundPage;

        internal bool IsInitialized { get; set; }
    }
}