// Part of GuerrillaNtp: https://guerrillantp.machinezoo.com
using System;

namespace GuerrillaNtp
{
    public partial class NtpClient
    {
        volatile NtpTime last;

        /// <summary>
        /// Result of the last NTP query.
        /// </summary>
        /// <value>
        /// Last <see cref="NtpTime"/> returned by <see cref="Query()"/>
        /// or <see cref="QueryAsync(System.Threading.CancellationToken)"/>.
        /// If NTP server has not been queried yet, this property is null.
        /// </value>
        /// <remarks>
        /// <para>
        /// You can use <see cref="NtpTime.LocalFallback"/> as fallback as in
        /// <see cref="Last"/> ?? <see cref="NtpTime.LocalFallback"/>.
        /// </para>
        /// <para>
        /// If multiple threads query the NTP server in parallel (not recommended),
        /// this property will hold result of whichever query finishes last.
        /// This property is safe to access from multiple threads.
        /// </para>
        /// </remarks>
        public NtpTime Last => last;
    }
}
