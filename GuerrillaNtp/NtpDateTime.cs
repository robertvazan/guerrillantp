// Part of GuerrillaNtp: https://guerrillantp.machinezoo.com
using System;
using System.Buffers.Binary;

namespace GuerrillaNtp
{
    // SNTP timestamp is a 64-bit signed fixed-point number with 32 bits of precision.
    static class NtpDateTime
    {
        private const double FACTOR = 1L << 32;

        // SNTP epochs start every 2^32 seconds. The current one is in year 2036.
        // Calculations below will work for timestamps within +/- 68 years of the epoch,
        // which fortunately covers machines that just booted with time reset to unix epoch of 1970.
        // This code must be updated sometime after year 2070 as year 2104 approaches.
        static readonly DateTime epoch = new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc) + TimeSpan.FromSeconds(1L << 32);
        static DateTime? Decode(long bits)
#if NET5_0_OR_GREATER
             => bits == 0
            ? null
            : epoch + TimeSpan.FromSeconds(bits / FACTOR);
#else
             => bits == 0
            ? null
            : epoch.AddTicks((long)(bits / FACTOR * 1000 * TimeSpan.TicksPerMillisecond));
#endif

        static long Encode(DateTime? time) => time == null ? 0 : Convert.ToInt64((time.Value - epoch).TotalSeconds * (1L << 32));
        public static DateTime? Read(ReadOnlySpan<byte> buffer) => Decode(BinaryPrimitives.ReadInt64BigEndian(buffer));
        public static void Write(Span<byte> buffer, DateTime? time) => BinaryPrimitives.WriteInt64BigEndian(buffer, Encode(time));
    }
}
