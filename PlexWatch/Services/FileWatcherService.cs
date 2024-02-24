using Microsoft.Extensions.Logging;
using PlexWatch.Utilities;
using FileInfo = System.IO.FileInfo;

namespace PlexWatch.Services;

public class FileWatcherService(ILogger<FileWatcherService> logger, Configuration configuration, EventParsingService eventParsingService)
    : IDisposable
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly string? _logDirectory = Path.GetDirectoryName(configuration.LogFileLocation);
    private readonly string? _logName = Path.GetFileName(configuration.LogFileLocation);
    private long _previousFileSize;
    private CancellationToken _cancellationToken;
    private FileSystemWatcher? _fileSystemWatcher;

    public void SetupFileWatcher(CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(_logDirectory) || string.IsNullOrWhiteSpace(_logName)) return;

        _cancellationToken = token;
        _fileSystemWatcher = new FileSystemWatcher(_logDirectory);
        _fileSystemWatcher.NotifyFilter = NotifyFilters.LastWrite;
        _fileSystemWatcher.Changed += OnChanged;
        _fileSystemWatcher.Filter = _logName;
        _fileSystemWatcher.EnableRaisingEvents = true;

        // Set the previous file size to the current file size to avoid processing the entire file on startup
        _previousFileSize = new FileInfo(Path.Join(_logDirectory, _logName)).Length;
    }

    private void OnChanged(object sender, FileSystemEventArgs fileSystemEventArgs)
    {
        _semaphore.Wait(_cancellationToken);

        try
        {
            var newLines = ReadNewFileLines();
            if (newLines.Count is 0) return;

            eventParsingService.OnFileChanged(newLines);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error processing file changed event");
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private List<string> ReadNewFileLines()
    {
        var file = Path.Join(_logDirectory, _logName);
        List<string> logLines = [];

        var length = new FileInfo(file).Length;
        // If the file was truncated, reset the previous file size to 0
        if (length < _previousFileSize) _previousFileSize = 0;

        using var fileStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        fileStream.Seek(_previousFileSize, SeekOrigin.Begin);

        using var streamReader = new StreamReader(fileStream);
        while (streamReader.ReadLine() is { } line)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            logLines.Add(line);
        }

        _previousFileSize = length;
        return logLines;
    }

    public void Dispose()
    {
        _semaphore.Dispose();
        _fileSystemWatcher?.Dispose();
    }
}
