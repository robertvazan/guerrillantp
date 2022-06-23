// Part of GuerrillaNtp: https://guerrillantp.machinezoo.com
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using NUnit.Framework;

namespace GuerrillaNtp.Tests
{
    [TestFixture]
    public class NtpClientTests
    {
        [Test]
        [Retry(3)]
        public void Query() => new NtpClient().Query();

        [Test]
        [Retry(3)]
        public async Task QueryAsync() => await new NtpClient().QueryAsync();

        [Test]
        public void Timeout()
        {
            var timeout = TimeSpan.FromMilliseconds(500);

            // Note: pick a host that *drops* packets. The test will fail if the host merely *rejects* packets.
            var client = new NtpClient(IPAddress.Parse("8.8.8.8"), timeout);

            var timer = Stopwatch.StartNew();

            try
            {
                client.Query();
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
