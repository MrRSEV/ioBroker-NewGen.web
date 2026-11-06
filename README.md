# ioBroker-NewGen Web Adapter

> **Unofficial community project.** This project is not affiliated with or endorsed by the official ioBroker project.

Eigenständiger nativer C#-Adapter für HTTP- und WebSocket-Endpunkte.

Der Adapter besitzt den einzigen Web-/WebSocket-Listener. Native und Legacy-
Adapter registrieren ihre Routen über den Core-Vertrag
`IWebEndpointRegistry`; sie starten keinen eigenen Webserver und greifen nicht
auf `RuntimeApiHost` oder `WebHostService.cs` zu.

## Build

Das Repository ist unabhängig vom Core-Repository. Für einen lokalen Build
müssen passende Host-Verträge bereitgestellt werden:

```powershell
dotnet build WebAdapter.csproj `
  -p:HostRuntimePath=..\..\ioBroker-NewGen.runtime\bin\Debug\net9.0\ioBroker-NewGen.dll `
  -p:RsevUtilitiesPath=..\..\ioBroker-NewGen.runtime\lib\RSEV.Utilities.dll
```

Für einen eigenständigen Checkout können die beiden DLLs alternativ nach
`lib/` kopiert werden. Die Ausgabe `bin/WebAdapter.dll` wird anschließend in
das native Adapterverzeichnis des Runtime-Releases kopiert.

## Betrieb

Standardwerte:

| Konfiguration | Standard | Bedeutung |
|---|---:|---|
| `web.enabled` | `true` | Listener aktivieren |
| `web.listenHost` | `127.0.0.1` | Bind-Adresse |
| `web.port` | `8080` | HTTP-/WebSocket-Port |

Der Adapter registriert `/health` und stellt den Listener bereit. Weitere
native Adapter verwenden beispielsweise:

```csharp
Context.WebEndpoints.Register(
    Id,
    "/api/example",
    WebEndpointKind.Http,
    async (request, cancellationToken) => WebEndpointResponse.Text(200, "ok"));
```

WebSocket-Endpunkte verwenden `WebEndpointKind.WebSocket` und erhalten in der
Anfrage eine `IWebSocketSession`. Legacy-Adapter registrieren Routen über die
TCP-Bridge; die Runtime legt dafür einen Bridge-Proxy im selben Registry-
Vertrag an.

## Sicherheits- und Lifecycle-Regel

`RuntimeApiHost` ist kein Adapter-Webserver. Ein installierter Adapter, der
`Context.ApiHost` bzw. `WebHostService.cs` direkt verwendet, wird vom Core mit
einem Fehler protokolliert und nur in der betroffenen Instanz in den Status
`Faulted` beendet. Core, Web-Adapter und andere Adapter bleiben aktiv.

## Repository

Dieses Projekt wird separat gepflegt:

<https://github.com/MrRSEV/ioBroker-NewGen.web>

Weitere Details stehen in [doc/architecture.md](doc/architecture.md) und
[doc/api.md](doc/api.md).
