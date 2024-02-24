using System.Text.Json;
using System.Text.Json.Serialization;

namespace PlexWatch.Utilities;

public class StringToIntConverter : JsonConverter<int>
{
    public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (int.TryParse(reader.GetString(), out var value))
        {
            return value;
        }

        throw new JsonException("Failed to convert string to int");
    }

    public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
    {
        // This is not needed for serialization
    }
}
