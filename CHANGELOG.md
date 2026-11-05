# Changelog

## 0.1.0 – initial Web Adapter

- Separates HTTP-/WebSocket-Listener vom Core-
  `RuntimeApiHost`.
- Native Adapter registrieren Endpunkte über `IWebEndpointRegistry`.
- Legacy-Adapter werden über korrelierte TCP-Bridge-Requests unterstützt.
- `/health`-Endpoint und konfigurierbarer Bind-Host/Port.
