namespace Microsoft.Playwright.Transport
{
    internal class MessageResponseArray : IMessageResponse
    {
        public IMessageResponse[] Items { get; set; }
    }
}
