using System.Collections.Concurrent;
using System.Diagnostics;
using CSVReconciliationTool.Core;
using CSVReconciliationTool.Core.Logging;
using CSVReconciliationTool.Core.Models;
using CSVReconciliationTool.Service.Csv.Interface;
using CSVReconciliationTool.Service.Reconciliation.Interfaces;

namespace CSVReconciliationTool.Service.Reconciliation;

public class ReconciliationEngineService : IReconciliationEngineService
{
    private readonly ICsvParser _csvParser;
    private readonly IResultGenerator _resultGenerator;
    private readonly ILogger _logger;
    public ReconciliationEngineService(
        ICsvParser csvParser, 
        IResultGenerator resultGenerator, 
        ILogger logger)
    {
        _csvParser = csvParser;
        _resultGenerator = resultGenerator;
        _logger = logger;
    }
    public async Task<ReconciliationSummary> RunAsync(
        ReconciliationConfiguration configuration,
        CancellationToken cancellationToken = default
        )
    {
        var summary = new ReconciliationSummary
        {
            StartTime = DateTime.Now,
            FolderA = configuration.FolderA,
            FolderB = configuration.FolderB,
            ComparisonMode = configuration.FilePairMode.ToString(),
            MatchingRuleConfiguration = configuration.MatchingRule
        };

        var stopwatch = Stopwatch.StartNew();

        _logger.LogInfo($"Starting reconciliation: FolderA={configuration.FolderA}, FolderB={configuration.FolderB}");
        _logger.LogInfo($"Comparison mode: {configuration.FilePairMode}");
        _logger.LogInfo($"Matching fields: {string.Join(", ", configuration.MatchingRule.MatchingFields)}");
        _logger.LogInfo($"Case sensitive: {configuration.MatchingRule.CaseSensitive}, Trim: {configuration.MatchingRule.Trim}");
          var filePairs = GetFilePairs(configuration);
        summary.TotalFilePairs = filePairs.Count;

        _logger.LogInfo($"Found {filePairs.Count} file pairs to process");

        // Determine parallelism
        var maxParallelism = configuration.DegreeOfParallelism > 0
            ? configuration.DegreeOfParallelism
            : Environment.ProcessorCount;

        _logger.LogInfo($"Using degree of parallelism: {maxParallelism}");

        // Process file pairs in parallel using Task-based model
        var results = new ConcurrentBag<FilePairResult>();
        var semaphore = new SemaphoreSlim(maxParallelism);

        var tasks = filePairs.Select(async pair =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                _logger.LogInfo($"[Task {Environment.CurrentManagedThreadId}] Starting reconciliation: {Path.GetFileName(pair.FileA)} <-> {Path.GetFileName(pair.FileB)}");
                
                var result = await ReconcileFilePairAsync(pair.FileA, pair.FileB, configuration, cancellationToken);
                results.Add(result);

                // Write outputs for this file pair
                await _resultGenerator.WriteFilePairResultsAsync(result, configuration.OutputFolder, configuration.Delimiter, cancellationToken);

                _logger.LogInfo($"[Task {Environment.CurrentManagedThreadId}] Completed: {Path.GetFileName(pair.FileA)} - Matched: {result.MatchedCount}, OnlyInA: {result.OnlyInACount}, OnlyInB: {result.OnlyInBCount}");
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);

        stopwatch.Stop();

        // Aggregate results
        foreach (var result in results)
        {
            summary.TotalRecordsInA += result.TotalInA;
            summary.TotalRecordsInB += result.TotalInB;
            summary.TotalMatched += result.MatchedCount;
            summary.TotalOnlyInA += result.OnlyInACount;
            summary.TotalOnlyInB += result.OnlyInBCount;
            summary.TotalErrors += result.Errors.Count;

            if (result.FileAMissing || result.FileBMissing)
                summary.MissingFiles++;

            summary.FilePairResults.Add(new FilePairSummary
            {
                FileA = result.FileA,
                FileB = result.FileB,
                FileAMissing = result.FileAMissing,
                FileBMissing = result.FileBMissing,
                TotalInA = result.TotalInA,
                TotalInB = result.TotalInB,
                Matched = result.MatchedCount,
                OnlyInA = result.OnlyInACount,
                OnlyInB = result.OnlyInBCount,
                Errors = result.Errors.Count,
                ProcessingTimeMs = result.ProcessingTimeMs
            });
        }

        summary.EndTime = DateTime.Now;
        summary.TotalProcessingTimeMs = stopwatch.ElapsedMilliseconds;

        // Write global summary
        await _resultGenerator.WriteGlobalSummaryAsync(summary, configuration.OutputFolder, cancellationToken);

        _logger.LogInfo($"Reconciliation complete. Total time: {summary.TotalProcessingTimeMs}ms");
        _logger.LogInfo($"Total matched: {summary.TotalMatched}, Only in A: {summary.TotalOnlyInA}, Only in B: {summary.TotalOnlyInB}");

        return summary;
    }

