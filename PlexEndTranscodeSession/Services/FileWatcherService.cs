using Microsoft.Extensions.Logging;
using PlexEndTranscodeSession.Utilities;
using FileInfo = System.IO.FileInfo;

namespace PlexEndTranscodeSession.Services;

public class FileWatcherService(ILogger<FileWatcherService> logger, Configuration configuration) : IDisposable
{
    public event Func<IEnumerable<string>, CancellationToken, Task>? OnFileChanged;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly string? _logDirectory = Path.GetDirectoryName(configuration.LogFileLocation);
    private readonly string? _logName = Path.GetFileName(configuration.LogFileLocation);
    private long _previousFileSize;
    private CancellationToken _cancellationToken;
    private FileSystemWatcher? _fileWatcherService;

    public void SetupFileWatcher(CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(_logDirectory) || string.IsNullOrWhiteSpace(_logName)) return;

        _cancellationToken = token;
        _fileWatcherService = new FileSystemWatcher(_logDirectory);
        _fileWatcherService.NotifyFilter = NotifyFilters.LastWrite;
        _fileWatcherService.Changed += OnChanged;
        _fileWatcherService.Filter = _logName;
        _fileWatcherService.EnableRaisingEvents = true;

        // Set the previous file size to the current file size to avoid processing the entire file on startup
        _previousFileSize = new FileInfo(Path.Join(_logDirectory, _logName)).Length;
    }

    private void OnChanged(object sender, FileSystemEventArgs e)
    {
        _semaphore.Wait(_cancellationToken);

        try
        {
            var newLines = ReadNewFileLines();
            if (newLines.Count is 0) return;

            Task.Run(() => OnFileChanged?.Invoke(newLines, _cancellationToken).ContinueWith(result =>
            {
                if (!result.IsFaulted) return;
                logger.LogError(result.Exception, "Error invoking file changed event");
            }, _cancellationToken), _cancellationToken).Wait(_cancellationToken);
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
        _fileWatcherService?.Dispose();
    }
}
