using System;
using Microsoft.AspNetCore.Http;

namespace Microsoft.Playwright.Test.TestServer
{
    public class RequestReceivedEventArgs : EventArgs
    {
        public HttpRequest Request { get; set; }
    }
}
