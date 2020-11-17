namespace PlaywrightSharp
{
    /// <summary>
    /// See <seealso cref="IRequest.Timing"/>.
    /// </summary>
    public class ResourceTiming
    {
        /// <summary>
        /// Request start time in milliseconds elapsed since January 1, 1970 00:00:00 UTC.
        /// </summary>
        public decimal StartTime { get; set; }

        /// <summary>
        /// Time immediately before the browser starts the domain name lookup for the resource.
        /// The value is given in milliseconds relative to startTime, -1 if not available.
        /// </summary>
        public decimal DomainLookupStart { get; set; } = -1;

        /// <summary>
        /// Time immediately after the browser starts the domain name lookup for the resource.
        /// The value is given in milliseconds relative to startTime, -1 if not available.
        /// </summary>
        public decimal DomainLookupEnd { get; set; } = -1;

        /// <summary>
        /// Time immediately before the user agent starts establishing the connection to the server to retrieve the resource.
        /// The value is given in milliseconds relative to startTime, -1 if not available.
        /// </summary>
        public decimal ConnectStart { get; set; } = -1;

        /// <summary>
        /// Time immediately before the browser starts the handshake process to secure the current connection.
        /// The value is given in milliseconds relative to startTime, -1 if not available.
        /// </summary>
        public decimal SecureConnectionStart { get; set; } = -1;

        /// <summary>
        /// Time immediately before the user agent starts establishing the connection to the server to retrieve the resource.
        /// The value is given in milliseconds relative to startTime, -1 if not available.
        /// </summary>
        public decimal ConnectEnd { get; set; } = -1;

        /// <summary>
        /// Time immediately before the browser starts requesting the resource from the server, cache, or local resource.
        /// The value is given in milliseconds relative to startTime, -1 if not available.
        /// </summary>
        public decimal RequestStart { get; set; } = -1;

        /// <summary>
        /// Time immediately after the browser starts requesting the resource from the server, cache, or local resource.
        /// The value is given in milliseconds relative to startTime, -1 if not available.
        /// </summary>
        public decimal ResponseStart { get; set; } = -1;

        /// <summary>
        /// Time immediately after the browser receives the last byte of the resource or immediately before the transport connection is closed, whichever comes first.
        /// The value is given in milliseconds relative to startTime, -1 if not available.
        /// </summary>
        public decimal ResponseEnd { get; set; } = -1;
    }
}