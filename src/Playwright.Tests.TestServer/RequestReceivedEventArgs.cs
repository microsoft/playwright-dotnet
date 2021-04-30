using System;
using Microsoft.AspNetCore.Http;

namespace Microsoft.Playwright.Tests.TestServer
{
    public class RequestReceivedEventArgs : EventArgs
    {
        public HttpRequest Request { get; set; }
    }
}
