namespace PlaywrightSharp
{
    /// <summary>
    ///  Event arguments used by target related events.
    /// </summary>
    /// <seealso cref="IBrowser.TargetChanged"/>
    /// <seealso cref="IBrowser.TargetCreated"/>
    /// <seealso cref="IBrowser.TargetDestroyed"/>
    public class TargetChangedArgs
    {
        /// <summary>
        /// Gets or sets the target.
        /// </summary>
        public ITarget Target { get; set; }
    }
}
