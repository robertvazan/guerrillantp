// Part of GuerrillaNtp: https://guerrillantp.machinezoo.com
using System;

namespace GuerrillaNtp
{
    /// <summary>
    /// RFC4330 SNTP response.
    /// </summary>
    /// <remarks>
    /// This is a low-level API for examining SNTP responses extracted from <see cref="NtpPacket" />.
    /// Most applications should just use <see cref="NtpClient.Query()" /> and properties in <see cref="NtpTime" />.
    /// In addition to fields found in <see cref="NtpPacket" />, this object also carries <see cref="DestinationTimestamp" />,
    /// which is essential to calculation of <see cref="NtpTime" />.
    /// </remarks>
    /// <seealso cref="NtpTime" />
    /// <seealso cref="NtpPacket" />
    /// <seealso cref="NtpRequest" />
    public record NtpResponse
    {
        /// <summary>
        /// Leap second indicator.
        /// </summary>
        /// <value>
        /// Leap second warning, if any. Defaults to <see cref="NtpLeapIndicator.NoWarning" />.
        /// Special value <see cref="NtpLeapIndicator.AlarmCondition" /> indicates unsynchronized server clock.
        /// </value>
        public NtpLeapIndicator LeapIndicator { get; init; } = NtpLeapIndicator.NoWarning;

        /// <summary>
        /// Server's distance from reference clock.
        /// </summary>
        /// <value>
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
        /// </value>
        public int Precision { get; init; } = 0;

        /// <summary>
        /// Total round-trip delay from the server to the reference clock.
        /// </summary>
        /// <value>
        /// Round-trip delay to the reference clock. Normally a positive value smaller than one second.
        /// </value>
        public TimeSpan RootDelay { get; init; } = TimeSpan.Zero;

        /// <summary>
        /// Estimated error in time reported by the server.
        /// </summary>
        /// <value>
        /// Estimated error in reported time. Normally a positive value smaller than one second.
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
        /// </value>
        public uint ReferenceId { get; init; } = 0;

        /// <summary>
        /// Time when the server clock was last set or corrected.
        /// </summary>
        /// <value>
        /// UTC time when the server clock was last set or corrected. Null when not specified.
        /// </value>
        public DateTime? ReferenceTimestamp { get; init; } = null;

        /// <summary>
        /// Time when the client sent its request.
        /// </summary>
        /// <value>
        /// UTC time when client sent its request.
        /// Servers copy this value from request's <see cref="NtpRequest.TransmitTimestamp" />.
        /// </value>
        public DateTime OriginTimestamp { get; init; }

        /// <summary>
        /// Time when the request was received by the server.
        /// </summary>
        /// <value>
        /// UTC time when the server received client's request.
        /// </value>
        public DateTime ReceiveTimestamp { get; init; }

        /// <summary>
        /// Time when the response was sent.
        /// </summary>
        /// <value>
        /// UTC time when the server sent its response.
        /// </value>
        public DateTime TransmitTimestamp { get; init; }

        /// <summary>
        /// Time when the response was received.
        /// </summary>
        /// <value>
        /// UTC time when the response was received by the client.
        /// </value>
        /// <remarks>
        /// This property is not part of the protocol. It is added when response packet is received.
        /// </remarks>
        public DateTime DestinationTimestamp { get; init; }

