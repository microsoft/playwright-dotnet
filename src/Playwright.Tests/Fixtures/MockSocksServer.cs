/*
 * MIT License
 *
 * Copyright (c) Microsoft Corporation.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

using System.Buffers.Binary;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Microsoft.Playwright.Tests.Fixtures;

/// <summary>
/// The SOCKS5 server that answers every connection with a fixed page, <c>MockSocksServer</c> of
/// <c>tests/config/serverFixtures.ts</c>: the <c>socksPort</c> fixture is its port.
/// </summary>
public sealed class MockSocksServer
{
    private readonly SocksProxy _socksProxy;

    public MockSocksServer()
    {
        _socksProxy = new SocksProxy(async connection =>
        {
            await connection.SocketConnectedAsync(IPAddress.Loopback, 0).ConfigureAwait(false);
            var stream = connection.Socket.GetStream();
            var buffer = new byte[64 * 1024];
            if (await stream.ReadAsync(buffer).ConfigureAwait(false) == 0)
            {
                return;
            }
            var body = "<html><title>Served by the SOCKS proxy</title></html>";
            var data = Encoding.UTF8.GetBytes(string.Join("\r\n", (string[])[
                "HTTP/1.1 200 OK",
                "Connection: close",
                "Content-Type: text/html",
                "Content-Length: " + Encoding.UTF8.GetByteCount(body).ToString(CultureInfo.InvariantCulture),
                string.Empty,
                body,
            ]));
            await stream.WriteAsync(data).ConfigureAwait(false);
            connection.Socket.Client.Shutdown(SocketShutdown.Send);
            while (await stream.ReadAsync(buffer).ConfigureAwait(false) > 0)
            {
            }
        });
    }

    /// <summary>The port that the server listens on, after <see cref="Listen"/>.</summary>
    public int Port => _socksProxy.Port;

    public void Listen(int port, string hostname = null) => _socksProxy.Listen(port, hostname);

    public Task CloseAsync() => _socksProxy.CloseAsync();
}

/// <summary>A CONNECT request that a <see cref="SocksProxy"/> got.</summary>
internal sealed class SocksConnection
{
    public SocksConnection(TcpClient socket, string host, int port)
    {
        Socket = socket;
        Host = host;
        Port = port;
    }

    public TcpClient Socket { get; }

    public string Host { get; }

    public int Port { get; }

    /// <summary>Tells the client that it is connected, from this local address. The bytes of the connection follow.</summary>
    public async Task SocketConnectedAsync(IPAddress host, int port)
    {
        var address = (host.IsIPv4MappedToIPv6 ? host.MapToIPv4() : host).GetAddressBytes();
        byte[] reply =
        [
            0x05,
            0x00, // Succeeded
            0x00, // RSV
            (byte)(address.Length == 4 ? 0x01 : 0x04), // IPv4 or IPv6
            .. address,
            (byte)(port >> 8), (byte)(port & 0xFF),
        ];
        await Socket.GetStream().WriteAsync(reply).ConfigureAwait(false);
    }
}

/// <summary>
/// The server side of SOCKS5 (RFC 1928) without authentication, the port of <c>SocksProxy</c> of
/// <c>packages/utils/socksProxy.ts</c> with the pattern <c>*</c>. It calls the handler for every CONNECT
/// request and closes the connection when the handler returns or throws.
/// </summary>
internal sealed class SocksProxy
{
    private readonly Func<SocksConnection, Task> _onSocksRequested;
    private readonly HashSet<TcpClient> _sockets = [];
    private TcpListener _server;
    private volatile bool _closed;

    public SocksProxy(Func<SocksConnection, Task> onSocksRequested)
    {
        _onSocksRequested = onSocksRequested;
    }

    public int Port { get; private set; }

    public void Listen(int port, string hostname = null)
    {
        _server = new TcpListener(hostname == null ? IPAddress.IPv6Any : IPAddress.Parse(hostname), port);
        if (hostname == null)
        {
            _server.Server.DualMode = true;
        }
        _server.Start();
        Port = ((IPEndPoint)_server.LocalEndpoint).Port;
        _ = AcceptLoopAsync();
    }

    public async Task CloseAsync()
    {
        if (_closed)
        {
            return;
        }
        _closed = true;
        TcpClient[] sockets;
        lock (_sockets)
        {
            sockets = [.. _sockets];
            _sockets.Clear();
        }
        foreach (var socket in sockets)
        {
            socket.Close();
        }
        _server.Stop();
        await Task.CompletedTask.ConfigureAwait(false);
    }

    private static async Task<byte[]> ReadBytesAsync(Stream stream, int length)
    {
        var buffer = new byte[length];
        await stream.ReadExactlyAsync(buffer).ConfigureAwait(false);
        return buffer;
    }

    private async Task AcceptLoopAsync()
    {
        while (!_closed)
        {
            TcpClient socket;
            try
            {
                socket = await SafeAccept.AcceptAsync(_server).ConfigureAwait(false);
            }
            catch (Exception)
            {
                if (_closed)
                {
                    return;
                }
                continue;
            }
            _ = RunAsync(socket);
        }
    }

    private async Task RunAsync(TcpClient socket)
    {
        socket.NoDelay = true;
        lock (_sockets)
        {
            _sockets.Add(socket);
        }
        try
        {
            var stream = socket.GetStream();

            // +----+----------+----------+
            // |VER | NMETHODS | METHODS  |
            // +----+----------+----------+
            var greeting = await ReadBytesAsync(stream, 2).ConfigureAwait(false);
            if (greeting[0] != 0x05 || greeting[1] == 0)
            {
                return;
            }
            var methods = await ReadBytesAsync(stream, greeting[1]).ConfigureAwait(false);
            if (!methods.Contains((byte)0x00))
            {
                await stream.WriteAsync(new byte[] { 0x05, 0xFF }).ConfigureAwait(false); // No acceptable methods
                return;
            }
            await stream.WriteAsync(new byte[] { 0x05, 0x00 }).ConfigureAwait(false); // No authentication required

            // +----+-----+-------+------+----------+----------+
            // |VER | CMD |  RSV  | ATYP | DST.ADDR | DST.PORT |
            // +----+-----+-------+------+----------+----------+
            var request = await ReadBytesAsync(stream, 4).ConfigureAwait(false);
            if (request[0] != 0x05)
            {
                return;
            }
            var host = string.Empty;
            switch (request[3])
            {
                case 0x01: // IPv4
                    host = string.Join(".", await ReadBytesAsync(stream, 4).ConfigureAwait(false));
                    break;
                case 0x03: // FqName
                    var length = await ReadBytesAsync(stream, 1).ConfigureAwait(false);
                    host = Encoding.UTF8.GetString(await ReadBytesAsync(stream, length[0]).ConfigureAwait(false));
                    break;
                case 0x04: // IPv6
                    var bytes = await ReadBytesAsync(stream, 16).ConfigureAwait(false);
                    host = string.Join(":", Enumerable.Range(0, 8).Select(i => BinaryPrimitives.ReadUInt16BigEndian(bytes.AsSpan(i * 2)).ToString("x", CultureInfo.InvariantCulture)));
                    break;
            }
            var port = BinaryPrimitives.ReadUInt16BigEndian(await ReadBytesAsync(stream, 2).ConfigureAwait(false));
            if (request[1] != 0x01)
            {
                // Only CONNECT: command not supported.
                await stream.WriteAsync(new byte[] { 0x05, 0x07, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }).ConfigureAwait(false);
                return;
            }
            await _onSocksRequested(new SocksConnection(socket, host, port)).ConfigureAwait(false);
        }
        catch (Exception)
        {
            // The client closed the connection, or the handler refused it.
        }
        finally
        {
            socket.Close();
            lock (_sockets)
            {
                _sockets.Remove(socket);
            }
        }
    }
}
