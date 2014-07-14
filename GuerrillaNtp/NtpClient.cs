using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace GuerrillaNtp
{
    /// <summary>
    /// Represents a client used to connect to a network time server
    /// </summary>
    public class NtpClient : IDisposable
    {
        readonly UdpClient UdpClient;

        /// <summary>
        /// Creates new NtpClient from server endpoint
        /// </summary>
        /// <param name="endpoint">Endpoint of the remote NTP server</param>
        public NtpClient(IPEndPoint endpoint)
        {
            UdpClient = new UdpClient();
            UdpClient.Connect(endpoint);
        }

        /// <summary>
        /// Creates new NtpClient from server's IP address and optional port
        /// </summary>
        /// <param name="address">IP address of remote NTP server</param>
        /// <param name="port">Port of remote NTP server. Default is 123.</param>
        public NtpClient(IPAddress address, int port = 123) : this(new IPEndPoint(address, port)) { }

        /// <summary>
        /// Creates new NtpClient from server's host name and optional port
        /// </summary>
        /// <param name="host">The hostname of the NTP server to connect to</param>
        /// <param name="port">The port of the NTP server to connect to</param>
        public NtpClient(string host, int port = 123) : this(Dns.GetHostAddresses(host).First(), port) { }

        /// <summary>
        /// Releases all resources held by NtpClient
        /// </summary>
        public void Dispose() { UdpClient.Close(); }

        /// <summary>
        /// Gets the current date and time
        /// </summary>
        /// <returns>
        /// The current date and time
        /// </returns>
        public DateTime GetDateTime()
        {
            return Query().TransmitTimestamp;
        }

        /// <summary>
        /// Queries NTP server with configurable NTP packet
        /// </summary>
        /// <param name="request">NTP packet to use when querying the network time server </param>
        /// <returns>The response from the NTP server</returns>
        public NtpPacket Query(NtpPacket request)
        {
            UdpClient.Send(request.Bytes, request.Bytes.Length);
            IPEndPoint remote = null;
            return new NtpPacket(UdpClient.Receive(ref remote));
        }

        /// <summary>
        /// Queries NTP server with default options
        /// </summary>
        /// <returns>NTP packet returned from the server</returns>
        public NtpPacket Query() { return Query(new NtpPacket()); }
    }
}