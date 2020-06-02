namespace PlaywrightSharp.Messaging
{
    /// <summary>
    /// base class used by browser connections.
    /// </summary>
    internal interface IMessageTask
    {
        /// <summary>
        /// Gets the name of the method used in the task.
        /// </summary>
        string Method { get; set; }
    }
}
