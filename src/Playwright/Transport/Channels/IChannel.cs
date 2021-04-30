namespace Microsoft.Playwright.Transport.Channels
{
    /// <summary>
    /// Channel interface.
    /// </summary>
    /// <typeparam name="T">Chanel object type.</typeparam>
    internal interface IChannel<T>
        where T : ChannelOwnerBase, IChannelOwner<T>
    {
        /// <summary>
        /// Channel object.
        /// </summary>
        T Object { get; set; }
    }
}
