// Part of GuerrillaNtp: https://guerrillantp.machinezoo.com
using System;
using System.Buffers.Binary;

namespace GuerrillaNtp
{

    /// <summary>
    /// RFC4330 SNTP packet used for communication to and from a network time server.
    /// </summary>
    /// <remarks>
    /// The same data structure represents both request and reply packets.
    /// Classes <see cref="NtpRequest" /> and <see cref="NtpResponse" />
    /// provide highewr level representation specialized for requests and responses.
    /// Most applications should just use <see cref="NtpClient.Query()" /> and properties in <see cref="NtpClock" />.
    /// </remarks>
    /// <seealso cref="NtpRequest" />
    /// <seealso cref="NtpResponse" />
    public record NtpPacket
    {
        /// <summary>
        /// Leap second indicator.
        /// </summary>
        /// <value>
        /// Leap second warning, if any. Defaults to <see cref="NtpLeapIndicator.NoWarning" />.
        /// Special value <see cref="NtpLeapIndicator.AlarmCondition" /> indicates unsynchronized server clock.
        /// Response-only property. Leave on default in requests.
        /// </value>
        public NtpLeapIndicator LeapIndicator { get; init; } = NtpLeapIndicator.NoWarning;

        /// <summary>
        /// SNTP protocol version number.
        /// </summary>
        /// <value>
        /// SNTP protocol version. Defaults to 4, which is the latest version at the time of writing.
        /// </value>
        /// <remarks>
        /// Servers usually reply with the same protocol version.
        /// </remarks>
        public int VersionNumber { get; init; } = 4;

        /// <summary>
        /// SNTP packet mode, i.e. client or server.
        /// </summary>
        /// <value>
        /// SNTP packet mode. Defaults to <see cref="NtpMode.Client" />, indicating request packet.
        /// Server response should have this property set to <see cref="NtpMode.Server" />.
        /// </value>
        public NtpMode Mode { get; init; } = NtpMode.Client;

        /// <summary>
        /// Server's distance from reference clock.
        /// </summary>
        /// <value>
        /// Response-only property. Leave zeroed in requests.
        /// Value 1 indicates primary source connected to hardware clock.
        /// Values 2-15 indicate increasing number of hops from primary source.
        /// Special value 0 indicates that this packet is a Kiss-o'-Death message
        /// with kiss code stored in <see cref="ReferenceId" />.
        /// </value>
        public int Stratum { get; init; } = 0;

        /// <summary>
        /// Server's preferred polling interval.
        /// </summary>
        /// <value>
        /// Polling interval in log₂ seconds, e.g. 4 stands for 16s and 17 means 131,072s.
        /// Response-only property. Leave zeroed in requests.
        /// </value>
        /// <remarks>
        /// <see cref="NtpClient" /> does not enforce the polling interval.
        /// It is application responsibility to be a good netizen and respect server's policy.
        /// </remarks>
        public int PollInterval { get; init; } = 0;

        /// <summary>
        /// Precision of server clock.
        /// </summary>
        /// <value>
        /// Clock precision in log₂ seconds, e.g. -19 for at least microsecond precision.
        /// Response-only property. Leave zeroed in requests.
        /// </value>
        public int Precision { get; init; } = 0;

        /// <summary>
        /// Total round-trip delay from the server to the reference clock.
        /// </summary>
        /// <value>
        /// Round-trip delay to the reference clock. Normally a positive value smaller than one second.
        /// Response-only property. Leave zeroed in requests.
        /// </value>
        public TimeSpan RootDelay { get; init; } = TimeSpan.Zero;

        /// <summary>
        /// Estimated error in time reported by the server.
        /// </summary>
        /// <value>
        /// Estimated error in reported time. Normally a positive value smaller than one second.
        /// Response-only property. Leave zeroed in requests.
        /// </value>
        public TimeSpan RootDispersion { get; init; } = TimeSpan.Zero;

        /// <summary>
        /// ID of the time source used by the server or Kiss-o'-Death code.
        /// </summary>
        /// <value>
        /// Stratum 1 servers write here one of several special values that describe the kind of hardware clock they use.
        /// Stratum 2 and lower servers set this property to IPv4 address of their upstream server.
        /// If upstream server has IPv6 address, the address is hashed, because it doesn't fit in this property.
        /// When server sets <see cref="Stratum" /> to special value 0,
        /// this property contains so called kiss code that instructs the client to stop querying the server.
        /// Response-only property. Leave zeroed in requests.
        /// </value>
        public uint ReferenceId { get; init; } = 0;

        /// <summary>
        /// Time when the server clock was last set or corrected.
        /// </summary>
        /// <value>
        /// UTC time when the server clock was last set or corrected. Null when not specified.
        /// Response-only property. Leave nulled in requests.
        /// </value>
        public DateTime? ReferenceTimestamp { get; init; } = null;

        /// <summary>
        /// Time when the client sent its request.
        /// </summary>
        /// <value>
        /// In response packet, this is the UTC time when client sent its request.
        /// Servers copy this value from request's <see cref="TransmitTimestamp" />.
        /// Response-only property. Leave nulled in requests.
        /// </value>
        public DateTime? OriginTimestamp { get; init; } = null;

        /// <summary>
        /// Time when the request was received by the server.
        /// </summary>
        /// <value>
        /// UTC time when the server received client's request.
        /// Response-only property. Leave nulled in requests.
        /// </value>
        public DateTime? ReceiveTimestamp { get; init; } = null;

        /// <summary>
        /// Time when the packet was sent.
        /// </summary>
        /// <value>
        /// UTC time when the packet was sent. Both client and server set this property.
        /// Default value is <see cref="DateTime.UtcNow" />.
        /// This property can be technically null, but doing so is not recommended.
        /// </value>
        public DateTime? TransmitTimestamp { get; init; } = DateTime.UtcNow;

