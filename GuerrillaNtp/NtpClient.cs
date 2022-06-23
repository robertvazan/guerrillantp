// Part of GuerrillaNtp: https://guerrillantp.machinezoo.com
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace GuerrillaNtp
{
    /// <summary>
    /// Represents UDP socket used to communicate with RFC4330-compliant SNTP/NTP server.
    /// </summary>
    /// <remarks>
    /// <para>
    /// See <a href="https://guerrillantp.machinezoo.com/">project homepage</a> for guidance on how to use GuerrillaNtp.
    /// Most applications should just call <see cref="Query()" /> after instantiating this class.
    /// and then accessing <see cref="NtpTime.UtcNow" /> or <see cref="NtpTime.Now" /> on the returned <see cref="NtpTime" />.
    /// </para>
    /// <para>
    /// It is application responsibility to be a good netizen,
    /// which most importantly means using reasonable polling intervals
    /// and exponential backoff when querying public NTP server.
    /// </para>
    /// <para>
    /// This class is intended to be used by only one thread at a time,
    /// but multi-threaded access is nevertheless tolerated and safe.
    /// </para>
    /// </remarks>
    public partial class NtpClient
    {
        /// <summary>
        /// The default NTP endpoint (pool.ntp.org).
        /// </summary>
        public static readonly string DefaultEndpoint = "pool.ntp.org";

        /// <summary>
        /// The default NTP port (123).
        /// </summary>
        public const int DefaultPort = 123;

        /// <summary>
        /// The default NTP timeout (1 second).
        /// </summary>
        public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(1);

        /// <summary>
        /// The default <see cref="NtpClient"/> which communicates with pool.ntp.org
        /// </summary>
        public static readonly NtpClient Default = new();

        /// <summary>
        /// Gets or sets the timeout for SNTP queries.
        /// </summary>
        /// <value>
        /// Timeout for SNTP queries. Default is one second.
        /// </value>
        public TimeSpan Timeout { get; set; }

        readonly EndPoint endpoint;

        Socket CreateSocket()
        {
            var socket = new Socket(SocketType.Dgram, ProtocolType.Udp)
            {
                ReceiveTimeout = Convert.ToInt32(Timeout.TotalMilliseconds),
            };

            return socket;
        }

        /// <summary>
        /// Creates a new <see cref="NtpClient"/> using <see cref="DefaultEndpoint"/>, <see cref="DefaultPort"/>, and <see cref="DefaultTimeout"/>.
        /// </summary>
        public NtpClient()
        {
            this.endpoint = new DnsEndPoint(DefaultEndpoint, DefaultPort);
            this.Timeout = DefaultTimeout;
        }

        /// <inheritdoc cref="NtpClient(string, TimeSpan?, int?)"/>
        public NtpClient(IPAddress endpoint, TimeSpan? timeout = default, int? port = default)
        {
            this.endpoint = new IPEndPoint(endpoint, port ?? DefaultPort);
            this.Timeout = timeout ?? DefaultTimeout;
        }

        /// <inheritdoc cref="NtpClient(string, TimeSpan?, int?)"/>
        public NtpClient(EndPoint endpoint, TimeSpan? timeout = default)
        {
            this.endpoint = endpoint;
            this.Timeout = timeout ?? DefaultTimeout;
        }

        /// <summary>
        /// Create a new <see cref="NtpClient"/> using defaults for any unspecified values.
        /// </summary>
        /// <param name="endpoint">The NTP server</param>
        /// <param name="timeout">The amount of time to wait for a reply</param>
        /// <param name="port">The NTP port</param>
        public NtpClient(string endpoint, TimeSpan? timeout = default, int? port = default)
        {
            this.endpoint = new DnsEndPoint(endpoint, port ?? DefaultPort);
            this.Timeout = timeout ?? DefaultTimeout;
        }

        volatile NtpTime last;

        /// <summary>
        /// Result of the last NTP query.
        /// </summary>
        /// <value>
        /// Last <see cref="NtpTime"/> returned by <see cref="Query()"/>
        /// or <see cref="QueryAsync(System.Threading.CancellationToken)"/>.
        /// If NTP server has not been queried yet, this property is null.
        /// </value>
        /// <remarks>
        /// <para>
        /// Once this property is populated with <see cref="NtpTime"/> that is <see cref="NtpTime.Synchronized"/>,
        /// it will be updated only with another <see cref="NtpTime"/> that is also <see cref="NtpTime.Synchronized"/>,
        /// This logic is intended to prevent special responses (e.g. Kiss-o'-Death packets),
        /// which do not really carry network time, from replacing previously obtained network time.
        /// </para>
        /// <para>
        /// You can use <see cref="NtpTime.LocalFallback"/> as fallback as in
        /// <see cref="Last"/> ?? <see cref="NtpTime.LocalFallback"/>.
        /// </para>
        /// <para>
        /// If multiple threads query the NTP server in parallel (not recommended),
        /// this property will hold result of whichever query finishes last.
        /// This property is safe to access from multiple threads.
        /// </para>
        /// </remarks>
        public NtpTime Last => last;

        NtpTime Update(NtpRequest request, byte[] buffer, int length)
        {
            var response = NtpResponse.FromPacket(NtpPacket.FromBytes(buffer, length));
            if (!response.Matches(request))
                throw new NtpException("Response does not match the request.");
            var time = new NtpTime(response);
            if (time.Synchronized || last == null)
                last = time;
            return time;
        }

        Socket Connect()
        {
            var socket = CreateSocket();
            try
            {
                socket.Connect(endpoint);
            }
            catch
            {
                socket.Dispose();
                throw;
            }
            return socket;
        }

        /// <summary>
        /// Queries the SNTP server.
        /// </summary>
        /// <returns>Network time reported by the server.</returns>
        /// <exception cref="NtpException">
        /// Thrown when the server sends invalid response.
        /// </exception>
        /// <exception cref="SocketException">
        /// Thrown when no reply is received before <see cref="Timeout" /> is reached
        /// or when there is an error communicating with the server.
        /// </exception>
        public NtpTime Query()
        {
            using var socket = Connect();
            var request = new NtpRequest();
            socket.Send(request.ToPacket().ToBytes());
            var buffer = new byte[160];
            int length = socket.Receive(buffer);
            return Update(request, buffer, length);
        }

        async Task<Socket> ConnectAsync(CancellationToken token)
        {
            var socket = CreateSocket();
            try
            {
                await socket.ConnectAsync(endpoint, token).ConfigureAwait(false);
            }
            catch
            {
                socket.Dispose();
                throw;
            }
            return socket;
        }

        /// <summary>
        /// Queries the SNTP server.
        /// </summary>
        /// <param name="token">A <see cref="CancellationToken"/> used to cancel this request.</param>
        /// <returns>Network time reported by the server.</returns>
        /// <exception cref="NtpException">
        /// Thrown when the server sends invalid response.
        /// </exception>
        /// <exception cref="SocketException">
        /// Thrown when no reply is received before <see cref="Timeout" /> is reached
        /// or when there is an error communicating with the server.
        /// </exception>
        public async Task<NtpTime> QueryAsync(CancellationToken token = default)
        {
            using var socket = await ConnectAsync(token).ConfigureAwait(false);
            var request = new NtpRequest();
            await socket.SendAsync(request.ToPacket().ToBytes(), SocketFlags.None, token).ConfigureAwait(false);
            var buffer = new byte[160];
            int length = await socket.ReceiveAsync(buffer, SocketFlags.None, token).ConfigureAwait(false);
            return Update(request, buffer, length);
        }
    }
}
