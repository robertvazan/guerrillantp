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
    /// This class holds unmanaged resources (the socket) and callers are responsible
    /// for calling <see cref="M:GuerrillaNtp.NtpClient.Dispose" /> when they are done,
    /// perhaps by instantiating this class in <c>using</c> block.
    /// Most applications should just call <see cref="M:GuerrillaNtp.NtpClient.GetCorrectionOffset" />
    /// after instantiating this class. Method <see cref="M:GuerrillaNtp.NtpClient.Query" />
    /// can be used to obtain additional details stored in reply <see cref="T:GuerrillaNtp.NtpPacket" />.
    /// </remarks>
    public class NtpClient : IDisposable
    {
        readonly Socket socket;

        /// <summary>
        /// Gets or sets the timeout for SNTP queries.
        /// </summary>
        /// <value>
        /// Timeout for SNTP queries. Default is one second.
        /// </value>
        public TimeSpan Timeout
        {
            get { return TimeSpan.FromMilliseconds(socket.ReceiveTimeout); }
            set
            {
                if (value < TimeSpan.FromMilliseconds(1))
                    throw new ArgumentOutOfRangeException();
                socket.ReceiveTimeout = Convert.ToInt32(value.TotalMilliseconds);
            }
        }

        /// <summary>
        /// Creates new <see cref="T:GuerrillaNtp.NtpClient" /> from server endpoint.
        /// </summary>
        /// <param name="endpoint">Endpoint of the remote SNTP server.</param>
        public NtpClient(IPEndPoint endpoint)
        {
            socket = new Socket(endpoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            socket.ReceiveTimeout = 1000;
            socket.Connect(endpoint);
        }

        /// <summary>
        /// Creates new <see cref="T:GuerrillaNtp.NtpClient" /> from server's IP address and optional port.
        /// </summary>
        /// <param name="address">IP address of remote SNTP server</param>
        /// <param name="port">Port of remote SNTP server. Default is 123 (standard SNTP port).</param>
        public NtpClient(IPAddress address, int port = 123) : this(new IPEndPoint(address, port)) { }

        /// <summary>
        /// Releases all resources held by <see cref="T:GuerrillaNtp.NtpClient" />.
        /// </summary>
        /// <remarks>
        /// <see cref="T:GuerrillaNtp.NtpClient" /> holds reference to <see cref="T:System.Net.Sockets.Socket" />,
        /// which must be explicitly released to avoid memory leaks.
        /// </remarks>
        public void Dispose() { socket.Dispose(); }

        /// <summary>
        /// Queries the SNTP server and returns correction offset.
        /// </summary>
        /// <returns>
        /// Time that should be added to local time to synchronize it with SNTP server's time.
        /// </returns>
        /// <exception cref="T:GuerrillaNtp.NtpException">Thrown when the server responds with invalid reply packet.</exception>
        /// <exception cref="T:System.Net.Sockets.SocketException">
        /// Thrown when no reply is received before <see cref="P:GuerrillaNtp.NtpClient.Timeout" /> is reached
        /// or when there is an error communicating with the server.
        /// </exception>
        public TimeSpan GetCorrectionOffset() { return Query().CorrectionOffset; }

        /// <summary>
        /// Queries the SNTP server with configurable <see cref="T:GuerrillaNtp.NtpPacket" /> request.
        /// </summary>
        /// <param name="request">SNTP request packet to use when querying the network time server.</param>
        /// <returns>SNTP reply packet returned by the server.</returns>
        /// <remarks>
        /// Default constructor of <see cref="T:GuerrillaNtp.NtpPacket" /> creates valid request packet
        /// except for property <see cref="P:GuerrillaNtp.NtpPacket.TransmitTimestamp" /> that must be set explicitly.
        /// </remarks>
        /// <exception cref="T:GuerrillaNtp.NtpException">
        /// Thrown when the request packet is invalid or when the server responds with invalid reply packet.
        /// </exception>
        /// <exception cref="T:System.Net.Sockets.SocketException">
        /// Thrown when no reply is received before <see cref="P:GuerrillaNtp.NtpClient.Timeout" /> is reached
        /// or when there is an error communicating with the server.
        /// </exception>
        public NtpPacket Query(NtpPacket request)
        {
            request.ValidateRequest();
            socket.Send(request.Bytes);
            var response = new byte[160];
            int received = socket.Receive(response);
            var truncated = new byte[received];
            Array.Copy(response, truncated, received);
            NtpPacket reply = new NtpPacket(truncated) { DestinationTimestamp = DateTime.UtcNow };
            reply.ValidateReply(request);
            return reply;
        }

        /// <summary>
        /// Queries the SNTP server with default options.
        /// </summary>
        /// <returns>SNTP reply packet returned by the server.</returns>
        /// <exception cref="T:GuerrillaNtp.NtpException">Thrown when the server responds with invalid reply packet.</exception>
        /// <exception cref="T:System.Net.Sockets.SocketException">
        /// Thrown when no reply is received before <see cref="P:GuerrillaNtp.NtpClient.Timeout" /> is reached
        /// or when there is an error communicating with the server.
        /// </exception>
        public NtpPacket Query() { return Query(new NtpPacket() { TransmitTimestamp = DateTime.UtcNow }); }
    }
}