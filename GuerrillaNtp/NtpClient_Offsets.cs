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
        /// Once this property is populated with <see cref="NtpTime"/> that is <see cref="NtpTime.Synchronized"/>,
        /// it will be updated only with another <see cref="NtpTime"/> that is also <see cref="NtpTime.Synchronized"/>,
        /// This logic is intended to prevent special responses (e.g. Kiss-o'-Death packets),
        /// which do not really carry network time, from replacing previously obtained network time.
        /// </para>
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

        NtpTime Update(NtpRequest request, byte[] buffer, int length)
        {
            var response = NtpResponse.FromPacket(NtpPacket.FromBytes(buffer, length));
            if (!response.Matches(request))
                throw new NtpException("Response does not match the request.");
            var time = new NtpTime(response);
            if (time.Synchronized || last == null)
                last = time;
            return time;
        }
    }
}
