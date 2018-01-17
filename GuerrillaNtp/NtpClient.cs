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
        readonly UdpClient UdpClient;

        /// <summary>
        /// Gets or sets timeout for NTP queries
        /// </summary>
        public TimeSpan Timeout
        {
            get { return TimeSpan.FromMilliseconds(UdpClient.Client.ReceiveTimeout); }
            set
            {
                if (value < TimeSpan.FromMilliseconds(1))
                    throw new ArgumentOutOfRangeException();
                UdpClient.Client.ReceiveTimeout = Convert.ToInt32(value.TotalMilliseconds);
            }
        }

        /// <summary>
        /// Creates new NtpClient from server endpoint
        /// </summary>
        /// <param name="endpoint">Endpoint of the remote NTP server</param>
        public NtpClient(IPEndPoint endpoint)
        {
            UdpClient = new UdpClient();
            UdpClient.Client.ReceiveTimeout = 15000;
            UdpClient.Client.Connect(endpoint);
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
        public void Dispose() { UdpClient.Dispose(); }

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
            UdpClient.Client.Send(request.Bytes);
            var response = new NtpPacket(UdpClient.ReceiveAsync().Result.Buffer);
            response.OriginTimestamp = request.OriginTimestamp;
            response.DestinationTimestamp = DateTime.UtcNow;
            return response;
        }

        /// <summary>
        /// Queries NTP server with default options
        /// </summary>
        /// <returns>NTP packet returned from the server</returns>
        public NtpPacket Query() { return Query(new NtpPacket() { OriginTimestamp = DateTime.UtcNow }); }
    }
}