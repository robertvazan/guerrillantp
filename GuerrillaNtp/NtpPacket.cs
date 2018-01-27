// Part of GuerrillaNtp: https://guerrillantp.machinezoo.com
using System;

namespace GuerrillaNtp
{
    /// <summary>
    /// Represents a packet for communications to and from a network time server
    /// </summary>
    public class NtpPacket
    {
        static readonly DateTime epoch = new DateTime(1900, 1, 1);

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
        /// Gets or sets the version number
        /// </summary>
        public int VersionNumber
        {
            get { return (Bytes[0] & 0x38) >> 3; }
            set { Bytes[0] = (byte)((Bytes[0] & ~0x38) | value << 3); }
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
        /// Gets the server's distance from the reference clock
        /// </summary>
        public int Stratum { get { return Bytes[1]; } }

        /// <summary>
        /// Gets the polling interval (in log₂ seconds)
        /// </summary>
        public int Poll { get { return Bytes[2]; } }

        /// <summary>
        /// Gets the precision of the system clock (in log₂ seconds)
        /// </summary>
        public int Precision { get { return (sbyte)Bytes[3]; } }

        /// <summary>
        /// Gets the total round trip delay from the server to the reference clock
        /// </summary>
        public TimeSpan RootDelay { get { return GetTimeSpan32(4); } }

        /// <summary>
        /// Gets the amount of jitter the server observes in the reference clock
        /// </summary>
        public TimeSpan RootDispersion { get { return GetTimeSpan32(8); } }

        /// <summary>
        /// Gets the ID of the server or reference clock
        /// </summary>
        public uint ReferenceId { get { return GetUInt32BE(12); } }

        /// <summary>
        /// Gets the date and time the server was last set or corrected
        /// </summary>
        public DateTime? ReferenceTimestamp { get { return GetDateTime64(16); } set { SetDateTime64(16, value); } }

        /// <summary>
        /// When creating a response, the server copies the TransmitTimestamp of the request to the OriginTimestamp of the response.
        /// 
        /// Not used / null in request packets
        /// </summary>
        public DateTime? OriginTimestamp { get { return GetDateTime64(24); } set { SetDateTime64(24, value); } }

        /// <summary>
        /// Gets the date and time this packet was received by the server
        /// </summary>
        public DateTime? ReceiveTimestamp { get { return GetDateTime64(32); } set { SetDateTime64(32, value); } }

        /// <summary>
        /// Gets the date and time that the packet was transmitted from the client (in request packets) or from the server (in response packets)
        /// </summary>
        public DateTime? TransmitTimestamp { get { return GetDateTime64(40); } set { SetDateTime64(40, value); } }

        /// <summary>
        /// Gets or sets the time of reception of response NTP packet on the client.
        /// This property is not part of the protocol. It is set by NtpClient.
        /// </summary>
        public DateTime? DestinationTimestamp { get; set; }

        /// <summary>
        /// Time spent on the wire in both directions together
        /// </summary>
        public TimeSpan RoundTripTime
        {
            get
            {
                CheckTimestamps();
                return (ReceiveTimestamp.Value - OriginTimestamp.Value) + (DestinationTimestamp.Value - TransmitTimestamp.Value);
            }
        }

        /// <summary>
        /// Offset that should be added to local time to synchronize it with server time
        /// </summary>
        public TimeSpan CorrectionOffset
        {
            get
            {
                CheckTimestamps();
                return TimeSpan.FromTicks(((ReceiveTimestamp.Value - OriginTimestamp.Value) - (DestinationTimestamp.Value - TransmitTimestamp.Value)).Ticks / 2);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NtpPacket" /> class
        /// </summary>
        public NtpPacket()
            : this(new byte[48])
        {
            Mode = NtpMode.Client;
            VersionNumber = 4;
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