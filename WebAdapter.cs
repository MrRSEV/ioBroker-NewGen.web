using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using ioBroker_NewGen.Core.Adapters;
using ioBroker_NewGen.Core.Web;

namespace ioBroker_NewGen.Web;

/// <summary>
/// Zentraler Web-/WebSocket-Adapter. Er besitzt den Listener, aber keine
/// fachlichen Adapterrouten: diese werden über IWebEndpointRegistry registriert.
/// </summary>
public sealed class WebAdapter : DeviceAdapterBase
{
    private readonly object _listenerLock = new();
    private HttpListener? _listener;
    private CancellationTokenSource? _shutdown;
    private Task? _acceptLoop;

    public override string Name => "web";

    protected override Task OnStartAsync(CancellationToken cancellationToken)
    {
        Context.WebEndpoints.Register(
            Id,
            "/health",
            WebEndpointKind.Http,
            static (_, _) => Task.FromResult(WebEndpointResponse.Text(200, "ok")));

        if (!ReadConfigBool("web.enabled", true))
        {
            Context.RuntimeContext.Logger.LogInfo("[WebAdapter] Deaktiviert.");
            return Task.CompletedTask;
        }

        var host = ReadConfigString("web.listenHost", "127.0.0.1");
        var port = ReadConfigInt("web.port", 8080);
        var prefix = BuildPrefix(host, port);

        var listener = new HttpListener();
        listener.Prefixes.Add(prefix);
        listener.Start();
        lock (_listenerLock)
        {
            _listener = listener;
            _shutdown = new CancellationTokenSource();
            _acceptLoop = AcceptLoopAsync(listener, _shutdown.Token);
        }

        Context.RuntimeContext.Logger.LogInfo($"[WebAdapter] Web-/WebSocket-Listener auf {prefix} gestartet.");
        return Task.CompletedTask;
    }

