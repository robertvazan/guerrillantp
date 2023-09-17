// Part of GuerrillaNtp: https://guerrillantp.machinezoo.com
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace GuerrillaNtp;

/// <summary>
/// Client for RFC4330-compliant SNTP/NTP server.
/// </summary>
/// <remarks>
/// <para>
/// See <a href="https://guerrillantp.machinezoo.com/">project homepage</a> for guidance on how to use GuerrillaNtp.
/// Most applications should just call <see cref="Query()" /> or <see cref="QueryAsync(CancellationToken)" /> once
/// and then access <see cref="NtpClock.UtcNow" /> or <see cref="NtpClock.Now" />
/// on the returned <see cref="NtpClock" /> or the one stored in <see cref="Last" />.
/// </para>
/// <para>
/// It is recommended to have only one instance of this class in the application.
/// You can use <see cref="Default" /> one to query pool.ntp.org.
/// Socket is allocated anew for every query. There is no need to dispose the client.
/// This class is intended to be used by one thread at a time,
/// but multi-threaded access is nevertheless tolerated and safe.
/// </para>
/// <para>
/// It is application responsibility to be a good netizen,
/// which most importantly means using reasonable polling intervals
/// and exponential backoff when querying public NTP servers.
/// </para>
/// </remarks>
public partial class NtpClient
{
    /// <summary>
    /// Default SNTP host (pool.ntp.org).
    /// </summary>
    public static readonly string DefaultHost = "pool.ntp.org";

    /// <summary>
    /// Default SNTP port (123).
    /// </summary>
    public const int DefaultPort = 123;

    /// <summary>
    /// Default AddressFamily (AddressFamily.Unspecified).
    /// </summary>
    public const AddressFamily DefaultAddressFamily = AddressFamily.Unspecified;

    /// <summary>
    /// Default SNTP endpoint (pool.ntp.org).
    /// </summary>
    public static readonly EndPoint DefaultEndpoint = new DnsEndPoint(DefaultHost, DefaultPort);

    /// <summary>
    /// Default query timeout (1 second).
    /// </summary>
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Default instance configured to use <see cref="DefaultEndpoint" /> and <see cref="DefaultTimeout" />.
    /// </summary>
    public static readonly NtpClient Default = new();

    /// <summary>
    /// Query timeout.
    /// </summary>
    /// <value>
    /// How long to wait for server response. Initialized in constructor.
    /// </value>
    public TimeSpan Timeout { get; init; }

    private readonly EndPoint endpoint;

    private readonly AddressFamily addressFamily;

    private Socket CreateSocket()
    {
        var socket = new Socket(addressFamily, SocketType.Dgram, ProtocolType.Udp)
        {
            ReceiveTimeout = Convert.ToInt32(Timeout.TotalMilliseconds),
        };

        return socket;
    }

    /// <summary>
    /// Creates new client for SNTP server on given endpoint.
    /// </summary>
    /// <param name="endpoint">SNTP server endpoint.</param>
    /// <param name="timeout">Query timeout. Null to use <see cref="DefaultTimeout"/>.</param>
    /// <param name="addressfamily">AddressFamily. Null to use <see cref="DefaultAddressFamily"/>.</param>
    public NtpClient(EndPoint endpoint, TimeSpan? timeout = default, AddressFamily? addressfamily = DefaultAddressFamily)
    {
        this.endpoint = endpoint;
        Timeout = timeout ?? DefaultTimeout;
        addressFamily = addressfamily ?? DefaultAddressFamily;
    }

    /// <summary>
    /// Creates new client using <see cref="DefaultEndpoint"/> and <see cref="DefaultTimeout"/>.
    /// </summary>
    public NtpClient() : this(DefaultEndpoint) { }

    /// <summary>
    /// Creates new client for SNTP server on given IP address.
    /// </summary>
    /// <param name="ip">IP address of the SNTP server.</param>
    /// <param name="timeout">Query timeout. Null to use <see cref="DefaultTimeout"/>.</param>
    /// <param name="port">Server port. Null to use <see cref="DefaultPort"/>.</param>
    public NtpClient(IPAddress ip, TimeSpan? timeout = null, int? port = null) : this(new IPEndPoint(ip, port ?? DefaultPort), timeout) { }

    /// <summary>
    /// Creates new client for SNTP server on given host.
    /// </summary>
    /// <param name="host">DNS name or IP address of the SNTP server.</param>
    /// <param name="timeout">Query timeout. Null to use <see cref="DefaultTimeout"/>.</param>
    /// <param name="port">Server port. Null to use <see cref="DefaultPort"/>.</param>
    public NtpClient(string host, TimeSpan? timeout = null, int? port = null) : this(new DnsEndPoint(host, port ?? DefaultPort), timeout) { }

