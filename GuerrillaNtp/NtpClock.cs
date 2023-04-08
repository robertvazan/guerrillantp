// Part of GuerrillaNtp: https://guerrillantp.machinezoo.com
using System;

namespace GuerrillaNtp
{
    /// <summary>
    /// NTP-synchronized time source.
    /// </summary>
    /// <remarks>
    /// Application can obtain <see cref="NtpClock" /> by calling <see cref="NtpClient.Query()" />.
    /// <see cref="NtpClock" /> can be also constructed directly from <see cref="NtpResponse" />.
    /// Network time is then available from <see cref="UtcNow" /> and <see cref="Now" /> without further network communication.
    /// Application can also calculate network time on their own using <see cref="CorrectionOffset" />.
    /// </remarks>
    public record NtpClock(NtpResponse Response)
    {
        /// <summary>
        /// SNTP response used to calculate network time.
        /// </summary>
        /// <value>
        /// Valid SNTP response. Non-fatal response issues are tolerated and reported via <see cref="Synchronized" />.
        /// </value>
        /// <remarks>
        /// All properties of <see cref="NtpClock" /> are calculated from information in the response.
        /// You can find additional detail in the response, including information about accuracy,
        /// leap second, and server's preferred poll interval.
        /// </remarks>
        public NtpResponse Response { get; init; } = Response;

        /// <summary>
        /// Indicates whether reported time is really synchronized.
        /// </summary>
        /// <value>
        /// True if time reported by this object is synchronized via NTP, false otherwise.
        /// </value>
        /// <remarks>
        /// Time might be unsynchronized even after response is successfully received from NTP server.
        /// The response might not contain valid network time, for example in the case
        /// of Kiss-o'-Death packet, leap indicator set to alarm condition,
        /// or other fields indicating NTP server itself is not synchronized.
        /// Consult this property to check whether time reported by other properties of this object can be trusted.
        /// </remarks>
        public bool Synchronized
        {
            get
            {
                if (Response.LeapIndicator == NtpLeapIndicator.AlarmCondition)
                    return false;
                if (Response.Stratum == 0)
                    return false;
                if (Response.RootDelay.TotalSeconds > 1)
                    return false;
                if (Response.RootDispersion.TotalSeconds > 1)
                    return false;
                if (RoundTripTime.TotalSeconds > 1)
                    return false;
                return true;
            }
        }

        /// <summary>
        /// Time offset that should be added to local time to calculate network time.
        /// </summary>
        /// <value>
        /// Time difference between server and client. It is positive when local time lags behind network time
        /// and negative when local time is ahead of network time.
        /// </value>
        /// <remarks>
        /// Correction offset is calculated from timestamps in the response as <c>0.5 * ((t1 - t0) - (t3 - t2))</c>
        /// where t0 is <see cref="NtpResponse.OriginTimestamp" />,
        /// t1 is <see cref="NtpResponse.ReceiveTimestamp" />,
        /// t2 is <see cref="NtpResponse.TransmitTimestamp" />,
        /// and t3 is <see cref="NtpResponse.DestinationTimestamp" />.
        /// </remarks>
        /// <seealso cref="Now" />
        /// <seealso cref="UtcNow"/>
        public TimeSpan CorrectionOffset
        {
#if NET5_0_OR_GREATER
            get => 0.5 * ((Response.ReceiveTimestamp - Response.OriginTimestamp) - (Response.DestinationTimestamp - Response.TransmitTimestamp));
#else
            get => TimeSpan.FromTicks((long)(0.5 * ((Response.ReceiveTimestamp - Response.OriginTimestamp) - (Response.DestinationTimestamp - Response.TransmitTimestamp)).Ticks));
#endif
        }

        /// <summary>
        /// NTP time in UTC timezone.
        /// </summary>
        /// <value>
        /// NTP time in UTC timezone calculated as <see cref="DateTimeOffset.UtcNow"/> + <see cref="CorrectionOffset"/>.
        /// </value>
        /// <remarks>
        /// This property returns NTP time as <see cref="DateTimeOffset"/>.
        /// Use its <see cref="DateTimeOffset.UtcDateTime"/> property To obtain NTP time as <see cref="DateTime"/>.
        /// </remarks>
        /// <seealso cref="Now" />
        /// <seealso cref="CorrectionOffset"/>
        public DateTimeOffset UtcNow => DateTimeOffset.UtcNow + CorrectionOffset;

        /// <summary>
        /// NTP time in local timezone.
        /// </summary>
        /// <value>
        /// NTP time in local timezone calculated as <see cref="DateTimeOffset.Now"/> + <see cref="CorrectionOffset"/>.
        /// </value>
        /// <remarks>
        /// This property returns NTP time as <see cref="DateTimeOffset"/>.
        /// Use its <see cref="DateTimeOffset.LocalDateTime"/> property To obtain NTP time as <see cref="DateTime"/>.
        /// </remarks>
        /// <seealso cref="UtcNow"/>
        /// <seealso cref="CorrectionOffset" />
        public DateTimeOffset Now => DateTimeOffset.Now + CorrectionOffset;

        /// <summary>
        /// Round-trip time to the server.
        /// </summary>
        /// <value>
        /// Time the request spent travelling to the server plus the time the reply spent travelling back.
        /// This time can be negative if clock skew occured on the client while NTP server was queried.
        /// </value>
        /// <remarks>
        /// Round-trip time is calculated from timestamps in the packet as <c>(t1 - t0) + (t3 - t2)</c>
        /// where t0 is <see cref="NtpResponse.OriginTimestamp" />,
        /// t1 is <see cref="NtpResponse.ReceiveTimestamp" />,
        /// t2 is <see cref="NtpResponse.TransmitTimestamp" />,
        /// and t3 is <see cref="NtpResponse.DestinationTimestamp" />.
        /// </remarks>
        public TimeSpan RoundTripTime
        {
            get => (Response.ReceiveTimestamp - Response.OriginTimestamp) + (Response.DestinationTimestamp - Response.TransmitTimestamp);
        }

        /// <summary>
        /// Unsynchronized fallback time source.
        /// </summary>
        /// <value>
        /// An instance of <see cref="NtpClock" /> with zero <see cref="CorrectionOffset" />
        /// and with <see cref="Synchronized" /> returning false.
        /// </value>
        /// <remarks>
        /// You can use this fallback when <see cref="NtpClient.Last" /> is null.
        /// In C#, that would be <see cref="NtpClient.Last" /> ?? <see cref="LocalFallback" />.
        /// </remarks>
        public static readonly NtpClock LocalFallback;

        static NtpClock()
        {
            var time = DateTime.UtcNow;
            LocalFallback = new NtpClock(new NtpResponse
            {
                LeapIndicator = NtpLeapIndicator.AlarmCondition,
                OriginTimestamp = time,
                ReceiveTimestamp = time,
                TransmitTimestamp = time,
                DestinationTimestamp = time,
            });
        }
    }
}
