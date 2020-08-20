using System.Runtime.Serialization;

namespace PlaywrightSharp
{
    /// <summary>
    /// Abort error codes.
    /// </summary>
    public enum RequestAbortErrorCode
    {
        /// <summary>
        /// An operation was aborted (due to user action)
        /// </summary>
        Aborted,

        /// <summary>
        /// Permission to access a resource, other than the network, was denied
        /// </summary>
        [EnumMember(Value = "accessdenied")]
        AccessDenied,

        /// <summary>
        /// The IP address is unreachable. This usually means that there is no route to the specified host or network.
        /// </summary>
        [EnumMember(Value = "ddressunreachable")]
        AddressUnreachable,

        /// <summary>
        /// The client chose to block the request.
        /// </summary>
        [EnumMember(Value = "blockedbyclient")]
        BlockedByClient,

        /// <summary>
        /// The request failed because the response was delivered along with requirements which are not met
        /// ('X-Frame-Options' and 'Content-Security-Policy' ancestor checks, for instance).
        /// </summary>
        [EnumMember(Value = "blockedbyresponse")]
        BlockedByResponse,

        /// <summary>
        /// A connection timed out as a result of not receiving an ACK for data sent.
        /// </summary>
        [EnumMember(Value = "connectionaborted")]
        ConnectionAborted,

        /// <summary>
        /// A connection was closed (corresponding to a TCP FIN).
        /// </summary>
        [EnumMember(Value = "connectionclosed")]
        ConnectionClosed,

        /// <summary>
        /// A connection attempt failed.
        /// </summary>
        [EnumMember(Value = "connectionfailed")]
        ConnectionFailed,

        /// <summary>
        /// A connection attempt was refused.
        /// </summary>
        [EnumMember(Value = "connectionrefused")]
        ConnectionRefused,

        /// <summary>
        ///  A connection was reset (corresponding to a TCP RST).
        /// </summary>
        [EnumMember(Value = "connectionreset")]
        ConnectionReset,

        /// <summary>
        /// The Internet connection has been lost.
        /// </summary>
        [EnumMember(Value = "internetdisconnected")]
        InternetDisconnected,

        /// <summary>
        /// The host name could not be resolved.
        /// </summary>
        [EnumMember(Value = "namenotresolved")]
        NameNotResolved,

        /// <summary>
        /// An operation timed out.
        /// </summary>
        [EnumMember(Value = "timedout")]
        TimedOut,

        /// <summary>
        ///  A generic failure occurred.
        /// </summary>
        Failed,
    }
}
