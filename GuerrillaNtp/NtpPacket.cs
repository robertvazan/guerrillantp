// Part of GuerrillaNtp: https://guerrillantp.machinezoo.com
using System;

namespace GuerrillaNtp
{
    /// <summary>
    /// Represents RFC4330 SNTP packet used for communication to and from a network time server.
    /// </summary>
    /// <remarks>
    /// <para>
    /// See <a href="https://guerrillantp.machinezoo.com/">project homepage</a> for guidance on how to use GuerrillaNtp.
    /// Most applications should just use the <see cref="P:GuerrillaNtp.NtpPacket.CorrectionOffset" /> property
    /// or even better call <see cref="M:GuerrillaNtp.NtpClient.GetCorrectionOffset" />.
    /// </para>
    /// <para>
    /// The same data structure represents both request and reply packets.
    /// Request and reply differ in which properties are set and to what values.
    /// </para>
    /// <para>
    /// The only real property is <see cref="P:GuerrillaNtp.NtpPacket.Bytes" />.
    /// All other properties read from and write to the underlying byte array
    /// with the exception of <see cref="P:GuerrillaNtp.NtpPacket.DestinationTimestamp" />,
    /// which is not part of the packet on network and it is instead set locally after receiving the packet.
    /// </para>
    /// </remarks>
    /// <seealso cref="T:GuerrillaNtp.NtpClient" />
    /// <seealso cref="M:GuerrillaNtp.NtpClient.Query(GuerrillaNtp.NtpPacket)" />
    /// <seealso cref="P:GuerrillaNtp.NtpPacket.CorrectionOffset" />
    public class NtpPacket
    {
        static readonly DateTime epoch = new DateTime(1900, 1, 1);

        /// <summary>
        /// Gets RFC4330-encoded SNTP packet.
        /// </summary>
        /// <value>
        /// Byte array containing RFC4330-encoded SNTP packet. It is at least 48 bytes long.
        /// </value>
        /// <remarks>
        /// This is the only real property. All other properties except
        /// <see cref="P:GuerrillaNtp.NtpPacket.DestinationTimestamp" /> read from or write to this byte array.
        /// </remarks>
        public byte[] Bytes { get; private set; }

        /// <summary>
        /// Gets the leap second indicator.
        /// </summary>
        /// <value>
        /// Leap second warning, if any. Special value
        /// <see cref="F:GuerrillaNtp.NtpLeapIndicator.AlarmCondition" /> indicates unsynchronized server clock.
        /// Default is <see cref="F:GuerrillaNtp.NtpLeapIndicator.NoWarning" />.
        /// </value>
        /// <remarks>
        /// Only servers fill in this property. Clients can consult this property for possible leap second warning.
        /// </remarks>
        public NtpLeapIndicator LeapIndicator
        {
            get { return (NtpLeapIndicator)((Bytes[0] & 0xC0) >> 6); }
        }

        /// <summary>
        /// Gets or sets protocol version number.
        /// </summary>
        /// <value>
        /// SNTP protocol version. Default is 4, which is the latest version at the time of this writing.
        /// </value>
        /// <remarks>
        /// In request packets, clients should leave this property at default value 4.
        /// Servers usually reply with the same protocol version.
        /// </remarks>
        public int VersionNumber
        {
            get { return (Bytes[0] & 0x38) >> 3; }
            set { Bytes[0] = (byte)((Bytes[0] & ~0x38) | value << 3); }
        }

        /// <summary>
        /// Gets or sets SNTP packet mode, i.e. whether this is client or server packet.
        /// </summary>
        /// <value>
        /// SNTP packet mode. Default is <see cref="F:GuerrillaNtp.NtpMode.Client" /> in newly created packets.
        /// Server reply should have this property set to <see cref="F:GuerrillaNtp.NtpMode.Server" />.
        /// </value>
        public NtpMode Mode
        {
            get { return (NtpMode)(Bytes[0] & 0x07); }
            set { Bytes[0] = (byte)((Bytes[0] & ~0x07) | (int)value); }
        }

        /// <summary>
        /// Gets server's distance from the reference clock.
        /// </summary>
        /// <value>
        /// <para>
        /// Distance from the reference clock. This property is set only in server reply packets.
        /// Servers connected directly to reference clock hardware set this property to 1.
        /// Statum number is incremented by 1 on every hop down the NTP server hierarchy.
        /// </para>
        /// <para>
        /// Special value 0 indicates that this packet is a Kiss-o'-Death message
        /// with kiss code stored in <see cref="P:GuerrillaNtp.NtpPacket.ReferenceId" />.
        /// </para>
        /// </value>
        public int Stratum { get { return Bytes[1]; } }