    private async Task<FilePairResult> ReconcileFilePairAsync(
        string fileA,
        string fileB,
        ReconciliationConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new FilePairResult
        {
            FileA = Path.GetFileName(fileA),
            FileB = Path.GetFileName(fileB)
        };

        // Check for missing files
        bool fileAExists = File.Exists(fileA);
        bool fileBExists = File.Exists(fileB);

        if (!fileAExists)
        {
            result.FileAMissing = true;
            _logger.LogWarning($"File A missing: {fileA}");
        }

        if (!fileBExists)
        {
            result.FileBMissing = true;
            _logger.LogWarning($"File B missing: {fileB}");
        }

        if (!fileAExists && !fileBExists)
        {
            result.ProcessingTimeMs = stopwatch.ElapsedMilliseconds;
            return result;
        }

        var config = configuration.MatchingRule;
        
        // Get headers from both files
        var headersA = fileAExists 
            ? await _csvParser.GetHeadersAsync(fileA, configuration.Delimiter, cancellationToken)
            : new List<string>();
        var headersB = fileBExists 
            ? await _csvParser.GetHeadersAsync(fileB, configuration.Delimiter, cancellationToken)
            : new List<string>();

        // Union of headers
        result.AllHeaders = headersA.Union(headersB).Distinct().ToList();

        // Validate matching fields exist
        var missingFieldsA = config.MatchingFields.Except(headersA).ToList();
        var missingFieldsB = config.MatchingFields.Except(headersB).ToList();

        if (fileAExists && missingFieldsA.Any())
        {
            _logger.LogWarning($"File A ({fileA}) missing matching fields: {string.Join(", ", missingFieldsA)}");
        }

        if (fileBExists && missingFieldsB.Any())
        {
            _logger.LogWarning($"File B ({fileB}) missing matching fields: {string.Join(", ", missingFieldsB)}");
        }

        // Build dictionaries for matching using concurrent collections
        var recordsA = new ConcurrentDictionary<string, List<CsvRecord>>();
        var recordsB = new ConcurrentDictionary<string, List<CsvRecord>>();

        // Load records from both files in parallel
        var loadTasks = new List<Task>();

        if (fileAExists)
        {
            loadTasks.Add(LoadRecordsAsync(fileA, recordsA, configuration, result, "A", cancellationToken));
        }

        if (fileBExists)
        {
            loadTasks.Add(LoadRecordsAsync(fileB, recordsB, configuration, result, "B", cancellationToken));
        }

        await Task.WhenAll(loadTasks);

        result.TotalInA = recordsA.Values.Sum(list => list.Count);
        result.TotalInB = recordsB.Values.Sum(list => list.Count);

        // Match records
        var matchedKeys = new HashSet<string>();

        foreach (var kvp in recordsA)
        {
            if (recordsB.TryGetValue(kvp.Key, out var matchingRecordsB))
            {
                matchedKeys.Add(kvp.Key);
                
                // Create matched records (pair first records from each side)
                foreach (var recA in kvp.Value)
                {
                    var recB = matchingRecordsB.FirstOrDefault();
                    if (recB != null)
                    {
                        result.MatchedRecords.Add(new MatchedRecord
                        {
                            MatchKey = kvp.Key,
                            RecordA = recA,
                            RecordB = recB
                        });
                    }
                }
            }
            else
            {
                result.OnlyInA.AddRange(kvp.Value);
            }
        }

        // Find records only in B
        foreach (var kvp in recordsB)
        {
            if (!matchedKeys.Contains(kvp.Key))
            {
                result.OnlyInB.AddRange(kvp.Value);
            }
        }

        result.MatchedCount = result.MatchedRecords.Count;
        result.OnlyInACount = result.OnlyInA.Count;
        result.OnlyInBCount = result.OnlyInB.Count;

        stopwatch.Stop();
        result.ProcessingTimeMs = stopwatch.ElapsedMilliseconds;

        return result;
    }

    private async Task LoadRecordsAsync(
        string filePath,
        ConcurrentDictionary<string, List<CsvRecord>> records,
        ReconciliationConfiguration configuration,
        FilePairResult result,
        string fileLabel,
        CancellationToken cancellationToken)
    {
        var config = configuration.MatchingRule;

        await foreach (var record in _csvParser.ParseFileAsync(filePath, configuration.Delimiter, configuration.HasHeaderRow, cancellationToken))
        {
            try
            {
                var key = record.GetMatchKey(config.MatchingFields, config.CaseSensitive, config.Trim);
                
                records.AddOrUpdate(
                    key,
                    _ => new List<CsvRecord> { record },
                    (_, existing) =>
                    {
                        lock (existing)
                        {
                            existing.Add(record);
                        }
                        return existing;
                    });
            }
            catch (Exception ex)
            {
                var error = new ProcessingError
                {
                    SourceFile = filePath,
                    LineNumber = record.LineNumber,
                    Message = ex.Message,
                    RawLine = record.RawLine
                };
                
                lock (result.Errors)
                {
                    result.Errors.Add(error);
                }
                
                _logger.LogError($"Error processing record in {fileLabel} at line {record.LineNumber}: {ex.Message}");
            }
        }
    }

    private List<(string FileA, string FileB)> GetFilePairs(ReconciliationConfiguration configuration)
    {
        var pairs = new List<(string FileA, string FileB)>();

        var filesA = Directory.Exists(configuration.FolderA)
            ? Directory.GetFiles(configuration.FolderA, "*.csv")
            : Array.Empty<string>();

        var filesB = Directory.Exists(configuration.FolderB)
            ? Directory.GetFiles(configuration.FolderB, "*.csv")
            : Array.Empty<string>();

        if (configuration.FilePairMode == FilePairMode.SingleFile)
        {
            // Pair by single file
            var fileNamesA = filesA.Select(f => Path.GetFileName(f)).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var fileNamesB = filesB.Select(f => Path.GetFileName(f)).ToHashSet(StringComparer.OrdinalIgnoreCase);

            var allFileNames = fileNamesA.Union(fileNamesB);

            foreach (var fileName in allFileNames)
            {
                var pathA = Path.Combine(configuration.FolderA, fileName);
                var pathB = Path.Combine(configuration.FolderB, fileName);
                pairs.Add((pathA, pathB));
            }
        }
        else // AllFile
        {
            foreach (var fileA in filesA)
            {
                foreach (var fileB in filesB)
                {
                    pairs.Add((fileA, fileB));
                }
            }
        }

        return pairs;
    }
}