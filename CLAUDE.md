# Claude instructions for ioBroker-NewGen.web

## Repository scope

This repository contains the native `web` adapter. It owns the single
HTTP/WebSocket listener and dispatches routes registered through
`IWebEndpointRegistry` by the runtime, Admin UI, and other adapters.

## Build

The Web Adapter is built against host contract assemblies supplied by the
runtime:

```powershell
dotnet build WebAdapter.csproj `
  -p:HostRuntimePath=..\..\ioBroker-NewGen.runtime\bin\Debug\net9.0\ioBroker-NewGen.dll `
  -p:RsevUtilitiesPath=..\..\ioBroker-NewGen.runtime\lib\RSEV.Utilities.dll
```

For an isolated checkout, place matching contract DLLs in `/lib` as described
in the repository README.

Every change must be recorded in `CHANGELOG.md`. Update the relevant
documentation under `/Doku` or `doc/` whenever routes, WebSocket behavior,
configuration, architecture, lifecycle, or operational procedures change.

## Implementation rules

- Do not create a second HTTP or WebSocket listener in an adapter.
- Preserve separate HTTP and WebSocket route registration, including exact and
  prefix route matching.
- Keep WebSocket upgrade handling, cancellation, client disconnect handling,
  and response status behavior intact.
- Validate both `/health` and representative Admin/legacy WebSocket routes
  when changing dispatch behavior.

## Runtime dependency synchronization

When runtime core classes or contracts change, rebuild the runtime first and
copy/replace the updated runtime and utility assemblies in this repository's
`/lib` directory before building. Refresh other dependent `/lib` directories as
well so all repositories use the same contract versions.

Use `apply_patch` for source/documentation edits and inspect `git diff` before
handoff.
