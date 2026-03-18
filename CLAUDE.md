# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run

```bash
dotnet build PlexWatch/PlexWatch.csproj
dotnet run --project PlexWatch/PlexWatch.csproj
```

The app requires .NET 10.0 SDK. In development, Scalar API docs are available at `/scalar/v1`.

## Configuration

Configuration uses standard ASP.NET Core layering: `appsettings.json` > `appsettings.Development.json` > user-secrets > environment variables.

Secrets (Plex token, Discord webhook URL, Plex server URL) must be set via `dotnet user-secrets` for local development:
```bash
cd PlexWatch
dotnet user-secrets set "Plex:Token" "<token>"
dotnet user-secrets set "Plex:ServerUrl" "http://<plex-ip>:32400"
dotnet user-secrets set "Discord:WebhookUrl" "https://discord.com/api/webhooks/..."
```

In Docker production, these are set via environment variables with `__` as the section separator (e.g. `Plex__Token`).

## Architecture

PlexWatch monitors active Plex Media Server streams and terminates those that violate configured rules (transcoding, wrong client, blocked devices). It notifies Discord when a stream is terminated.

### Core flow

`SessionMonitorService` (BackgroundService) polls Plex every N seconds. A webhook endpoint (`POST /plex-webhook`) can trigger an immediate check by cancelling the poll delay — it's an optimization, not the primary mechanism.

Each poll cycle: **SessionContextFactory** fetches sessions from the Plex API and transforms raw API models into `SessionContext` records → **RuleEvaluator** checks each session against rules and returns a `TerminationReason` → **SessionTerminator** kills violating streams via the Plex API with a user-facing message → **DiscordNotifier** sends a structured embed to Discord.

### Key services

- **SessionContextFactory** — owns all Plex API interaction (session fetching, metadata lookup, quality profile resolution)
- **RuleEvaluator** — pure rule logic, no I/O. Checks: Plex Web blocked → quality not Original → resolution mismatch → device blocklist
- **SessionTerminator** — calls Plex terminate API. User-facing messages must be layman-friendly and readable even when newlines are replaced with spaces (some Plex clients do this)
- **DiscordNotifier** — sends Discord embeds with structured fields, color-coded red for terminations

### Plex API integration

`IPlexApi` (Refit interface) talks to the Plex server. The session endpoint (`/status/sessions`) returns active streams. Content metadata endpoints (`/library/metadata/...`) provide source media details for comparison. The terminate endpoint stops a session with a reason message.

### Configuration classes

Three strongly-typed settings in `Configuration/`: `PlexSettings`, `DiscordSettings`, `MonitoringSettings`. All support hot-reload via `IOptionsMonitor`.

## Deployment

Docker image is built via multi-stage Dockerfile and pushed to GHCR by the GitHub Actions workflow on push to master. The Dockerfile builds the project internally — no separate publish step needed.
