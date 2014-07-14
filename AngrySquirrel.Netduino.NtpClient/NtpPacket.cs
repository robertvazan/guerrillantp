using System;
using AngrySquirrel.Netduino.Utilities;

namespace AngrySquirrel.Netduino.NtpClient
{
    /// <summary>
    /// Represents a packet for communications to and from a network time server
    /// </summary>
    public class NtpPacket
    {
        #region Fields

        private readonly DateTime primeEpoch = new DateTime(1900, 1, 1);

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="NtpPacket" /> class
        /// </summary>
        public NtpPacket()
            : this(new byte[48])
        {
            LeapIndicator = LeapIndicator.NoWarning;
            Mode = Mode.Client;
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
            {
                throw new ArgumentOutOfRangeException("bytes", "The byte array must be at least length 48.");
            }

            Bytes = bytes;
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the byte array representing this packet
        /// </summary>
        public byte[] Bytes { get; private set; }

        /// <summary>
        /// Gets or sets the value indicating which, if any, warning should be sent due to an impending leap second
        /// </summary>
        public LeapIndicator LeapIndicator
        {
            get
            {
                const int bitMask = 0xC0;
                return (LeapIndicator)((Bytes[0] & bitMask) >> 6);
            }

            set
            {
                const int bitMask = 0xC0;
                Bytes[0] = (byte)((Bytes[0] & ~bitMask) | (int)value << 6);
            }
        }

        /// <summary>
        /// Gets or sets the association mode
        /// </summary>
        public Mode Mode
        {
            get
            {
                const int bitMask = 0x07;
                return (Mode)(Bytes[0] & bitMask);
            }

            set
            {
                const int bitMask = 0x07;
                Bytes[0] = (byte)((Bytes[0] & ~bitMask) | (int)value);
            }
        }

        /// <summary>
        /// Gets the date and time this packet left the server
        /// </summary>
        public DateTime OriginTimestamp
        {
            get
            {
                var seconds = ByteHelper.ToUInt32(Bytes, 24, ByteOrder.BigEndian);
                return primeEpoch.AddSeconds(seconds);
            }
        }

        /// <summary>
        /// Gets the polling interval (in log₂ seconds)
        /// </summary>
        public byte Poll
        {
            get
            {
                return Bytes[2];
            }
        }

        /// <summary>
        /// Gets the precision of the system clock (in log₂ seconds)
        /// </summary>
        public byte Precision
        {
            get
            {
                return Bytes[3];
            }
        }

        /// <summary>
        /// Gets the date and time this packet was received by the server
        /// </summary>
        public DateTime ReceiveTimestamp
        {
            get
            {
                var seconds = ByteHelper.ToUInt32(Bytes, 32, ByteOrder.BigEndian);
                return primeEpoch.AddSeconds(seconds);
            }
        }

        /// <summary>
        /// Gets the ID of the server or reference clock
        /// </summary>
        public int ReferenceId
        {
            get
            {
                return ByteHelper.ToInt32(new[] { Bytes[12], Bytes[13], Bytes[14], Bytes[15] }, byteOrder: ByteOrder.BigEndian);
            }
        }

        /// <summary>
        /// Gets the date and time the server was last set or corrected
        /// </summary>
        public DateTime ReferenceTimestamp
        {
            get
            {
                var seconds = ByteHelper.ToUInt32(Bytes, 16, ByteOrder.BigEndian);
                return primeEpoch.AddSeconds(seconds);
            }
        }

        /// <summary>
        /// Gets the total round trip delay from the server to the reference clock
        /// </summary>
        public int RootDelay
        {
            get
            {
                return ByteHelper.ToInt32(new[] { Bytes[4], Bytes[5], Bytes[6], Bytes[7] }, byteOrder: ByteOrder.BigEndian);
            }
        }

        /// <summary>
        /// Gets the amount of jitter the server observes in the reference clock
        /// </summary>
        public int RootDispersion
        {
            get
            {
                return ByteHelper.ToInt32(new[] { Bytes[8], Bytes[9], Bytes[10], Bytes[11] }, byteOrder: ByteOrder.BigEndian);
            }
        }

        /// <summary>
        /// Gets the server's distance from the reference clock
        /// </summary>
        public byte Stratum
        {
            get
            {
                return Bytes[1];
            }
        }

        /// <summary>
        /// Gets the date and time that the packet was transmitted from the server
        /// </summary>
        public DateTime TransmitTimestamp
        {
            get
            {
                var seconds = ByteHelper.ToUInt32(Bytes, 40, ByteOrder.BigEndian);
                return new DateTime(1900, 1, 1).AddSeconds(seconds);
            }
        }

        /// <summary>
        /// Gets or sets the version number
        /// </summary>
        public int VersionNumber
        {
            get
            {
                const int bitMask = 0x38;
                return (Bytes[0] & bitMask) >> 3;
            }

            set
            {
                const int bitMask = 0x38;
                Bytes[0] = (byte)((Bytes[0] & ~bitMask) | value << 3);
            }
        }

        #endregion
    }
}