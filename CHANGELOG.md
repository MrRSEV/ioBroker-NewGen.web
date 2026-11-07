# Changelog

## 1.0.4 – 2026-11-07

- Query-Strings aus HTTP- und WebSocket-Anfragen werden an die registrierten
  Endpunkte weitergereicht.
- Vorhandene WebSocket-Routen werden vor dem Upgrade geprüft; nicht registrierte
  Routen werden als HTTP-404 abgewiesen. Erwartete Verbindungsabbrüche beim
  Schreiben oder Schließen werden nicht als Serverfehler protokolliert.
- Neu gebaut gegen Runtime `2.4.0-beta`.

## 1.0.2 – 2026-11-06

- Neu gebaut gegen Runtime `2.1.1-beta` (Adapter binden an den exakten Runtime-Assembly-Namen); keine funktionalen Änderungen.

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