    private volatile NtpClock last;

    /// <summary>
    /// Result of the last NTP query.
    /// </summary>
    /// <value>
    /// Last <see cref="NtpClock"/> returned by <see cref="Query()"/> or <see cref="QueryAsync(System.Threading.CancellationToken)"/>.
    /// If NTP server has not been queried yet, this property is null.
    /// </value>
    /// <remarks>
    /// <para>
    /// Once this property is populated with <see cref="NtpClock"/> that is <see cref="NtpClock.Synchronized"/>,
    /// it will be updated only with another <see cref="NtpClock"/> that is also <see cref="NtpClock.Synchronized"/>.
    /// This logic is intended to prevent special responses (e.g. Kiss-o'-Death packets),
    /// which do not really carry network time, from replacing previously obtained network time.
    /// </para>
    /// <para>
    /// You can use <see cref="NtpClock.LocalFallback"/> as fallback as in
    /// <see cref="Last"/> ?? <see cref="NtpClock.LocalFallback"/>.
    /// </para>
    /// <para>
    /// If multiple threads query the NTP server in parallel (not recommended),
    /// this property will hold result of whichever query finishes last.
    /// This property is safe to access from multiple threads.
    /// </para>
    /// </remarks>
    public NtpClock Last => last;

    private NtpClock Update(NtpRequest request, byte[] buffer, int length)
    {
        var response = NtpResponse.FromPacket(NtpPacket.FromBytes(buffer, length));
        if (!response.Matches(request))
        {
            throw new NtpException("Response does not match the request.");
        }

        var time = new NtpClock(response);
        if (time.Synchronized || last == null)
        {
            last = time;
        }

        return time;
    }

    private Socket Connect()
    {
        var socket = CreateSocket();
        try
        {
#if NET5_0_OR_GREATER
            socket.Connect(endpoint);
#else
            if (endpoint is DnsEndPoint dns)
            {
                socket.Connect(dns.Host, dns.Port);
            }
            else
            {
                socket.Connect(endpoint);
            }
#endif
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
    /// <returns>Network time source synchronized with the server.</returns>
    /// <exception cref="NtpException">
    /// Thrown when the server sends invalid response.
    /// </exception>
    /// <exception cref="SocketException">
    /// Thrown when no reply is received before <see cref="Timeout" /> is reached
    /// or when there is an error communicating with the server.
    /// </exception>
    public NtpClock Query()
    {
        using var socket = Connect();
        var request = new NtpRequest();
        socket.Send(request.ToPacket().ToBytes());
        var buffer = new byte[160];
        var length = socket.Receive(buffer);
        return Update(request, buffer, length);
    }

    private async Task<Socket> ConnectAsync(CancellationToken token)
    {
        var socket = CreateSocket();
        try
        {
#if NET5_0_OR_GREATER
            await socket.ConnectAsync(endpoint, token).ConfigureAwait(false);
#else
            if (endpoint is DnsEndPoint dns)
            {
                await Task.Factory.FromAsync(socket.BeginConnect, socket.EndConnect, dns.Host, dns.Port, null).ConfigureAwait(false);
            }
            else
            {
                await Task.Factory.FromAsync(socket.BeginConnect, socket.EndConnect, endpoint, null).ConfigureAwait(false);
            }
#endif
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
    /// <returns>Network time source synchronized with the server.</returns>
    /// <exception cref="NtpException">
    /// Thrown when the server sends invalid response.
    /// </exception>
    /// <exception cref="SocketException">
    /// Thrown when no reply is received before <see cref="Timeout" /> is reached
    /// or when there is an error communicating with the server.
    /// </exception>
    public async Task<NtpClock> QueryAsync(CancellationToken token = default)
    {
        using var socket = await ConnectAsync(token).ConfigureAwait(false);
        var request = new NtpRequest();
        var flags = SocketFlags.None;
        var rcvbuff = new byte[160];

#if NET5_0_OR_GREATER
        await socket.SendAsync(request.ToPacket().ToBytes(), flags, token).ConfigureAwait(false);
        int length = await socket.ReceiveAsync(rcvbuff, flags, token).ConfigureAwait(false);
#else
        var sndbuff = request.ToPacket().ToBytes();
        await Task.Factory.FromAsync(
                       socket.BeginSend(sndbuff, 0, sndbuff.Length, flags, null, null),
                       socket.EndSend
        ).ConfigureAwait(false);

        var length = await Task.Factory.FromAsync(
                 (cb, s) => socket.BeginReceive(rcvbuff, 0, rcvbuff.Length, flags, cb, s),
                 socket.EndReceive,
                 null
        ).ConfigureAwait(false);
#endif
        return Update(request, rcvbuff, length);
    }
}
