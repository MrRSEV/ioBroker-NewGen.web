# Architektur – ioBroker-NewGen Web Adapter

## Verantwortungsgrenze

Der Web Adapter ist der einzige Adapter, der einen HTTP-/WebSocket-Listener
öffnet. `RuntimeApiHost` gehört nicht zum Adapter und wird nicht verwendet.

```text
Native Adapter ─┐
                ├─ IWebEndpointRegistry ── WebAdapter ── HTTP/WebSocket
Legacy Adapter ─┘          (Core-Vertrag)       Listener
```

Der Core besitzt nur die thread-sichere Registrierung und Dispatch-Schnittstelle
`IWebEndpointRegistry`. Dadurch können native Adapter Handler direkt registrieren.
Legacy-Adapter melden eine Route über die TCP-Bridge; die Runtime registriert
einen korrelierten Bridge-Handler.

## Request-Fluss

1. Der Web Adapter nimmt HTTP oder WebSocket auf seinem konfigurierten Port an.
2. Die Route wird an `IWebEndpointRegistry.DispatchAsync` übergeben.
3. Der passende Handler des Besitzer-Adapters wird ausgeführt.
4. Die Antwort wird durch den Web Adapter als HTTP-Antwort ausgegeben oder die
   WebSocket-Session bleibt beim Handler.

Für Legacy-HTTP-Routen verwendet die Runtime `WebEndpointRequest`-/
`WebEndpointResponse`-Frames mit Correlation-ID über die TCP-Bridge.

## Isolation

Der Zugriff auf `IAdapterContext.ApiHost` und
`IAdapterContext.RuntimeContext.ApiHost` ist für installierte Adapter gesperrt.
Der adapterbezogene Context protokolliert den Fehler mit dem Präfix
`[RuntimeApiHost]`, markiert die betroffene Instanz als `Faulted` und stoppt sie
asynchron. Andere Adapter, der Web Adapter und der Core werden nicht beendet.

Die Registry fängt Handler-Ausnahmen in der Dispatch-Grenze ab und liefert eine
500-Antwort. Cancellation wird dagegen an den Listener weitergegeben.
