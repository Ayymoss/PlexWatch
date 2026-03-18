# PlexWatch

Monitors active Plex Media Server streams and automatically terminates sessions that violate configured rules. Sends notifications to Discord when a stream is killed.

## What it does

PlexWatch polls your Plex server for active sessions every 30 seconds (configurable). For each session, it checks:

- **Plex Web blocked** — Plex Web clients are not permitted; users must use the Desktop app
- **Remote Quality** — The client's remote quality must be set to Original (no server-side transcoding)
- **Resolution mismatch** — The stream resolution must be within 10% of the source (catches transcoding that slips through)
- **Device blocklist** — Specific users/devices can be blocked by name

When a rule is violated, the stream is terminated with a user-friendly message explaining what to change, and a Discord notification is sent with session details.

Plex webhooks are optionally supported as an optimization — when configured, they trigger an immediate check instead of waiting for the next poll cycle.

## Requirements

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download)
- A Plex Media Server with a valid API token
- A Discord webhook URL (for notifications)

## Configuration

PlexWatch uses standard ASP.NET Core configuration. Settings are loaded from `appsettings.json`, environment variables, and (in development) user-secrets.

### appsettings.json

```json
{
  "Debug": false,
  "Plex": {
    "ServerUrl": "http://localhost:32400",
    "Token": ""
  },
  "Discord": {
    "WebhookUrl": ""
  },
  "Monitoring": {
    "PollingIntervalSeconds": 30,
    "BlockedDeviceNames": {}
  }
}
```

### Development (user-secrets)

```bash
cd PlexWatch
dotnet user-secrets set "Plex:Token" "your-plex-token"
dotnet user-secrets set "Plex:ServerUrl" "http://your-plex-ip:32400"
dotnet user-secrets set "Discord:WebhookUrl" "https://discord.com/api/webhooks/..."
```

### Docker (environment variables)

```
Plex__Token=your-plex-token
Plex__ServerUrl=http://plex:32400
Discord__WebhookUrl=https://discord.com/api/webhooks/...
Monitoring__PollingIntervalSeconds=30
```

### Blocking devices

To block specific users or devices, add entries to `BlockedDeviceNames`. Use `*` to block all devices for a user:

```json
{
  "Monitoring": {
    "BlockedDeviceNames": {
      "SomeUser": ["Living Room TV", "Bedroom Roku"],
      "AnotherUser": ["*"]
    }
  }
}
```

## Running

```bash
dotnet run --project PlexWatch/PlexWatch.csproj
```

## Docker

The image is built using a multi-stage Dockerfile and published to GitHub Container Registry on push to `master`.

```bash
docker run -d \
  -p 80:80 \
  -e Plex__Token=your-token \
  -e Plex__ServerUrl=http://plex:32400 \
  -e Discord__WebhookUrl=https://discord.com/api/webhooks/... \
  ghcr.io/your-username/plexwatch:latest
```

Optional PUID/PGID support for file permissions:

```bash
docker run -d \
  -e PUID=1000 \
  -e PGID=1000 \
  ...
```

## Endpoints

| Method | Path | Description |
|--------|------|-------------|
| `GET` | `/health` | Health check |
| `POST` | `/plex-webhook` | Plex webhook receiver (triggers immediate session check) |

In development, API docs are available at `/scalar/v1`.

## Plex Webhook Setup

Optional. In your Plex server settings, add a webhook pointing to `http://your-plexwatch-host/plex-webhook`. This makes PlexWatch react immediately to play/pause/resume/stop events instead of waiting for the next poll cycle.
