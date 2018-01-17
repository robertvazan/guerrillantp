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
                using (var ntp = new NtpClient(Dns.GetHostAddresses(host)[0]))
                {
                    ntp.Timeout = TimeSpan.FromSeconds(5);
                    var packet = ntp.Query();
                    Console.WriteLine(host);
                    Console.WriteLine("-------------------------------------");
                    Console.WriteLine("Correction offset: {0}", packet.CorrectionOffset);
                    Console.WriteLine("RTT:               {0}", packet.RoundTripTime);
                    Console.WriteLine("Origin time:       {0:hh:mm:ss.fff}", packet.OriginTimestamp);
                    Console.WriteLine("Receive time:      {0:hh:mm:ss.fff}", packet.ReceiveTimestamp);
                    Console.WriteLine("Transmit time:     {0:hh:mm:ss.fff}", packet.TransmitTimestamp);
                    Console.WriteLine("Destination time:  {0:hh:mm:ss.fff}", packet.DestinationTimestamp);
                }
            }
        }
    }
}
