# API – Web Adapter

## Native Adapter

```csharp
Context.WebEndpoints.Register(
    Id,
    "/api/device",
    WebEndpointKind.Http,
    async (request, cancellationToken) =>
        WebEndpointResponse.Text(200, "ok"));
```

Für WebSockets:

```csharp
Context.WebEndpoints.Register(
    Id,
    "/ws/device",
    WebEndpointKind.WebSocket,
    async (request, cancellationToken) =>
    {
        if (request.WebSocket is not null)
        {
            await request.WebSocket.SendTextAsync("connected", cancellationToken);
        }

        return new WebEndpointResponse { StatusCode = 101 };
    });
```

Eine Route kann nur genau einem Adapter gehören. Der Besitzer darf seine Route
ersetzen oder wieder entfernen; fremde Routen werden nicht überschrieben.

## Legacy-Adapter

Legacy-Adapter verwenden `registerWebEndpointAsync(route, handler)` aus dem
gebündelten AdapterCore. Die Runtime registriert die Route für die jeweilige
Adapterinstanz und führt HTTP-Anfragen über korrelierte Bridge-Frames aus.

## Konfiguration

| Schlüssel | Standard |
|---|---:|
| `web.enabled` | `true` |
| `web.listenHost` | `127.0.0.1` |
| `web.port` | `8080` |
