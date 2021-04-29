namespace Microsoft.Playwright
{
    /// <summary>
    /// <see cref="Cookie"/>.
    /// </summary>
    public partial class Cookie
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Cookie"/> class.
        /// </summary>
        public Cookie()
        {
            SameSite = SameSiteAttribute.None;
        }
    }
}