    protected override async Task OnStopAsync(CancellationToken cancellationToken)
    {
        HttpListener? listener;
        CancellationTokenSource? shutdown;
        Task? acceptLoop;
        lock (_listenerLock)
        {
            listener = _listener;
            shutdown = _shutdown;
            acceptLoop = _acceptLoop;
            _listener = null;
            _shutdown = null;
            _acceptLoop = null;
        }

        shutdown?.Cancel();
        listener?.Stop();
        listener?.Close();
        if (acceptLoop is not null)
        {
            try
            {
                await acceptLoop.WaitAsync(cancellationToken);
            }
            catch (HttpListenerException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
        }

        shutdown?.Dispose();
        Context.WebEndpoints.Unregister(Id, "/health");
        Context.RuntimeContext.Logger.LogInfo("[WebAdapter] Listener gestoppt.");
    }

    private async Task AcceptLoopAsync(HttpListener listener, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && listener.IsListening)
        {
            HttpListenerContext context;
            try
            {
                context = await listener.GetContextAsync().WaitAsync(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (HttpListenerException)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }

            _ = HandleRequestSafeAsync(context, cancellationToken);
        }
    }

    private async Task HandleRequestSafeAsync(HttpListenerContext context, CancellationToken cancellationToken)
    {
        try
        {
            await HandleRequestAsync(context, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            context.Response.StatusCode = 503;
        }
        catch (Exception ex)
        {
            Context.RuntimeContext.Logger.LogError($"[WebAdapter] Anfrage fehlgeschlagen: {ex}");
            try
            {
                context.Response.StatusCode = 500;
                var payload = Encoding.UTF8.GetBytes("Web endpoint failed.");
                await context.Response.OutputStream.WriteAsync(payload);
            }
            catch
            {
            }
        }
        finally
        {
            try
            {
                context.Response.Close();
            }
            catch
            {
            }
        }
    }

    private async Task HandleRequestAsync(HttpListenerContext context, CancellationToken cancellationToken)
    {
        var route = context.Request.Url?.AbsolutePath ?? "/";
        if (context.Request.IsWebSocketRequest)
        {
            var socketContext = await context.AcceptWebSocketAsync(null);
            var session = new HttpListenerWebSocketSession(socketContext.WebSocket);
            var websocketResponse = await Context.WebEndpoints.DispatchAsync(new WebEndpointRequest
            {
                Route = route,
                Method = context.Request.HttpMethod,
                Headers = ReadHeaders(context.Request.Headers),
                IsWebSocket = true,
                WebSocket = session
            }, cancellationToken);
            if (websocketResponse.StatusCode >= 400)
            {
                await session.CloseAsync(cancellationToken);
            }
            return;
        }

        await using var body = new MemoryStream();
        await context.Request.InputStream.CopyToAsync(body, cancellationToken);
        var response = await Context.WebEndpoints.DispatchAsync(new WebEndpointRequest
        {
            Route = route,
            Method = context.Request.HttpMethod,
            Headers = ReadHeaders(context.Request.Headers),
            Body = body.ToArray()
        }, cancellationToken);

        context.Response.StatusCode = response.StatusCode;
        foreach (var header in response.Headers)
        {
            if (string.Equals(header.Key, "Content-Length", StringComparison.OrdinalIgnoreCase)) continue;
            context.Response.Headers[header.Key] = header.Value;
        }

        context.Response.ContentLength64 = response.Body.Length;
        await context.Response.OutputStream.WriteAsync(response.Body, cancellationToken);
    }

    private static Dictionary<string, string> ReadHeaders(System.Collections.Specialized.NameValueCollection headers)
        => headers.AllKeys
            .Where(key => key is not null)
            .ToDictionary(key => key!, key => headers[key!] ?? string.Empty, StringComparer.OrdinalIgnoreCase);

    private static string BuildPrefix(string host, int port)
    {
        if (port is < 1 or > 65535) throw new ArgumentOutOfRangeException(nameof(port));
        var bindHost = string.IsNullOrWhiteSpace(host) ? "127.0.0.1" : host.Trim();
        if (bindHost is "0.0.0.0" or "*") bindHost = "+";
        return $"http://{bindHost}:{port}/";
    }

    private string ReadConfigString(string key, string fallback)
    {
        try
        {
            return Context.Config.Get<string>(key) is { Length: > 0 } value ? value : fallback;
        }
        catch
        {
            return fallback;
        }
    }

    private int ReadConfigInt(string key, int fallback)
    {
        try
        {
            var value = Context.Config.Get<int>(key);
            return value > 0 ? value : fallback;
        }
        catch
        {
            return fallback;
        }
    }

    private bool ReadConfigBool(string key, bool fallback)
    {
        try
        {
            return Context.Config.Get<bool>(key);
        }
        catch
        {
            return fallback;
        }
    }
}

internal sealed class HttpListenerWebSocketSession : IWebSocketSession
{
    private readonly WebSocket _socket;

    public HttpListenerWebSocketSession(WebSocket socket)
    {
        _socket = socket;
    }

    public bool IsOpen => _socket.State == WebSocketState.Open;

    public Task SendTextAsync(string text, CancellationToken cancellationToken = default)
        => _socket.SendAsync(Encoding.UTF8.GetBytes(text ?? string.Empty), WebSocketMessageType.Text, true, cancellationToken);

    public async Task SendBinaryAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
    {
        await _socket.SendAsync(data, WebSocketMessageType.Binary, true, cancellationToken);
    }

    public async Task<WebSocketMessage?> ReceiveAsync(CancellationToken cancellationToken = default)
    {
        if (!IsOpen) return null;
        var buffer = new byte[16 * 1024];
        await using var payload = new MemoryStream();
        WebSocketReceiveResult result;
        do
        {
            result = await _socket.ReceiveAsync(buffer, cancellationToken);
            if (result.MessageType == WebSocketMessageType.Close) return null;
            await payload.WriteAsync(buffer.AsMemory(0, result.Count), cancellationToken);
        }
        while (!result.EndOfMessage);

        return new WebSocketMessage(payload.ToArray(), result.MessageType == WebSocketMessageType.Text, true);
    }

    public async Task CloseAsync(CancellationToken cancellationToken = default)
    {
        if (_socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
        {
            await _socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "closed", cancellationToken);
        }
    }
}
