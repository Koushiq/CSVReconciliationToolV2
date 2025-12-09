using System.Collections.Concurrent;
using System.Text;
using CSVReconciliationTool.Core.Constants;
using CSVReconciliationTool.Core.Logging;

namespace CSVReconciliationTool.Service.Logging;

public class Logger : ILogger , IDisposable
{
    private string _logFilePath;
    private readonly SemaphoreSlim _writeLock = new SemaphoreSlim(1, 1);
    private readonly ConcurrentQueue<string> _logQueue = new();
    private bool _disposed;

    #region Methods

    public void LogDebug(string message) => Log(LogLevel.Debug, message);
    public void LogInfo(string message) => Log(LogLevel.Info, message);
    public void LogWarning(string message) => Log(LogLevel.Warning, message);
    public void LogError(string message) => Log(LogLevel.Error, message);
    public void LogError(string message, Exception ex) => Log(LogLevel.Error, $"{message}: {ex}");

    #endregion
    
    public Logger(string logFilePath)
    {
        _logFilePath = logFilePath;
        var dir = Path.GetDirectoryName(_logFilePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        
        //initate log header
        File.WriteAllText(_logFilePath, $"=== CSV Reconciliation Tool Log - Started {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===\n");
    }
    private void Log(LogLevel logLevel, string message)
    {
        Console.WriteLine($"{logLevel}: {message}");
        
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var logLine = $"[{timestamp}] [{logLevel,-7}] {message}";
        
        _logQueue.Enqueue(logLine);
        
        var originalColor = Console.ForegroundColor;
        Console.ForegroundColor = logLevel switch
        {
            LogLevel.Debug => ConsoleColor.Gray,
            LogLevel.Info => ConsoleColor.White,
            LogLevel.Warning => ConsoleColor.Yellow,
            LogLevel.Error => ConsoleColor.Red,
            _ => ConsoleColor.White
        };
        Console.WriteLine(logLine);
        Console.ForegroundColor = originalColor;
    }
    
    private async Task FlushToFileAsync()
    {
        if (_disposed) return;

        await _writeLock.WaitAsync();
        try
        {
            var sb = new StringBuilder();
            while (_logQueue.TryDequeue(out var line))
            {
                sb.AppendLine(line);
            }

            if (sb.Length > 0)
            {
                await File.AppendAllTextAsync(_logFilePath, sb.ToString());
            }
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _writeLock.Wait();
        try
        {
            var sb = new StringBuilder();
            while (_logQueue.TryDequeue(out var line))
            {
                sb.AppendLine(line);
            }

            if (sb.Length > 0)
            {
                File.AppendAllText(_logFilePath, sb.ToString());
            }

            File.AppendAllText(_logFilePath, $"\n=== Log Ended {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===\n");
        }
        finally
        {
            _writeLock.Release();
        }

        _writeLock.Dispose();
    }
}