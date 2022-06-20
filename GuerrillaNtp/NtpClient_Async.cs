// Part of GuerrillaNtp: https://guerrillantp.machinezoo.com
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GuerrillaNtp
{
    public partial class NtpClient
    {
        async Task<Socket> ConnectAsync(CancellationToken token)
        {
            var socket = CreateSocket();
            try
            {
                await socket.ConnectAsync(endpoint, token).DefaultAwait();
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
        /// <param name="Token">A <see cref="CancellationToken"/> used to cancel this request.</param>
        /// <returns>SNTP reply packet returned by the server.</returns>
        /// <remarks>
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
        /// <seealso cref="NtpPacket.CorrectionOffset" />
        public async Task<NtpPacket> QueryAsync(CancellationToken Token = default)
        {
            var request = new NtpPacket();
            request.ValidateRequest();

            using var socket = await ConnectAsync(Token).DefaultAwait();

            await socket.SendAsync(request.Bytes, SocketFlags.None, Token).DefaultAwait();

            var response = new byte[160];
            int received = await socket.ReceiveAsync(response, SocketFlags.None, Token).DefaultAwait();

            var truncated = new byte[received];
            Array.Copy(response, truncated, received);
            var packet = new NtpPacket(truncated)
            {
                DestinationTimestamp = DateTime.UtcNow
            };

            packet.ValidateReply(request);

            last = packet;

            return packet;
        }
    }
}
