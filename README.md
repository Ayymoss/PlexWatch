# Plex Watch

Listens to Plex Media Server webhooks and raises typed events.
Currently, it will terminate a stream if they are transcoding (though this is nuanced).

Easily extendable to add more functionality.

## Configuration

The application can be configured using environment variables. The following variables are available:

| Variable                  | Description                                                                                                                              | Default |
|---------------------------|------------------------------------------------------------------------------------------------------------------------------------------|---------|
| `PLEX_URL`                | The URL of your Plex server.                                                                                                             |         |
| `PLEX_TOKEN`              | Your Plex token.                                                                                                                         |         |
| `DISCORD_WEBHOOK`         | The URL of your Discord webhook for notifications.                                                                                       |         |
| `DEBUG`                   | Set to `true` to enable debug logging.                                                                                                   | `false` |
| `TRANSCODE_KICK_BEHAVIOUR` | The behaviour for kicking transcoded streams. <br/> `0` = Disabled <br/> `1` = Kick on 4K transcode <br/> `2` = Kick on all transcodes      | `2`     |
| `PUID`                    | The user ID to run the application as.                                                                                                   | `0`     |
| `PGID`                    | The group ID to run the application as.                                                                                                  | `0`     |
