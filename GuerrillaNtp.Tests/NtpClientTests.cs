// Part of GuerrillaNtp: https://guerrillantp.machinezoo.com
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using NUnit.Framework;
using GuerrillaNtp;

namespace GuerrillaNtp.Tests
{
    [TestFixture]
    public class NtpClientTests
    {
        private IPAddress server = Dns.GetHostEntry("pool.ntp.org").AddressList[0];

        [Test]
        public void TestCorrectionOffset()
        {
            const int tries = 3;
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
            Assert.GreaterOrEqual(hits, 1);
        }

        [Test]
        public void TestTimeout()
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
                catch (SocketException ex) when (ex.ErrorCode == 10060 || ex.ErrorCode == 10035 || ex.ErrorCode == 110)
                {
                    // We expect a socket timeout error
                }

                timer.Stop();

                Assert.IsTrue(timer.Elapsed >= timeout, timer.Elapsed.ToString());
                Assert.IsTrue(timer.Elapsed < timeout + timeout + timeout, timer.Elapsed.ToString());
            }
        }
        [Test]
        public void TestTimeoutViaConstructor()
        {
            var timeout = TimeSpan.FromMilliseconds(500);

            // Note: pick a host that *drops* packets. The test will fail if the host merely *rejects* packets.
            using (var client = new NtpClient(IPAddress.Parse("8.8.8.8"), timeout))
            {
                var timer = Stopwatch.StartNew();

                try
                {
                    client.GetCorrectionOffset();
                    Assert.Fail("Shouldn't get here. Expecting timeout!");
                }
                catch (SocketException ex) when (ex.ErrorCode == 10060 || ex.ErrorCode == 10035 || ex.ErrorCode == 110)
                {
                    // We expect a socket timeout error
                }

                timer.Stop();

                Assert.IsTrue(timer.Elapsed >= timeout, timer.Elapsed.ToString());
                Assert.IsTrue(timer.Elapsed < timeout + timeout + timeout, timer.Elapsed.ToString());
            }
        }
    }
}
