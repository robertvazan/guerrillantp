using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace GuerrillaNtp
{
    /// <summary>
    /// Represents a client used to connect to a network time server
    /// </summary>
    public static class NtpClient
    {
        /// <summary>
        /// Gets the current date and time
        /// </summary>
        /// <returns>
        /// The current date and time
        /// </returns>
        public static DateTime GetDateTime()
        {
            return Send(new NtpPacket
            {
                LeapIndicator = NtpLeapIndicator.NoWarning, 
                Mode = NtpMode.Client, 
                VersionNumber = 4
            }).TransmitTimestamp;
        }

        /// <summary>
        /// Gets the current date and time using the given <see cref="NtpPacket"/>
        /// </summary>
        /// <param name="ntpPacket">
        /// The <see cref="NtpPacket"/> object to use when querying the network time server
        /// </param>
        /// <param name="host">
        /// The hostname of the network time server to connect to
        /// </param>
        /// <param name="port">
        /// The port of the network time server to connect to
        /// </param>
        /// <returns>
        /// The response from the NTP server
        /// </returns>
        public static NtpPacket Send(NtpPacket ntpPacket, string host = "time.nist.gov", int port = 123)
        {
            var address = Dns.GetHostEntry(host).AddressList.FirstOrDefault();
            if (address != null)
            {
                var ipEndPoint = new IPEndPoint(address, port);
                using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
                {
                    socket.Connect(ipEndPoint);
                    socket.Send(ntpPacket.Bytes);

                    var buffer = new byte[48];
                    socket.Receive(buffer);

                    ntpPacket = new NtpPacket(buffer);
                }
            }
            return ntpPacket;
        }
    }
}