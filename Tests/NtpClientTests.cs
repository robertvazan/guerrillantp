using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

using GuerrillaNtp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests {
    [TestClass]
    public class NtpClientTests {
        private IPAddress server = Dns.GetHostEntry("pool.ntp.org").AddressList[0];

        [TestMethod]
        public void Test_can_get_correction_offset() {
            using (var client = new NtpClient(server)) {
                for (int i = 0; i < 10; i++)
                    Trace.WriteLine($"Offset #{i+1}: {client.GetCorrectionOffset()}");
            }
        }

        [TestMethod]
        public void Test_Timeout_expires() {
            var timeout = TimeSpan.FromMilliseconds(500);

            // Note: pick a host that *drops* packets. The test will fail if the host merely *rejects* packets.
            using (var client = new NtpClient(IPAddress.Parse("8.8.8.8"))) {
                client.Timeout = timeout;

                var timer = Stopwatch.StartNew();

                try {
                    client.GetCorrectionOffset();
                    Assert.Fail("Shouldn't get here. Expecting timeout!");
                }
                catch (SocketException ex) when (ex.ErrorCode == 10060) { /* We expect a socket timeout error */ }

                timer.Stop();

                Assert.IsTrue(timer.Elapsed >= timeout, timer.Elapsed.ToString());
                Assert.IsTrue(timer.Elapsed < 2 * timeout, timer.Elapsed.ToString());
            }
        }
    }
}
