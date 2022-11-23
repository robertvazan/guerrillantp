// Part of GuerrillaNtp: https://guerrillantp.machinezoo.com
using System;
using System.Threading.Tasks;

namespace GuerrillaNtp.Cli
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var servers = args.Length > 0 ? args : new[] { "pool.ntp.org" };
            foreach (var host in servers)
            {
                Console.WriteLine("Querying {0}...", host);
                try
                {
                    var ntp = new NtpClient(servers[0]);
                    var time = await ntp.QueryAsync();
                    var response = time.Response;
                    Console.WriteLine();
                    Console.WriteLine("Received response");
                    Console.WriteLine("-------------------------------------");
                    Console.WriteLine("Synchronized:       {0}", time.Synchronized ? "yes" : "no");
                    Console.WriteLine("Network time (UTC): {0:HH:mm:ss.fff}", time.UtcNow);
                    Console.WriteLine("Network time:       {0:HH:mm:ss.fff}", time.Now);
                    Console.WriteLine("Correction offset:  {0:s'.'FFFFFFF}", time.CorrectionOffset);
                    Console.WriteLine("Round-trip time:    {0:s'.'FFFFFFF}", time.RoundTripTime);
                    Console.WriteLine("Origin time:        {0:HH:mm:ss.fff}", response.OriginTimestamp);
                    Console.WriteLine("Receive time:       {0:HH:mm:ss.fff}", response.ReceiveTimestamp);
                    Console.WriteLine("Transmit time:      {0:HH:mm:ss.fff}", response.TransmitTimestamp);
                    Console.WriteLine("Destination time:   {0:HH:mm:ss.fff}", response.DestinationTimestamp);
                    Console.WriteLine("Leap second:        {0}", response.LeapIndicator);
                    Console.WriteLine("Stratum:            {0}", response.Stratum);
                    Console.WriteLine("Reference ID:       0x{0:x}", response.ReferenceId);
                    Console.WriteLine("Reference time:     {0:HH:mm:ss.fff}", response.ReferenceTimestamp);
                    Console.WriteLine("Root delay:         {0}ms", response.RootDelay.TotalMilliseconds);
                    Console.WriteLine("Root dispersion:    {0}ms", response.RootDispersion.TotalMilliseconds);
                    Console.WriteLine("Poll interval:      2^{0}s", response.PollInterval);
                    Console.WriteLine("Precision:          2^{0}s", response.Precision);
                    Console.WriteLine();
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
