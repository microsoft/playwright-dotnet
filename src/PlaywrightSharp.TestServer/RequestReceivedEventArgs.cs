using System;
using Microsoft.AspNetCore.Http;

namespace PlaywrightSharp.TestServer
{
    public class RequestReceivedEventArgs : EventArgs
    {
        public HttpRequest Request { get; set; }
    }
}
