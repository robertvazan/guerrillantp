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
        /// SNTP packet that caused this exception, if any.
        /// </summary>
        public NtpPacket Packet { get; private set; }

        internal NtpException(NtpPacket packet, String message)
            : base(message)
        {
            Packet = packet;
        }
    }
}