        /// <summary>
        /// Gets server's preferred polling interval.
        /// </summary>
        /// <value>
        /// Polling interval in log₂ seconds, e.g. 4 stands for 16s and 17 means 131,072s.
        /// </value>
        public int Poll { get { return Bytes[2]; } }

        /// <summary>
        /// Gets the precision of server clock.
        /// </summary>
        /// <value>
        /// Clock precision in log₂ seconds, e.g. -20 for microsecond precision.
        /// </value>
        public int Precision { get { return (sbyte)Bytes[3]; } }

        /// <summary>
        /// Gets the total round-trip delay from the server to the reference clock.
        /// </summary>
        /// <value>
        /// Round-trip delay to the reference clock. Normally a positive value smaller than one second.
        /// </value>
        public TimeSpan RootDelay { get { return GetTimeSpan32(4); } }

        /// <summary>
        /// Gets the estimated error in time reported by the server.
        /// </summary>
        /// <value>
        /// Estimated error in time reported by the server. Normally a positive value smaller than one second.
        /// </value>
        public TimeSpan RootDispersion { get { return GetTimeSpan32(8); } }

        /// <summary>
        /// Gets the ID of the time source used by the server or Kiss-o'-Death code sent by the server.
        /// </summary>
        /// <value>
        /// <para>
        /// ID of server's time source or Kiss-o'-Death code.
        /// Purpose of this property depends on value of <see cref="P:GuerrillaNtp.NtpPacket.Stratum" /> property.
        /// </para>
        /// <para>
        /// Stratum 1 servers write here one of several special values that describe the kind of hardware clock they use.
        /// </para>
        /// <para>
        /// Stratum 2 and lower servers set this property to IPv4 address of their upstream server.
        /// If upstream server has IPv6 address, the address is hashed, because it doesn't fit in this property.
        /// </para>
        /// <para>
        /// When server sets <see cref="P:GuerrillaNtp.NtpPacket.Stratum" /> to special value 0,
        /// this property contains so called kiss code that instructs the client to stop querying the server.
        /// </para>
        /// </value>
        public uint ReferenceId { get { return GetUInt32BE(12); } }

        /// <summary>
        /// Gets or sets the time when the server clock was last set or corrected.
        /// </summary>
        /// <value>
        /// Time when the server clock was last set or corrected or <c>null</c> when not specified.
        /// </value>
        /// <remarks>
        /// This Property is usually set only by servers. It usually lags server's current time by several minutes,
        /// so don't use this property for time synchronization.
        /// </remarks>
        public DateTime? ReferenceTimestamp { get { return GetDateTime64(16); } set { SetDateTime64(16, value); } }

        /// <summary>
        /// Gets or sets the time when the client sent its request.
        /// </summary>
        /// <value>
        /// This property is <c>null</c> in request packets.
        /// In reply packets, it is the time when the client sent its request.
        /// Servers copy this value from <see cref="P:GuerrillaNtp.NtpPacket.TransmitTimestamp" />
        /// that they find in received request packet.
        /// </value>
        /// <seealso cref="P:GuerrillaNtp.NtpPacket.CorrectionOffset" />
        /// <seealso cref="P:GuerrillaNtp.NtpPacket.RoundTripTime" />
        public DateTime? OriginTimestamp { get { return GetDateTime64(24); } set { SetDateTime64(24, value); } }

        /// <summary>
        /// Gets or sets the time when the request was received by the server.
        /// </summary>
        /// <value>
        /// This property is <c>null</c> in request packets.
        /// In reply packets, it is the time when the server received client request.
        /// </value>
        /// <seealso cref="P:GuerrillaNtp.NtpPacket.CorrectionOffset" />
        /// <seealso cref="P:GuerrillaNtp.NtpPacket.RoundTripTime" />
        public DateTime? ReceiveTimestamp { get { return GetDateTime64(32); } set { SetDateTime64(32, value); } }

        /// <summary>
        /// Gets or sets the time when the packet was sent.
        /// </summary>
        /// <value>
        /// Time when the packet was sent. It should never be <c>null</c>.
        /// Default value is <see cref="P:System.DateTime.UtcNow" />.
        /// </value>
        /// <remarks>
        /// This property must be set by both clients and servers.
        /// </remarks>
        /// <seealso cref="P:GuerrillaNtp.NtpPacket.CorrectionOffset" />
        /// <seealso cref="P:GuerrillaNtp.NtpPacket.RoundTripTime" />
        public DateTime? TransmitTimestamp { get { return GetDateTime64(40); } set { SetDateTime64(40, value); } }

