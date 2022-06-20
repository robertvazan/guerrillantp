// Part of GuerrillaNtp: https://guerrillantp.machinezoo.com
using System;

namespace GuerrillaNtp
{
    public partial class NtpClient
    {
        /// <summary>
        /// The default NTP endpoint (pool.ntp.org).
        /// </summary>
        public static readonly string DefaultEndpoint = "pool.ntp.org";

        /// <summary>
        /// The default NTP port (123).
        /// </summary>
        public const int DefaultPort = 123;

        /// <summary>
        /// The default NTP timeout (1 second).
        /// </summary>
        public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(1);

        /// <summary>
        /// The default <see cref="NtpClient"/> which communicates with pool.ntp.org
        /// </summary>
        public static readonly NtpClient Default = new();
    }
}
