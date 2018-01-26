using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using NUnit.Framework;
using GuerrillaNtp;

namespace Tests
{
    [TestFixture]
    public class NtpClientTests
    {
        private IPAddress server = Dns.GetHostEntry("pool.ntp.org").AddressList[0];

        [Test]
        public void Test_can_get_correction_offset()
        {
            const int tries = 10;
            int hits = 0;
            using (var client = new NtpClient(server))
            {
                for (int i = 0; i < tries; i++)
                {
                    try
                    {
                        Console.WriteLine($"Offset #{i + 1}: {client.GetCorrectionOffset()}");
                        ++hits;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Offset #{i + 1}: {ex}");
                    }
                }
            }
            Console.WriteLine($"Got {hits} of {tries} replies");
            Assert.GreaterOrEqual(2 * hits, tries);
        }

        [Test]
        public void Test_Timeout_expires()
        {
            var timeout = TimeSpan.FromMilliseconds(500);

            // Note: pick a host that *drops* packets. The test will fail if the host merely *rejects* packets.
            using (var client = new NtpClient(IPAddress.Parse("8.8.8.8")))
            {
                client.Timeout = timeout;

                var timer = Stopwatch.StartNew();

                try
                {
                    client.GetCorrectionOffset();
                    Assert.Fail("Shouldn't get here. Expecting timeout!");
                }
                catch (SocketException ex) when (ex.ErrorCode == 10060 || ex.ErrorCode == 10035)
                {
                    // We expect a socket timeout error
                }

                timer.Stop();

                Assert.IsTrue(timer.Elapsed >= timeout, timer.Elapsed.ToString());
                Assert.IsTrue(timer.Elapsed < timeout + timeout, timer.Elapsed.ToString());
            }
        }
    }
}
