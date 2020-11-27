namespace PlaywrightSharp.Har
{
    /// <summary>
    /// HAR Cache.
    /// </summary>
    public class HarCache
    {
        /// <summary>
        /// After request state.
        /// </summary>
        public HarCacheState BeforeRequest { get; set; }

        /// <summary>
        /// After request state.
        /// </summary>
        public HarCacheState AfterRequest { get; set; }
    }
}
