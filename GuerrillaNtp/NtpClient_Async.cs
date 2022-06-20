// Part of GuerrillaNtp: https://guerrillantp.machinezoo.com
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GuerrillaNtp {
    public partial class NtpClient
    {
        private async Task<Socket> GetConnectionAsync(CancellationToken token) {
            var ret = GetSocket();
            try {
                await ret.ConnectAsync(endpoint, token)
                    .DefaultAwait()
                    ;
            }
            catch {
                ret.Dispose();
                throw;
            }

            return ret;
        }

        /// <summary>
        /// Queries the SNTP server with configurable <see cref="T:GuerrillaNtp.NtpPacket" /> request.
        /// </summary>
        /// <param name="request">SNTP request packet to use when querying the network time server.</param>
        /// <param name="Token">A <see cref="CancellationToken"/> used to cancel this request.</param>
        /// <returns>SNTP reply packet returned by the server.</returns>
        /// <remarks>
        /// <see cref="NtpPacket()" /> constructor
        /// creates valid request packet, which you can further customize.
        /// If you don't need any customization of the request packet, call <see cref="GetCorrectionResponseAsync(NtpPacket, CancellationToken)" /> instead.
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
        /// <seealso cref="GetCorrectionOffsetAsync" />
        /// <seealso cref="GetCorrectionResponseAsync(CancellationToken)" />
        /// <seealso cref="NtpPacket.CorrectionOffset" />
        public async Task<NtpPacket> GetCorrectionResponseAsync(NtpPacket request, CancellationToken Token = default) {
            request.ValidateRequest();

            using var socket = await GetConnectionAsync(Token)
                .DefaultAwait()
                ;

            await socket.SendAsync(request.Bytes,SocketFlags.None, Token)
                .DefaultAwait()
                ;

            var response = new byte[160];
            int received = await socket.ReceiveAsync(response, SocketFlags.None, Token)
                .DefaultAwait()
                ;

            var truncated = new byte[received];
            Array.Copy(response, truncated, received);
            var ret = new NtpPacket(truncated) {
                DestinationTimestamp = DateTime.UtcNow 
            };

            ret.ValidateReply(request);

            this.LastCorrectionOffset = ret.CorrectionOffset;

            return ret;
        }

        /// <inheritdoc cref="GetCorrectionResponseAsync(NtpPacket, CancellationToken)"/>
        /// <summary>
        /// Queries the SNTP server with default options.
        /// </summary>
        public Task<NtpPacket> GetCorrectionResponseAsync(CancellationToken Token = default) {
            return GetCorrectionResponseAsync(new NtpPacket(), Token);
        }

        /// <summary>
        /// Queries the SNTP server and returns correction offset.
        /// </summary>
        /// <param name="Token">A <see cref="CancellationToken"/> used to cancel this request.</param>
        /// <remarks>
        /// Use this method if you just want correction offset from the server.
        /// Call <see cref="GetCorrectionResponseAsync(CancellationToken)" /> to obtain <see cref="NtpPacket" />
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
        public async Task<TimeSpan> GetCorrectionOffsetAsync(CancellationToken Token = default) {
            var tret = await GetCorrectionResponseAsync(Token)
                .DefaultAwait()
                ;
            
            var ret = tret.CorrectionOffset;
            return ret;
        }

    }
}
