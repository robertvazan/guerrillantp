// Part of GuerrillaNtp: https://guerrillantp.machinezoo.com
using GuerrillaNtp;
using System;
using System.Net;

namespace NtpCommand
{
    class Program
    {
        static void Main(string[] args)
        {
            var servers = args.Length > 0 ? args : new[] { "pool.ntp.org" };
            foreach (var host in servers)
            {
                Console.WriteLine("Querying {0}...", host);
                try
                {
                    using (var ntp = new NtpClient(Dns.GetHostAddresses(host)[0]))
                    {
                        var packet = ntp.Query();
                        Console.WriteLine();
                        Console.WriteLine("Received {0}B packet", packet.Bytes.Length);
                        Console.WriteLine("-------------------------------------");
                        Console.WriteLine("Correction offset: {0:s'.'FFFFFFF}", packet.CorrectionOffset);
                        Console.WriteLine("Round-trip time:   {0:s'.'FFFFFFF}", packet.RoundTripTime);
                        Console.WriteLine("Origin time:       {0:hh:mm:ss.fff}", packet.OriginTimestamp);
                        Console.WriteLine("Receive time:      {0:hh:mm:ss.fff}", packet.ReceiveTimestamp);
                        Console.WriteLine("Transmit time:     {0:hh:mm:ss.fff}", packet.TransmitTimestamp);
                        Console.WriteLine("Destination time:  {0:hh:mm:ss.fff}", packet.DestinationTimestamp);
                        Console.WriteLine("Protocol version:  {0}", packet.VersionNumber);
                        Console.WriteLine("Protocol mode:     {0}", packet.Mode);
                        Console.WriteLine("Leap second:       {0}", packet.LeapIndicator);
                        Console.WriteLine("Stratum:           {0}", packet.Stratum);
                        Console.WriteLine("Reference ID:      0x{0:x}", packet.ReferenceId);
                        Console.WriteLine("Reference time:    {0:hh:mm:ss.fff}", packet.ReferenceTimestamp);
                        Console.WriteLine("Root delay:        {0}ms", packet.RootDelay.TotalMilliseconds);
                        Console.WriteLine("Root dispersion:   {0}ms", packet.RootDispersion.TotalMilliseconds);
                        Console.WriteLine("Poll interval:     2^{0}s", packet.Poll);
                        Console.WriteLine("Precision:         2^{0}s", packet.Precision);
                        Console.WriteLine();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine();
                    Console.WriteLine("NTP query failed: {0}", ex.Message);
                }
            }
        }
    }
}
