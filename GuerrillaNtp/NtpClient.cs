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
    /// Most applications should just call <see cref="M:GuerrillaNtp.NtpClient.GetCorrectionOffset" />
    /// after instantiating this class. Method <see cref="M:GuerrillaNtp.NtpClient.Query" />
    /// can be used to obtain additional details stored in reply <see cref="T:GuerrillaNtp.NtpPacket" />.
    /// </para>
    /// <para>
    /// This class holds unmanaged resources (the socket) and callers are responsible
    /// for calling <see cref="M:GuerrillaNtp.NtpClient.Dispose" /> when they are done,
    /// perhaps by instantiating this class in <c>using</c> block.
    /// </para>
    /// <para>
    /// It is application responsibility to be a good netizen,
    /// which most importantly means using reasonable polling intervals
    /// and exponential backoff when querying public NTP server.
    /// </para>
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
        /// <seealso cref="M:GuerrillaNtp.NtpClient.#ctor(System.Net.IPAddress,System.Int32)" />
        /// <seealso cref="M:GuerrillaNtp.NtpClient.Dispose" />
        public NtpClient(IPEndPoint endpoint)
        {
            socket = new Socket(endpoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            try
            {
                socket.ReceiveTimeout = 1000;
                socket.Connect(endpoint);
            }
            catch
            {
                socket.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Creates new <see cref="T:GuerrillaNtp.NtpClient" /> from server's IP address and optional port.
        /// </summary>
        /// <param name="address">IP address of remote SNTP server</param>
        /// <param name="port">Port of remote SNTP server. Default is 123 (standard NTP port).</param>
        /// <seealso cref="M:GuerrillaNtp.NtpClient.#ctor(System.Net.IPEndPoint)" />
        /// <seealso cref="M:GuerrillaNtp.NtpClient.Dispose" />
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
        /// <remarks>
        /// Use this method if you just want correction offset from the server.
        /// Call <see cref="M:GuerrillaNtp.NtpClient.Query" /> to obtain <see cref="T:GuerrillaNtp.NtpPacket" />
        /// with additional information besides <see cref="P:GuerrillaNtp.NtpPacket.CorrectionOffset" />.
        /// </remarks>
        /// <returns>
        /// Offset that should be added to local time to match server time.
        /// </returns>
        /// <exception cref="T:GuerrillaNtp.NtpException">Thrown when the server responds with invalid reply packet.</exception>
        /// <exception cref="T:System.Net.Sockets.SocketException">
        /// Thrown when no reply is received before <see cref="P:GuerrillaNtp.NtpClient.Timeout" /> is reached
        /// or when there is an error communicating with the server.
        /// </exception>
        /// <seealso cref="M:GuerrillaNtp.NtpClient.Query" />
        /// <seealso cref="P:GuerrillaNtp.NtpPacket.CorrectionOffset" />
        public TimeSpan GetCorrectionOffset() { return Query().CorrectionOffset; }

        /// <summary>
        /// Queries the SNTP server with configurable <see cref="T:GuerrillaNtp.NtpPacket" /> request.
        /// </summary>
        /// <param name="request">SNTP request packet to use when querying the network time server.</param>
        /// <returns>SNTP reply packet returned by the server.</returns>
        /// <remarks>
        /// <see cref="M:GuerrillaNtp.NtpPacket.#ctor" /> constructor
        /// creates valid request packet, which you can further customize.
        /// If you don't need any customization of the request packet, call <see cref="M:GuerrillaNtp.NtpClient.Query" /> instead.
        /// Returned <see cref="T:GuerrillaNtp.NtpPacket" /> contains correction offset in
        /// <see cref="P:GuerrillaNtp.NtpPacket.CorrectionOffset" /> property.
        /// </remarks>
        /// <exception cref="T:GuerrillaNtp.NtpException">
        /// Thrown when the request packet is invalid or when the server responds with invalid reply packet.
        /// </exception>
        /// <exception cref="T:System.Net.Sockets.SocketException">
        /// Thrown when no reply is received before <see cref="P:GuerrillaNtp.NtpClient.Timeout" /> is reached
        /// or when there is an error communicating with the server.
        /// </exception>
        /// <seealso cref="M:GuerrillaNtp.NtpClient.GetCorrectionOffset" />
        /// <seealso cref="P:GuerrillaNtp.NtpPacket.CorrectionOffset" />
        /// <seealso cref="M:GuerrillaNtp.NtpClient.Query" />
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
        /// <remarks>
        /// Use this method to obtain additional details from the returned <see cref="T:GuerrillaNtp.NtpPacket" />
        /// besides <see cref="P:GuerrillaNtp.NtpPacket.CorrectionOffset" />.
        /// If you just need the correction offset, call <see cref="M:GuerrillaNtp.NtpClient.GetCorrectionOffset" /> instead.
        /// You can customize request packed by calling <see cref="M:GuerrillaNtp.NtpClient.Query(GuerrillaNtp.NtpPacket)" />.
        /// </remarks>
        /// <returns>SNTP reply packet returned by the server.</returns>
        /// <exception cref="T:GuerrillaNtp.NtpException">Thrown when the server responds with invalid reply packet.</exception>
        /// <exception cref="T:System.Net.Sockets.SocketException">
        /// Thrown when no reply is received before <see cref="P:GuerrillaNtp.NtpClient.Timeout" /> is reached
        /// or when there is an error communicating with the server.
        /// </exception>
        /// <seealso cref="M:GuerrillaNtp.NtpClient.GetCorrectionOffset" />
        /// <seealso cref="P:GuerrillaNtp.NtpPacket.CorrectionOffset" />
        /// <seealso cref="M:GuerrillaNtp.NtpClient.Query(GuerrillaNtp.NtpPacket)" />
       public NtpPacket Query() { return Query(new NtpPacket()); }
    }
}