// Part of GuerrillaNtp: https://guerrillantp.machinezoo.com
using System;
using System.Net;
using System.Net.Sockets;

namespace GuerrillaNtp
{
    /// <summary>
    /// Represents a client used to connect to a network time server
    /// </summary>
    public class NtpClient : IDisposable
    {
        private readonly Socket socket;

        /// <summary>
        /// Gets or sets timeout for NTP queries
        /// </summary>
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
        /// Creates new NtpClient from server endpoint
        /// </summary>
        /// <param name="endpoint">Endpoint of the remote NTP server</param>
        public NtpClient(IPEndPoint endpoint)
        {
            socket = new Socket(endpoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
            socket.ReceiveTimeout = 1000;
            socket.Connect(endpoint);
        }

        /// <summary>
        /// Creates new NtpClient from server's IP address and optional port
        /// </summary>
        /// <param name="address">IP address of remote NTP server</param>
        /// <param name="port">Port of remote NTP server. Default is 123.</param>
        public NtpClient(IPAddress address, int port = 123) : this(new IPEndPoint(address, port)) { }

        /// <summary>
        /// Releases all resources held by NtpClient
        /// </summary>
        public void Dispose() { socket.Dispose(); }

        /// <summary>
        /// Queries the NTP server and returns correction offset
        /// </summary>
        /// <returns>
        /// Time that should be added to local time to synchronize it with NTP server
        /// </returns>
        public TimeSpan GetCorrectionOffset() { return Query().CorrectionOffset; }

        /// <summary>
        /// Queries NTP server with configurable NTP packet
        /// </summary>
        /// <param name="request">NTP packet to use when querying the network time server </param>
        /// <returns>The response from the NTP server</returns>
        public NtpPacket Query(NtpPacket request)
        {
            var responseBuffer = new byte[request.Bytes.Length];

            socket.Send(request.Bytes);
            socket.Receive(responseBuffer);

            return new NtpPacket(responseBuffer) { DestinationTimestamp = DateTime.UtcNow };
        }

        /// <summary>
        /// Queries NTP server with default options
        /// </summary>
        /// <returns>NTP packet returned from the server</returns>
        public NtpPacket Query() { return Query(new NtpPacket() { TransmitTimestamp = DateTime.UtcNow }); }
    }
}