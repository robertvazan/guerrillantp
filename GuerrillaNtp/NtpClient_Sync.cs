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
        /// <returns>Network time reported by the server.</returns>
        /// <exception cref="NtpException">
        /// Thrown when the server sends invalid response.
        /// </exception>
        /// <exception cref="SocketException">
        /// Thrown when no reply is received before <see cref="Timeout" /> is reached
        /// or when there is an error communicating with the server.
        /// </exception>
        public NtpTime Query()
        {
            using var socket = Connect();
            var request = new NtpRequest();
            socket.Send(request.ToPacket().ToBytes());
            var buffer = new byte[160];
            int length = socket.Receive(buffer);
            return Update(request, buffer, length);
        }
    }
}
