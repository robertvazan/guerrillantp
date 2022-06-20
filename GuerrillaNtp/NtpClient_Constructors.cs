// Part of GuerrillaNtp: https://guerrillantp.machinezoo.com
using System;
using System.Net;
using System.Net.Sockets;

namespace GuerrillaNtp
{
    /// <summary>
    /// Represents UDP socket used to communicate with RFC4330-compliant SNTP/NTP server.
    /// </summary>
    /// <remarks>
    /// <para>
    /// See <a href="https://guerrillantp.machinezoo.com/">project homepage</a> for guidance on how to use GuerrillaNtp.
    /// Most applications should just call <see cref="GetCorrectionOffset" />
    /// after instantiating this class. Method <see cref="GetCorrectionResponse()" />
    /// can be used to obtain additional details stored in reply <see cref="NtpPacket" />.
    /// </para>
    /// <para>
    /// It is application responsibility to be a good netizen,
    /// which most importantly means using reasonable polling intervals
    /// and exponential backoff when querying public NTP server.
    /// </para>
    /// </remarks>
    public partial class NtpClient
    {
        /// <summary>
        /// Gets or sets the timeout for SNTP queries.
        /// </summary>
        /// <value>
        /// Timeout for SNTP queries. Default is one second.
        /// </value>
        public TimeSpan Timeout { get; set; }

        readonly EndPoint endpoint;

        Socket GetSocket()
        {
            var socket = new Socket(SocketType.Dgram, ProtocolType.Udp)
            {
                ReceiveTimeout = Convert.ToInt32(Timeout.TotalMilliseconds),
            };

            return socket;
        }

        /// <summary>
        /// Creates a new <see cref="NtpClient"/> using <see cref="DefaultEndpoint"/>, <see cref="DefaultPort"/>, and <see cref="DefaultTimeout"/>.
        /// </summary>
        public NtpClient()
        {
            this.endpoint = new DnsEndPoint(DefaultEndpoint, DefaultPort);
            this.Timeout = DefaultTimeout;
        }

        /// <inheritdoc cref="NtpClient(string, TimeSpan?, int?)"/>
        public NtpClient(IPAddress endpoint, TimeSpan? timeout = default, int? port = default)
        {
            this.endpoint = new IPEndPoint(endpoint, port ?? DefaultPort);
            this.Timeout = timeout ?? DefaultTimeout;
        }

        /// <inheritdoc cref="NtpClient(string, TimeSpan?, int?)"/>
        public NtpClient(EndPoint endpoint, TimeSpan? timeout = default)
        {
            this.endpoint = endpoint;
            this.Timeout = timeout ?? DefaultTimeout;
        }

        /// <summary>
        /// Create a new <see cref="NtpClient"/> using defaults for any unspecified values.
        /// </summary>
        /// <param name="endpoint">The NTP server</param>
        /// <param name="timeout">The amount of time to wait for a reply</param>
        /// <param name="port">The NTP port</param>
        public NtpClient(string endpoint, TimeSpan? timeout = default, int? port = default)
        {
            this.endpoint = new DnsEndPoint(endpoint, port ?? DefaultPort);
            this.Timeout = timeout ?? DefaultTimeout;
        }
    }
}
