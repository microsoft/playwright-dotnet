namespace Microsoft.Playwright
{
    public class BindingSource
    {
        /// <summary>
        /// Browser Context.
        /// </summary>
        public IBrowserContext Context { get; set; }

        /// <summary>
        /// Page.
        /// </summary>
        public IPage Page { get; set; }

        /// <summary>
        /// Frame.
        /// </summary>
        public IFrame Frame { get; set; }
    }
}
