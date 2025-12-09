using System.Collections.Concurrent;
using System.Diagnostics;
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
    public Task<ReconciliationSummary> RunAsync(
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


        return Task.FromResult( summary);

    }
}