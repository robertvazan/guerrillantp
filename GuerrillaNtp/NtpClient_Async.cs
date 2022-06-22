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
        /// <param name="token">A <see cref="CancellationToken"/> used to cancel this request.</param>
        /// <returns>Network time reported by the server.</returns>
        /// <exception cref="NtpException">
        /// Thrown when the server sends invalid response.
        /// </exception>
        /// <exception cref="SocketException">
        /// Thrown when no reply is received before <see cref="Timeout" /> is reached
        /// or when there is an error communicating with the server.
        /// </exception>
        public async Task<NtpTime> QueryAsync(CancellationToken token = default)
        {
            using var socket = await ConnectAsync(token).DefaultAwait();
            var request = new NtpRequest();
            await socket.SendAsync(request.ToPacket().ToBytes(), SocketFlags.None, token).DefaultAwait();
            var buffer = new byte[160];
            int length = await socket.ReceiveAsync(buffer, SocketFlags.None, token).DefaultAwait();
            var response = NtpResponse.FromPacket(NtpPacket.FromBytes(buffer, length));
            if (!response.Matches(request))
                throw new NtpException("Response does not match the request.");
            return last = new NtpTime(response);
        }
    }
}