        /// <summary>
        /// Gets or sets the time of reception of response SNTP packet on the client.
        /// </summary>
        /// <value>
        /// Time of reception of response SNTP packet on the client. It is <c>null</c> in request packets.
        /// </value>
        /// <remarks>
        /// This property is not part of the protocol.
        /// It is set by <see cref="T:GuerrillaNtp.NtpClient" /> when reply packet is received.
        /// </remarks>
        /// <seealso cref="P:GuerrillaNtp.NtpPacket.CorrectionOffset" />
        /// <seealso cref="P:GuerrillaNtp.NtpPacket.RoundTripTime" />
        public DateTime? DestinationTimestamp { get; set; }

        /// <summary>
        /// Gets the round-trip time to the server.
        /// </summary>
        /// <value>
        /// Time the request spent travelling to the server plus the time the reply spent travelling back.
        /// This is calculated from timestamps in the packet as <c>(t1 - t0) + (t3 - t2)</c>
        /// where t0 is <see cref="P:GuerrillaNtp.NtpPacket.OriginTimestamp" />,
        /// t1 is <see cref="P:GuerrillaNtp.NtpPacket.ReceiveTimestamp" />,
        /// t2 is <see cref="P:GuerrillaNtp.NtpPacket.TransmitTimestamp" />,
        /// and t3 is <see cref="P:GuerrillaNtp.NtpPacket.DestinationTimestamp" />.
        /// This property throws an exception in request packets.
        /// </value>
        /// <exception cref="T:GuerrillaNtp.NtpException">Thrown when one of the required timestamps is not present.</exception>
        /// <seealso cref="P:GuerrillaNtp.NtpPacket.OriginTimestamp" />
        /// <seealso cref="P:GuerrillaNtp.NtpPacket.ReceiveTimestamp" />
        /// <seealso cref="P:GuerrillaNtp.NtpPacket.TransmitTimestamp" />
        /// <seealso cref="P:GuerrillaNtp.NtpPacket.DestinationTimestamp" />
        /// <seealso cref="P:GuerrillaNtp.NtpPacket.CorrectionOffset" />
        public TimeSpan RoundTripTime
        {
            get
            {
                CheckTimestamps();
                return (ReceiveTimestamp.Value - OriginTimestamp.Value) + (DestinationTimestamp.Value - TransmitTimestamp.Value);
            }
        }

        /// <summary>
        /// Gets the offset that should be added to local time to synchronize it with server time.
        /// </summary>
        /// <value>
        /// Time difference between server and client. It should be added to local time to get server time.
        /// It is calculated from timestamps in the packet as <c>0.5 * ((t1 - t0) - (t3 - t2))</c>
        /// where t0 is <see cref="P:GuerrillaNtp.NtpPacket.OriginTimestamp" />,
        /// t1 is <see cref="P:GuerrillaNtp.NtpPacket.ReceiveTimestamp" />,
        /// t2 is <see cref="P:GuerrillaNtp.NtpPacket.TransmitTimestamp" />,
        /// and t3 is <see cref="P:GuerrillaNtp.NtpPacket.DestinationTimestamp" />.
        /// This property throws an exception in request packets.
        /// </value>
        /// <exception cref="T:GuerrillaNtp.NtpException">Thrown when one of the required timestamps is not present.</exception>
        /// <seealso cref="M:GuerrillaNtp.NtpClient.GetCorrectionOffset" />
        /// <seealso cref="P:GuerrillaNtp.NtpPacket.OriginTimestamp" />
        /// <seealso cref="P:GuerrillaNtp.NtpPacket.ReceiveTimestamp" />
        /// <seealso cref="P:GuerrillaNtp.NtpPacket.TransmitTimestamp" />
        /// <seealso cref="P:GuerrillaNtp.NtpPacket.DestinationTimestamp" />
        /// <seealso cref="P:GuerrillaNtp.NtpPacket.RoundTripTime" />
        public TimeSpan CorrectionOffset
        {
            get
            {
                CheckTimestamps();
                return TimeSpan.FromTicks(((ReceiveTimestamp.Value - OriginTimestamp.Value) - (DestinationTimestamp.Value - TransmitTimestamp.Value)).Ticks / 2);
            }
        }

