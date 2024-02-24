using System.Reflection;
using System.Text.Json;

namespace PlexWatch.Utilities;

public class Configuration
{
    public string ApiKey { get; set; } = null!;
    public string LogFileLocation { get; set; } = null!;
}

public static class SetupConfiguration
{
    public static Configuration ReadConfiguration()
    {
        var workingDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        var fileName = Path.Join(workingDirectory, "_Configuration", "Configuration.json");
        var jsonString = File.ReadAllText(fileName);
        var configuration = JsonSerializer.Deserialize<Configuration>(jsonString);

        if (configuration is not null) return configuration;

        Console.WriteLine("Configuration empty?");
        Console.ReadKey();
        Environment.Exit(-1);

        // This will never be reached.
        return new Configuration();
    }
}