        /// <summary>
        /// Converts <see cref="NtpPacket" /> to <see cref="NtpResponse" />.
        /// </summary>
        /// <param name="packet">Packet that encodes the response.</param>
        /// <param name="time">
        /// UTC time when the response was received. It will be assigned to <see cref="DestinationTimestamp" />.
        /// </param>
        /// <returns>SNTP response found in the packet.</returns>
        /// <exception cref="NtpException">
        /// Thrown when the packet does not encode valid SNTP response or reception time is not in UTC.
        /// </exception>
        public static NtpResponse FromPacket(NtpPacket packet, DateTime time)
        {
            packet.Validate();
            if (packet.Mode != NtpMode.Server)
                throw new NtpException("Not a response packet.");
            if (packet.OriginTimestamp == null)
                throw new NtpException("Origin timestamp is missing.");
            if (packet.ReceiveTimestamp == null)
                throw new NtpException("Receive timestamp is missing.");
            if (packet.TransmitTimestamp == null)
                throw new NtpException("Transmit timestamp is missing.");
            if (time.Kind != DateTimeKind.Utc)
                throw new NtpException("Destination timestamp must have UTC timezone.");
            return new NtpResponse
            {
                LeapIndicator = packet.LeapIndicator,
                Stratum = packet.Stratum,
                PollInterval = packet.PollInterval,
                Precision = packet.Precision,
                RootDelay = packet.RootDelay,
                RootDispersion = packet.RootDispersion,
                ReferenceId = packet.ReferenceId,
                ReferenceTimestamp = packet.ReferenceTimestamp,
                OriginTimestamp = packet.OriginTimestamp.Value,
                ReceiveTimestamp = packet.ReceiveTimestamp.Value,
                TransmitTimestamp = packet.TransmitTimestamp.Value,
                DestinationTimestamp = time,
            };
        }

        /// <summary>
        /// Converts just received <see cref="NtpPacket" /> to <see cref="NtpResponse" />.
        /// </summary>
        /// <param name="packet">Packet that encodes the response.</param>
        /// <returns>
        /// SNTP response found in the packet. <see cref="DestinationTimestamp" /> is set to <see cref="DateTime.UtcNow" />.
        /// </returns>
        /// <exception cref="NtpException">
        /// Thrown when the packet does not encode valid SNTP response.
        /// </exception>
        public static NtpResponse FromPacket(NtpPacket packet) => FromPacket(packet, DateTime.UtcNow);

        /// <summary>
        /// Validates the response and converts it to <see cref="NtpPacket" />.
        /// </summary>
        /// <returns>Valid SNTP packet encoding the response.</returns>
        /// <exception cref="NtpException">
        /// Thrown if this is not a valid SNTP response.
        /// </exception>
        public NtpPacket ToPacket()
        {
            var packet = new NtpPacket
            {
                Mode = NtpMode.Server,
                LeapIndicator = LeapIndicator,
                Stratum = Stratum,
                PollInterval = PollInterval,
                Precision = Precision,
                RootDelay = RootDelay,
                RootDispersion = RootDispersion,
                ReferenceId = ReferenceId,
                ReferenceTimestamp = ReferenceTimestamp,
                OriginTimestamp = OriginTimestamp,
                ReceiveTimestamp = ReceiveTimestamp,
                TransmitTimestamp = TransmitTimestamp,
            };
            packet.Validate();
            return packet;
        }

        /// <summary>
        /// Checks whether this object describes valid SNTP response.
        /// </summary>
        /// <exception cref="NtpException">
        /// Thrown if this is not a valid SNTP response.
        /// </exception>
        /// <remarks>
        /// Object properties do not perform validation. Call this method to validate the response.
        /// <see cref="FromPacket(NtpPacket)" /> and <see cref="ToPacket()" /> perform validation automatically.
        /// </remarks>
        public void Validate()
        {
            ToPacket();
            if (DestinationTimestamp.Kind != DateTimeKind.Utc)
                throw new NtpException("Destination timestamp must have UTC timezone.");
        }

        /// <summary>
        /// Check whether this is a response to the given request.
        /// </summary>
        /// <param name="request">Request that this response might be answering.</param>
        /// <returns>True if this appears to be a response to the request, false otherwise.</returns>
        public bool Matches(NtpRequest request)
        {
            // Tolerate rounding errors on both sides.
            return Math.Abs((OriginTimestamp - request.TransmitTimestamp).TotalSeconds) < 0.000_001;
        }
    }
}
