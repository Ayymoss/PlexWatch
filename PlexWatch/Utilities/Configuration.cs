using System.Reflection;
using System.Text.Json;

namespace PlexWatch.Utilities;

public class Configuration
{
    public string ApiKey { get; set; } = null!;
    public string LogFileLocation { get; set; } = null!;
}
