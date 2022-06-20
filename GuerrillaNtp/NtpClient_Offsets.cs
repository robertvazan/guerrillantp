// Part of GuerrillaNtp: https://guerrillantp.machinezoo.com
using System;

namespace GuerrillaNtp
{
    public partial class NtpClient
    {
        volatile NtpPacket last;

        /// <summary>
        /// Gets result of the last NTP query.
        /// </summary>
        /// <value>
        /// Last <see cref="NtpPacket"/> returned by any method of this class.
        /// If NTP server has not been queried yet, this property is null.
        /// </value>
        /// <remarks>
        /// If multiple threads query the NTP server in parallel (not recommended),
        /// this property will hold result of whichever query finishes last.
        /// This property is safe to access from multiple threads.
        /// </remarks>
        public NtpPacket Last => last;
    }
}
