namespace Microsoft.Playwright
{
    internal static class Converters
    {
        internal static LoadState ToLoadState(this WaitUntilState waitUntilState)
            => waitUntilState switch
            {
                WaitUntilState.Undefined => LoadState.Undefined,
                WaitUntilState.Load => LoadState.Load,
                WaitUntilState.DOMContentLoaded => LoadState.DOMContentLoaded,
                WaitUntilState.NetworkIdle => LoadState.NetworkIdle,
                _ => LoadState.Undefined,
            };
    }
}
