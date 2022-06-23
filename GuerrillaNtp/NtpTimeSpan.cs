// Part of GuerrillaNtp: https://guerrillantp.machinezoo.com
using System;
using System.Buffers.Binary;

namespace GuerrillaNtp
{
    // SNTP duration is a 32-bit signed fixed-point number with 16 bits of precision.
    static class NtpTimeSpan
    {
        static TimeSpan Decode(int bits) => TimeSpan.FromSeconds(bits / (double)(1 << 16));
        static int Encode(TimeSpan time) => (int)(time.TotalSeconds * (1 << 16));
        public static TimeSpan Read(ReadOnlySpan<byte> buffer) => Decode(BinaryPrimitives.ReadInt32BigEndian(buffer));
        public static void Write(Span<byte> buffer, TimeSpan time) => BinaryPrimitives.WriteInt32BigEndian(buffer, Encode(time));
    }
}
