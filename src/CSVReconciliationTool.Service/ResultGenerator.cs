using System.Text;
using System.Text.Json;
using CSVReconciliationTool.Core.Logging;
using CSVReconciliationTool.Core.Models;

namespace CSVReconciliationTool.Service;

public class ResultGenerator : IResultGenerator
{
    private readonly ILogger _logger;
    private static readonly SemaphoreSlim _writeLock = new(1, 1);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ResultGenerator(ILogger logger)
    {
        _logger = logger;
    }

    public async Task WriteFilePairResultsAsync(
        FilePairResult result,
        string outputFolder,
        char delimiter = ',',
        CancellationToken cancellationToken = default)
    {
        // Create subfolder for this file pair
        var pairFolderName = GetSafeFolderName(result.FileA, result.FileB);
        var pairFolder = Path.Combine(outputFolder, pairFolderName);

        await _writeLock.WaitAsync(cancellationToken);
        try
        {
            Directory.CreateDirectory(pairFolder);
        }
        finally
        {
            _writeLock.Release();
        }

        var tasks = new List<Task>();

        // Write matched.csv
        if (result.MatchedRecords.Any())
        {
            tasks.Add(WriteMatchedCsvAsync(
                Path.Combine(pairFolder, "matched.csv"),
                result.MatchedRecords,
                result.AllHeaders,
                delimiter,
                cancellationToken));
        }

        // Write only-in-folderA.csv
        if (result.OnlyInA.Any())
        {
            tasks.Add(WriteCsvRecordsAsync(
                Path.Combine(pairFolder, "only-in-folderA.csv"),
                result.OnlyInA,
                result.AllHeaders,
                delimiter,
                cancellationToken));
        }

        // Write only-in-folderB.csv
        if (result.OnlyInB.Any())
        {
            tasks.Add(WriteCsvRecordsAsync(
                Path.Combine(pairFolder, "only-in-folderB.csv"),
                result.OnlyInB,
                result.AllHeaders,
                delimiter,
                cancellationToken));
        }

        // Write errors.csv if any
        if (result.Errors.Any())
        {
            tasks.Add(WriteErrorsCsvAsync(
                Path.Combine(pairFolder, "errors.csv"),
                result.Errors,
                delimiter,
                cancellationToken));
        }

        // Write reconcile-summary.json
        var pairSummary = new
        {
            fileA = result.FileA,
            fileB = result.FileB,
            fileAMissing = result.FileAMissing,
            fileBMissing = result.FileBMissing,
            totalInA = result.TotalInA,
            totalInB = result.TotalInB,
            matched = result.MatchedCount,
            onlyInA = result.OnlyInACount,
            onlyInB = result.OnlyInBCount,
            errors = result.Errors.Count,
            processingTimeMs = result.ProcessingTimeMs
        };

        tasks.Add(WriteJsonAsync(
            Path.Combine(pairFolder, "reconcile-summary.json"),
            pairSummary,
            cancellationToken));

        await Task.WhenAll(tasks);

        _logger.LogInfo($"Wrote output files to: {pairFolder}");
    }

    /// <inheritdoc />
    public async Task WriteGlobalSummaryAsync(
        ReconciliationSummary summary,
        string outputFolder,
        CancellationToken cancellationToken = default)
    {
        var summaryPath = Path.Combine(outputFolder, "global-summary.json");
        await WriteJsonAsync(summaryPath, summary, cancellationToken);
        _logger.LogInfo($"Wrote global summary to: {summaryPath}");
    }

    private async Task WriteMatchedCsvAsync(
        string filePath,
        List<MatchedRecord> records,
        List<string> headers,
        char delimiter,
        CancellationToken cancellationToken)
    {
        var sb = new StringBuilder();

        // Create headers for both sides
        var headerLineItems = new List<string>();
        headerLineItems.AddRange(headers.Select(h => $"A_{h}"));
        headerLineItems.AddRange(headers.Select(h => $"B_{h}"));
        sb.AppendLine(string.Join(delimiter.ToString(), headerLineItems.Select(EscapeCsvField)));

        foreach (var record in records)
        {
            var lineItems = new List<string>();
            
            // Add values from A
            foreach (var header in headers)
            {
                var value = record.RecordA.Fields.TryGetValue(header, out var v) ? v : string.Empty;
                lineItems.Add(value);
            }
            
            // Add values from B
            foreach (var header in headers)
            {
                var value = record.RecordB.Fields.TryGetValue(header, out var v) ? v : string.Empty;
                lineItems.Add(value);
            }

            sb.AppendLine(string.Join(delimiter.ToString(), lineItems.Select(EscapeCsvField)));
        }

        await File.WriteAllTextAsync(filePath, sb.ToString(), cancellationToken);
    }

    private async Task WriteCsvRecordsAsync(
        string filePath,
        List<CsvRecord> records,
        List<string> headers,
        char delimiter,
        CancellationToken cancellationToken)
    {
        var sb = new StringBuilder();

        // Write header
        sb.AppendLine(string.Join(delimiter.ToString(), headers.Select(EscapeCsvField)));

        // Write records
        foreach (var record in records)
        {
            var lineItems = headers.Select(h => 
                record.Fields.TryGetValue(h, out var v) ? v : string.Empty);
            sb.AppendLine(string.Join(delimiter.ToString(), lineItems.Select(EscapeCsvField)));
        }

        await File.WriteAllTextAsync(filePath, sb.ToString(), cancellationToken);
    }

    private async Task WriteErrorsCsvAsync(
        string filePath,
        List<ProcessingError> errors,
        char delimiter,
        CancellationToken cancellationToken)
    {
        var sb = new StringBuilder();

        // Header
        sb.AppendLine(string.Join(delimiter.ToString(), 
            new[] { "SourceFile", "LineNumber", "Message", "RawLine" }.Select(EscapeCsvField)));

        foreach (var error in errors)
        {
            var lineItems = new[]
            {
                error.SourceFile,
                error.LineNumber.ToString(),
                error.Message,
                error.RawLine
            };
            sb.AppendLine(string.Join(delimiter.ToString(), lineItems.Select(EscapeCsvField)));
        }

        await File.WriteAllTextAsync(filePath, sb.ToString(), cancellationToken);
    }

    private async Task WriteJsonAsync<T>(string filePath, T obj, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(obj, JsonOptions);
        await File.WriteAllTextAsync(filePath, json, cancellationToken);
    }

    private string EscapeCsvField(string field)
    {
        if (string.IsNullOrEmpty(field))
            return string.Empty;

        // Escape if contains delimiter, quotes, or newlines
        if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }

        return field;
    }

    private string GetSafeFolderName(string fileA, string fileB)
    {
        var nameA = Path.GetFileNameWithoutExtension(fileA);
        var nameB = Path.GetFileNameWithoutExtension(fileB);

        if (nameA.Equals(nameB, StringComparison.OrdinalIgnoreCase))
        {
            return SanitizeFolderName(nameA);
        }

        return SanitizeFolderName($"{nameA}_vs_{nameB}");
    }

    private string SanitizeFolderName(string name)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var result = new StringBuilder();
        
        foreach (var c in name)
        {
            result.Append(invalidChars.Contains(c) ? '_' : c);
        }

        return result.ToString();
    }
}