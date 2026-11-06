# Changelog

## 1.0.1 – 2026-11-06

- Der Adapter meldet sich jetzt mit seiner Version (aus
  `ioBroker-NewGen.web.csproj`) über `IAdapter.Version` an der Runtime; sie ist
  im Admin UI lesbar. Die Konstante `WebVersion.Value` wird beim Build erzeugt.
- Benötigt Runtime `2.1.0-beta` oder neuer.

## 0.1.0 – initial Web Adapter

- Separates HTTP-/WebSocket-Listener vom Core-
  `RuntimeApiHost`.
- Native Adapter registrieren Endpunkte über `IWebEndpointRegistry`.
- Legacy-Adapter werden über korrelierte TCP-Bridge-Requests unterstützt.
- `/health`-Endpoint und konfigurierbarer Bind-Host/Port.
