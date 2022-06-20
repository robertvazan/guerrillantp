// Part of GuerrillaNtp: https://guerrillantp.machinezoo.com
using System;
using System.Net.Sockets;

namespace GuerrillaNtp
{
    public partial class NtpClient
    {
        Socket GetConnection()
        {
            var socket = GetSocket();
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
        /// Queries the SNTP server with configurable <see cref="NtpPacket"/> request.
        /// </summary>
        /// <param name="request">SNTP request packet to use when querying the network time server.</param>
        /// <returns>SNTP reply packet returned by the server.</returns>
        /// <remarks>
        /// <see cref="NtpPacket()" /> constructor
        /// creates valid request packet, which you can further customize.
        /// If you don't need any customization of the request packet, call <see cref="GetCorrectionResponse(NtpPacket)" /> instead.
        /// Returned <see cref="NtpPacket" /> contains correction offset in
        /// <see cref="NtpPacket.CorrectionOffset" /> property.
        /// </remarks>
        /// <exception cref="NtpException">
        /// Thrown when the request packet is invalid or when the server responds with invalid reply packet.
        /// </exception>
        /// <exception cref="SocketException">
        /// Thrown when no reply is received before <see cref="Timeout" /> is reached
        /// or when there is an error communicating with the server.
        /// </exception>
        /// <seealso cref="GetCorrectionOffset" />
        /// <seealso cref="GetCorrectionResponse()" />
        /// <seealso cref="NtpPacket.CorrectionOffset" />
        public NtpPacket GetCorrectionResponse(NtpPacket request)
        {
            request.ValidateRequest();

            using var socket = GetConnection();

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

            this.LastCorrectionOffset = packet.CorrectionOffset;

            return packet;
        }

        /// <inheritdoc cref="GetCorrectionResponse(NtpPacket)"/>
        /// <summary>
        /// Queries the SNTP server with default options.
        /// </summary>
        public NtpPacket GetCorrectionResponse()
        {
            return GetCorrectionResponse(new NtpPacket());
        }

        /// <summary>
        /// Queries the SNTP server and returns correction offset.
        /// </summary>
        /// <remarks>
        /// Use this method if you just want correction offset from the server.
        /// Call <see cref="GetCorrectionResponse()" /> to obtain <see cref="NtpPacket" />
        /// with additional information besides <see cref="NtpPacket.CorrectionOffset" />.
        /// </remarks>
        /// <returns>
        /// Offset that should be added to local time to match server time.
        /// </returns>
        /// <exception cref="NtpException">Thrown when the server responds with invalid reply packet.</exception>
        /// <exception cref="SocketException">
        /// Thrown when no reply is received before <see cref="Timeout" /> is reached
        /// or when there is an error communicating with the server.
        /// </exception>
        /// <seealso cref="GetCorrectionResponse()" />
        /// <seealso cref="NtpPacket.CorrectionOffset" />
        public TimeSpan GetCorrectionOffset()
        {
            return GetCorrectionResponse().CorrectionOffset;
        }
    }
}