        /// <summary>
        /// Checks whether this object describes valid SNTP packet.
        /// </summary>
        /// <exception cref="NtpException">
        /// Thrown if this is not a valid SNTP packet.
        /// </exception>
        /// <remarks>
        /// Object properties do not perform validation. Call this method to validate the packet.
        /// <see cref="FromBytes(byte[], int)" /> and <see cref="ToBytes()" /> perform validation automatically.
        /// </remarks>
        public void Validate()
        {
            if (VersionNumber < 1 || VersionNumber > 7)
                throw new NtpException("Invalid SNTP protocol version.");

#if NET5_0_OR_GREATER
            if (!Enum.IsDefined(LeapIndicator))
                throw new NtpException("Invalid leap second indicator value.");

            if (!Enum.IsDefined(Mode))
                throw new NtpException("Invalid NTP protocol mode.");
#else
            if (!Enum.IsDefined(typeof(NtpLeapIndicator), LeapIndicator))
                throw new NtpException("Invalid leap second indicator value.");

            if (!Enum.IsDefined(typeof(NtpMode), Mode))
                throw new NtpException("Invalid NTP protocol mode.");
#endif

            if ((byte)Stratum != Stratum)
                throw new NtpException("Invalid stratum number.");
            if ((byte)PollInterval != PollInterval)
                throw new NtpException("Poll interval out of range.");
            if ((sbyte)Precision != Precision)
                throw new NtpException("Precision out of range.");
            if (Math.Abs(RootDelay.TotalSeconds) > 32000)
                throw new NtpException("Root delay out of range.");
            if (RootDispersion.Ticks < 0 || RootDispersion.TotalSeconds > 32000)
                throw new NtpException("Root dispersion out of range.");
            if (ReferenceTimestamp != null && ReferenceTimestamp.Value.Kind != DateTimeKind.Utc)
                throw new NtpException("Reference timestamp must have UTC timezone.");
            if (OriginTimestamp != null && OriginTimestamp.Value.Kind != DateTimeKind.Utc)
                throw new NtpException("Origin timestamp must have UTC timezone.");
            if (ReceiveTimestamp != null && ReceiveTimestamp.Value.Kind != DateTimeKind.Utc)
                throw new NtpException("Receive timestamp must have UTC timezone.");
            if (TransmitTimestamp != null && TransmitTimestamp.Value.Kind != DateTimeKind.Utc)
                throw new NtpException("Transmit timestamp must have UTC timezone.");
        }

        /// <summary>
        /// Parses and validates SNTP packet.
        /// </summary>
        /// <param name="buffer">
        /// RFC4330 SNTPv4 packet. Previous versions should be also parsed without issue.
        /// Extra bytes at the end of the buffer are ignored.
        /// </param>
        /// <param name="length">Number of bytes in the buffer that are actually filled with data.</param>
        /// <returns>
        /// Parsed SNTP packet. It has been already validated as if by calling <see cref="Validate()" />.
        /// </returns>
        /// <exception cref="NtpException">
        /// Thrown when the buffer does not contain valid SNTP packet.
        /// </exception>
        public static NtpPacket FromBytes(byte[] buffer, int length)
        {
            if (length < 48 || length > buffer.Length)
                throw new NtpException("NTP packet must be at least 48 bytes long.");
            var bytes = buffer.AsSpan();
            var packet = new NtpPacket
            {
                LeapIndicator = (NtpLeapIndicator)((buffer[0] & 0xC0) >> 6),
                VersionNumber = (buffer[0] & 0x38) >> 3,
                Mode = (NtpMode)(buffer[0] & 0x07),
                Stratum = buffer[1],
                PollInterval = buffer[2],
                Precision = (sbyte)buffer[3],
                RootDelay = NtpTimeSpan.Read(bytes[4..]),
                RootDispersion = NtpTimeSpan.Read(bytes[8..]),
                ReferenceId = BinaryPrimitives.ReadUInt32BigEndian(bytes[12..]),
                ReferenceTimestamp = NtpDateTime.Read(bytes[16..]),
                OriginTimestamp = NtpDateTime.Read(bytes[24..]),
                ReceiveTimestamp = NtpDateTime.Read(bytes[32..]),
                TransmitTimestamp = NtpDateTime.Read(bytes[40..]),
            };
            packet.Validate();
            return packet;
        }

        /// <summary>
        /// Validates and serializes the packet.
        /// </summary>
        /// <returns>
        /// Serialized RFC4330 SNTPv4 packet. Previous versions should be also serialized without issue.
        /// </returns>
        /// <exception cref="NtpException">
        /// Thrown when the packet fails validation as if <see cref="Validate()" /> was called.
        /// </exception>
        public byte[] ToBytes()
        {
            Validate();
            var buffer = new byte[48];
            var bytes = buffer.AsSpan();
            bytes[0] = (byte)(((uint)LeapIndicator << 6) | ((uint)VersionNumber << 3) | (uint)Mode);
            bytes[1] = (byte)Stratum;
            bytes[2] = (byte)PollInterval;
            bytes[3] = (byte)Precision;
            NtpTimeSpan.Write(bytes[4..], RootDelay);
            NtpTimeSpan.Write(bytes[8..], RootDispersion);
            BinaryPrimitives.WriteUInt32BigEndian(bytes[12..], ReferenceId);
            NtpDateTime.Write(bytes[16..], ReferenceTimestamp);
            NtpDateTime.Write(bytes[24..], OriginTimestamp);
            NtpDateTime.Write(bytes[32..], ReceiveTimestamp);
            NtpDateTime.Write(bytes[40..], TransmitTimestamp);
            return buffer;
        }
    }
}
