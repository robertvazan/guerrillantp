// Part of GuerrillaNtp: https://guerrillantp.machinezoo.com
using System;

namespace GuerrillaNtp
{
    public partial class NtpClient
    {
        /// <summary>
        /// The default <see cref="NtpClient"/> which communicates with pool.ntp.org
        /// </summary>
        public static NtpClient Default { get; }

        /// <summary>
        /// The default NTP endpoint (pool.ntp.org).
        /// </summary>
        public static string DefaultEndpoint { get; }

        /// <summary>
        /// The default NTP port (123).
        /// </summary>
        public static int DefaultPort { get; }

        /// <summary>
        /// The default NTP timeout (1 second).
        /// </summary>
        public static TimeSpan DefaultTimeout { get; }

        static NtpClient()
        {
            DefaultEndpoint = "pool.ntp.org";
            DefaultPort = 123;
            DefaultTimeout = TimeSpan.FromSeconds(1);

            Default = new();
        }
    }
}
