// Part of GuerrillaNtp: https://guerrillantp.machinezoo.com
using System;

namespace GuerrillaNtp
{
    /// <summary>
    /// RFC4330 SNTP request.
    /// </summary>
    /// <remarks>
    /// This is a low-level API for building SNTP requests, which can then be converted to <see cref="NtpPacket" />.
    /// Most applications should just use <see cref="NtpClient.Query()" /> and properties in <see cref="NtpClock" />.
    /// </remarks>
    /// <seealso cref="NtpPacket" />
    /// <seealso cref="NtpResponse" />
    public record NtpRequest
    {
        /// <summary>
        /// Time when the request was sent.
        /// </summary>
        /// <value>
        /// UTC time when the request was sent. Defaults to <see cref="DateTime.UtcNow" />.
        /// </value>
        public DateTime TransmitTimestamp { get; init; } = DateTime.UtcNow;

        /// <summary>
        /// Converts <see cref="NtpPacket" /> to <see cref="NtpRequest" />.
        /// </summary>
        /// <param name="packet">Packet that encodes the request.</param>
        /// <returns>SNTP request found in the packet.</returns>
        /// <exception cref="NtpException">
        /// Thrown when the packet does not encode valid SNTP request.
        /// </exception>
        public static NtpRequest FromPacket(NtpPacket packet)
        {
            if (packet.Mode != NtpMode.Client)
                throw new NtpException("Not a request packet.");
            if (packet.TransmitTimestamp == null)
                throw new NtpException("Request packet must have transit timestamp.");
            return new NtpRequest { TransmitTimestamp = packet.TransmitTimestamp.Value };
        }

        /// <summary>
        /// Validates the request and converts it to <see cref="NtpPacket" />.
        /// </summary>
        /// <returns>Valid SNTP packet encoding the request.</returns>
        /// <exception cref="NtpException">
        /// Thrown if this is not a valid SNTP request.
        /// </exception>
        public NtpPacket ToPacket()
        {
            var packet = new NtpPacket
            {
                Mode = NtpMode.Client,
                TransmitTimestamp = TransmitTimestamp
            };
            packet.Validate();
            return packet;
        }

        /// <summary>
        /// Checks whether this object describes valid SNTP request.
        /// </summary>
        /// <exception cref="NtpException">
        /// Thrown if this is not a valid SNTP request.
        /// </exception>
        /// <remarks>
        /// Object properties do not perform validation. Call this method to validate the request.
        /// <see cref="FromPacket(NtpPacket)" /> and <see cref="ToPacket()" /> perform validation automatically.
        /// </remarks>
        public void Validate() => ToPacket();
    }
}
