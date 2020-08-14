using System;

namespace PlaywrightSharp
{
    /// <summary>
    /// <see cref="IPage.Response"/> arguments.
    /// </summary>
    public class ResponseEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the response.
        /// </summary>
        /// <value>The response.</value>
        public IResponse Response { get; set; }
    }
}
