// Part of GuerrillaNtp: https://guerrillantp.machinezoo.com
using System;

namespace GuerrillaNtp
{
    /// <summary>
    /// Represents errors that occur in SNTP packets or during SNTP operation.
    /// </summary>
    public class NtpException : Exception
    {
        /// <summary>
        /// Gets the SNTP packet that caused this exception, if any.
        /// </summary>
        /// <value>
        /// SNTP packet that caused this exception, usually reply packet,
        /// or <c>null</c> if the error is not specific to any packet.
        /// </value>
        public NtpPacket Packet { get; private set; }

        internal NtpException(NtpPacket packet, String message)
            : base(message)
        {
            Packet = packet;
        }
    }
}
