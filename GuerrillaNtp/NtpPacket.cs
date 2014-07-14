using System;
using System.Net;

namespace GuerrillaNtp
{
    /// <summary>
    /// Represents a packet for communications to and from a network time server
    /// </summary>
    public class NtpPacket
    {
        readonly DateTime primeEpoch = new DateTime(1900, 1, 1);

        /// <summary>
        /// Initializes a new instance of the <see cref="NtpPacket" /> class
        /// </summary>
        public NtpPacket()
            : this(new byte[48])
        {
            LeapIndicator = NtpLeapIndicator.NoWarning;
            Mode = NtpMode.Client;
            VersionNumber = 4;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NtpPacket"/> class
        /// </summary>
        /// <param name="bytes">
        /// A byte array representing an NTP packet
        /// </param>
        public NtpPacket(byte[] bytes)
        {
            if (bytes.Length < 48)
                throw new ArgumentOutOfRangeException("bytes", "The byte array must be at least length 48.");
            Bytes = bytes;
        }

        /// <summary>
        /// Gets the byte array representing this packet
        /// </summary>
        public byte[] Bytes { get; private set; }

        /// <summary>
        /// Gets or sets the value indicating which, if any, warning should be sent due to an impending leap second
        /// </summary>
        public NtpLeapIndicator LeapIndicator
        {
            get { return (NtpLeapIndicator)((Bytes[0] & 0xC0) >> 6); }
            set { Bytes[0] = (byte)((Bytes[0] & ~0xC0) | (int)value << 6); }
        }

        /// <summary>
        /// Gets or sets the association mode
        /// </summary>
        public NtpMode Mode
        {
            get { return (NtpMode)(Bytes[0] & 0x07); }
            set { Bytes[0] = (byte)((Bytes[0] & ~0x07) | (int)value); }
        }

        /// <summary>
        /// Gets the date and time this packet left the server
        /// </summary>
        public DateTime OriginTimestamp { get { return primeEpoch.AddSeconds(ToUInt32BE(Bytes, 24)); } }

        /// <summary>
        /// Gets the polling interval (in log₂ seconds)
        /// </summary>
        public byte Poll { get { return Bytes[2]; } }

        /// <summary>
        /// Gets the precision of the system clock (in log₂ seconds)
        /// </summary>
        public byte Precision { get { return Bytes[3]; } }

        /// <summary>
        /// Gets the date and time this packet was received by the server
        /// </summary>
        public DateTime ReceiveTimestamp { get { return primeEpoch.AddSeconds(ToUInt32BE(Bytes, 32)); } }

        /// <summary>
        /// Gets the ID of the server or reference clock
        /// </summary>
        public int ReferenceId { get { return ToInt32BE(Bytes, 12); } }

        /// <summary>
        /// Gets the date and time the server was last set or corrected
        /// </summary>
        public DateTime ReferenceTimestamp { get { return primeEpoch.AddSeconds(ToUInt32BE(Bytes, 16)); } }

        /// <summary>
        /// Gets the total round trip delay from the server to the reference clock
        /// </summary>
        public int RootDelay { get { return ToInt32BE(Bytes, 4); } }

        /// <summary>
        /// Gets the amount of jitter the server observes in the reference clock
        /// </summary>
        public int RootDispersion { get { return ToInt32BE(Bytes, 8); } }

        /// <summary>
        /// Gets the server's distance from the reference clock
        /// </summary>
        public byte Stratum { get { return Bytes[1]; } }

        /// <summary>
        /// Gets the date and time that the packet was transmitted from the server
        /// </summary>
        public DateTime TransmitTimestamp { get { return new DateTime(1900, 1, 1).AddSeconds(ToUInt32BE(Bytes, 40)); } }

        /// <summary>
        /// Gets or sets the version number
        /// </summary>
        public int VersionNumber
        {
            get { return (Bytes[0] & 0x38) >> 3; }
            set { Bytes[0] = (byte)((Bytes[0] & ~0x38) | value << 3); }
        }

        static int ToInt32BE(byte[] bytes, int offset) { return (int)ToUInt32BE(bytes, offset); }
        static uint ToUInt32BE(byte[] bytes, int offset) { return SwapEndianness(BitConverter.ToUInt32(bytes, offset)); }
        static uint SwapEndianness(uint x) { return ((x & 0xff) << 24) | ((x & 0xff00) << 8) | ((x & 0xff0000) >> 8) | ((x & 0xff000000) >> 24); }
    }
}