// Part of GuerrillaNtp: https://guerrillantp.machinezoo.com
using System;
using System.Net.Sockets;

namespace GuerrillaNtp
{
    public partial class NtpClient
    {
        Socket Connect()
        {
            var socket = CreateSocket();
            try
            {
                socket.Connect(endpoint);
            }
            catch
            {
                socket.Dispose();
                throw;
            }
            return socket;
        }

        /// <summary>
        /// Queries the SNTP server.
        /// </summary>
        /// <returns>SNTP reply packet returned by the server.</returns>
        /// <remarks>
        /// Returned <see cref="NtpPacket" /> contains correction offset in
        /// <see cref="NtpPacket.CorrectionOffset" /> property.
        /// </remarks>
        /// <exception cref="NtpException">
        /// Thrown when the server responds with invalid reply packet.
        /// </exception>
        /// <exception cref="SocketException">
        /// Thrown when no reply is received before <see cref="Timeout" /> is reached
        /// or when there is an error communicating with the server.
        /// </exception>
        /// <seealso cref="NtpPacket.CorrectionOffset" />
        public NtpPacket Query()
        {
            var request = new NtpPacket();
            request.ValidateRequest();

            using var socket = Connect();

            socket.Send(request.Bytes);
            var response = new byte[160];
            int received = socket.Receive(response);
            var truncated = new byte[received];
            Array.Copy(response, truncated, received);

            var packet = new NtpPacket(truncated)
            {
                DestinationTimestamp = DateTime.UtcNow
            };

            packet.ValidateReply(request);

            Last = packet;

            return packet;
        }
    }
}
