// Part of GuerrillaNtp: https://guerrillantp.machinezoo.com
using System;

namespace GuerrillaNtp {

    public partial class NtpClient
    {


        /// <summary>
        /// Returns the last <see cref="NtpPacket.CorrectionOffset"/> returned by any method of this class.
        /// </summary>
        public TimeSpan LastCorrectionOffset { get; private set; }

        /// <summary>
        /// Returns <see cref="DateTime.Now"/> + <see cref="LastCorrectionOffset"/>
        /// </summary>
        public DateTime DateTimeNow => DateTime.Now + LastCorrectionOffset;

        /// <summary>
        /// Returns <see cref="DateTimeOffset.Now"/> + <see cref="LastCorrectionOffset"/>
        /// </summary>
        public DateTimeOffset DateTimeOffsetNow => DateTimeOffset.Now + LastCorrectionOffset;

        /// <summary>
        /// Returns <see cref="DateTime.UtcNow"/> + <see cref="LastCorrectionOffset"/>
        /// </summary>
        public DateTime DateTimeUtcNow => DateTime.UtcNow + LastCorrectionOffset;

        /// <summary>
        /// Returns <see cref="DateTimeOffset.UtcNow"/> + <see cref="LastCorrectionOffset"/>
        /// </summary>
        public DateTimeOffset DateTimeOffsetUtcNow => DateTimeOffset.UtcNow + LastCorrectionOffset;

    }
}
