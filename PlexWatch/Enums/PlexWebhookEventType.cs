namespace PlexWatch.Enums;

public enum PlexWebhookEventType
{
    LibraryOnDeck,
    LibraryNew,

    MediaPause,
    MediaPlay,
    MediaRate,
    MediaResume,
    MediaScrobble,
    MediaStop,

    AdminDatabaseBackup,
    AdminDatabaseCorrupted,

    DeviceNew,

    PlaybackStarted
}
