using System.Runtime.Serialization;

namespace PlaywrightSharp
{
    /// <summary>
    /// Abort error codes.
    /// </summary>
    public static class RequestAbortErrorCode
    {
        /// <summary>
        /// An operation was aborted (due to user action).
        /// </summary>
        public const string Aborted = "aborted";

        /// <summary>
        /// Permission to access a resource, other than the network, was denied.
        /// </summary>
        public const string AccessDenied = "accessdenied";

        /// <summary>
        /// The IP address is unreachable. This usually means that there is no route to the specified host or network.
        /// </summary>
        public const string AddressUnreachable = "addressunreachable";

        /// <summary>
        /// The client chose to block the request.
        /// </summary>
        public const string BlockedByClient = "blockedbyclient";

        /// <summary>
        /// The request failed because the response was delivered along with requirements which are not met
        /// ('X-Frame-Options' and 'Content-Security-Policy' ancestor checks, for instance).
        /// </summary>
        public const string BlockedByResponse = "blockedbyresponse";

        /// <summary>
        /// A connection timed out as a result of not receiving an ACK for data sent.
        /// </summary>
        public const string ConnectionAborted = "connectionaborted";

        /// <summary>
        /// A connection was closed (corresponding to a TCP FIN).
        /// </summary>
        public const string ConnectionClosed = "connectionclosed";

        /// <summary>
        /// A connection attempt failed.
        /// </summary>
        public const string ConnectionFailed = "connectionfailed";

        /// <summary>
        /// A connection attempt was refused.
        /// </summary>
        public const string ConnectionRefused = "connectionrefused";

        /// <summary>
        ///  A connection was reset (corresponding to a TCP RST).
        /// </summary>
        public const string ConnectionReset = "connectionreset";

        /// <summary>
        /// The Internet connection has been lost.
        /// </summary>
        public const string InternetDisconnected = "internetdisconnected";

        /// <summary>
        /// The host name could not be resolved.
        /// </summary>
        public const string NameNotResolved = "namenotresolved";

        /// <summary>
        /// An operation timed out.
        /// </summary>
        public const string TimedOut = "timedout";

        /// <summary>
        ///  A generic failure occurred.
        /// </summary>
        public const string Failed = "failed";
    }
}