        /// <summary>
        /// Initializes default request packet.
        /// </summary>
        /// <remarks>
        /// Created request packet can be passed to <see cref="M:GuerrillaNtp.NtpClient.Query(GuerrillaNtp.NtpPacket)" />.
        /// Properties <see cref="P:GuerrillaNtp.NtpPacket.Mode" /> and <see cref="P:GuerrillaNtp.NtpPacket.VersionNumber" />
        /// are set appropriately for request packet. Property <see cref="P:GuerrillaNtp.NtpPacket.TransmitTimestamp" />
        /// is set to <see cref="P:System.DateTime.UtcNow" />.
        /// </remarks>
        /// <seealso cref="M:GuerrillaNtp.NtpClient.Query(GuerrillaNtp.NtpPacket)" />
        public NtpPacket()
            : this(new byte[48])
        {
            Mode = NtpMode.Client;
            VersionNumber = 4;
            TransmitTimestamp = DateTime.UtcNow;
        }

        internal NtpPacket(byte[] bytes)
        {
            if (bytes.Length < 48)
                throw new NtpException(null, "SNTP reply packet must be at least 48 bytes long.");
            Bytes = bytes;
        }

        internal void ValidateRequest()
        {
            if (Mode != NtpMode.Client)
                throw new NtpException(this, "This is not a request SNTP packet.");
            if (VersionNumber == 0)
                throw new NtpException(this, "Protocol version of the request is not specified.");
            if (TransmitTimestamp == null)
                throw new NtpException(this, "TransmitTimestamp must be set in request packet.");
        }

        internal void ValidateReply(NtpPacket request)
        {
            if (Mode != NtpMode.Server)
                throw new NtpException(this, "This is not a reply SNTP packet.");
            if (VersionNumber == 0)
                throw new NtpException(this, "Protocol version of the reply is not specified.");
            if (Stratum == 0)
                throw new NtpException(this, String.Format("Received Kiss-o'-Death SNTP packet with code 0x{0:x}.", ReferenceId));
            if (LeapIndicator == NtpLeapIndicator.AlarmCondition)
                throw new NtpException(this, "SNTP server has unsynchronized clock.");
            CheckTimestamps();
            if (OriginTimestamp != request.TransmitTimestamp)
                throw new NtpException(this, "Origin timestamp in reply doesn't match transmit timestamp in request.");
        }

        void CheckTimestamps()
        {
            if (OriginTimestamp == null)
                throw new NtpException(this, "Origin timestamp is missing.");
            if (ReceiveTimestamp == null)
                throw new NtpException(this, "Receive timestamp is missing.");
            if (TransmitTimestamp == null)
                throw new NtpException(this, "Transmit timestamp is missing.");
            if (DestinationTimestamp == null)
                throw new NtpException(this, "Destination timestamp is missing.");
        }

        DateTime? GetDateTime64(int offset)
        {
            var field = GetUInt64BE(offset);
            if (field == 0)
                return null;
            return new DateTime(epoch.Ticks + Convert.ToInt64(field * (1.0 / (1L << 32) * 10000000.0)));
        }
        void SetDateTime64(int offset, DateTime? value) { SetUInt64BE(offset, value == null ? 0 : Convert.ToUInt64((value.Value.Ticks - epoch.Ticks) * (0.0000001 * (1L << 32)))); }
        TimeSpan GetTimeSpan32(int offset) { return TimeSpan.FromSeconds(GetInt32BE(offset) / (double)(1 << 16)); }
        ulong GetUInt64BE(int offset) { return SwapEndianness(BitConverter.ToUInt64(Bytes, offset)); }
        void SetUInt64BE(int offset, ulong value) { Array.Copy(BitConverter.GetBytes(SwapEndianness(value)), 0, Bytes, offset, 8); }
        int GetInt32BE(int offset) { return (int)GetUInt32BE(offset); }
        uint GetUInt32BE(int offset) { return SwapEndianness(BitConverter.ToUInt32(Bytes, offset)); }
        static uint SwapEndianness(uint x) { return ((x & 0xff) << 24) | ((x & 0xff00) << 8) | ((x & 0xff0000) >> 8) | ((x & 0xff000000) >> 24); }
        static ulong SwapEndianness(ulong x) { return ((ulong)SwapEndianness((uint)x) << 32) | SwapEndianness((uint)(x >> 32)); }
    }
}