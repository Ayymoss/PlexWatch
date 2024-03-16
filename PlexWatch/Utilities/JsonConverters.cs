using System.Text.Json;
using System.Text.Json.Serialization;
using PlexWatch.Enums;

namespace PlexWatch.Utilities;

public abstract class JsonConverters
{
    public static JsonSerializerOptions JsonOptions { get; } = new() { PropertyNameCaseInsensitive = true };
}

public class StringToIntConverter : JsonConverter<int>
{
    public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType is JsonTokenType.Number) return reader.GetInt32();
        if (int.TryParse(reader.GetString(), out var value)) return value;

        throw new JsonException($"Failed to convert string to int: {reader.GetString()}");
    }

    public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}

public class StringToFloatConverter : JsonConverter<float>
{
    public override float Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType is JsonTokenType.Number) return reader.GetSingle();
        if (float.TryParse(reader.GetString(), out var value)) return value;

        throw new JsonException($"Failed to convert string to int: {reader.GetString()}");
    }

    public override void Write(Utf8JsonWriter writer, float value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}

public class IntToBoolConverter : JsonConverter<bool>
{
    public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType is JsonTokenType.True or JsonTokenType.False) return reader.GetBoolean();
        var value = reader.GetInt32();
        return value != 0;
    }

    public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}

public class WebhookToEventEnumConverter : JsonConverter<PlexWebhookEventType>
{
    public override PlexWebhookEventType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var webEvent = reader.GetString();
        return webEvent switch
        {
            "library.new" => PlexWebhookEventType.LibraryNew,
            "library.on.deck" => PlexWebhookEventType.LibraryOnDeck,

            "media.play" => PlexWebhookEventType.MediaPlay,
            "media.resume" => PlexWebhookEventType.MediaResume,
            "media.pause" => PlexWebhookEventType.MediaPause,
            "media.stop" => PlexWebhookEventType.MediaStop,
            "media.rate" => PlexWebhookEventType.MediaRate,
            "media.scrobble" => PlexWebhookEventType.MediaScrobble,

            "admin.database.backup" => PlexWebhookEventType.AdminDatabaseBackup,
            "admin.database.corrupted" => PlexWebhookEventType.AdminDatabaseCorrupted,

            "device.new" => PlexWebhookEventType.DeviceNew,

            "playback.started" => PlexWebhookEventType.PlaybackStarted,
            _ => throw new JsonException($"Failed to convert string to enum: {webEvent}")
        };
    }

    public override void Write(Utf8JsonWriter writer, PlexWebhookEventType value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}

public class MediaTypeToEnumConverter : JsonConverter<MediaType>
{
    public override MediaType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var webEvent = reader.GetString();
        return webEvent switch
        {
            "movie" => MediaType.Movie,
            "episode" or "show" => MediaType.Episode,
            "clip" => MediaType.Clip,
            _ => throw new JsonException($"Failed to convert string to enum: {webEvent}")
        };
    }

    public override void Write(Utf8JsonWriter writer, MediaType value, JsonSerializerOptions options)
    {
        throw new NotImplementedException();
    }
}
