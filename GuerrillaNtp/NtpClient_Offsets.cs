// Part of GuerrillaNtp: https://guerrillantp.machinezoo.com
using System;

namespace GuerrillaNtp
{
    public partial class NtpClient
    {
        /// <summary>
        /// Returns the last <see cref="NtpPacket"/> returned by any method of this class.
        /// </summary>
        public NtpPacket Last { get; private set; }
    }
}
