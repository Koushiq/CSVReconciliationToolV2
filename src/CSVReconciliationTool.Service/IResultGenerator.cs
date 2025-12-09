using CSVReconciliationTool.Core.Models;

namespace CSVReconciliationTool.Service;

public interface IResultGenerator
{
    Task WriteFilePairResultsAsync(
        FilePairResult result,
        string outputFolder,
        char delimiter = ',',
        CancellationToken cancellationToken = default);

    Task WriteGlobalSummaryAsync(
        ReconciliationSummary summary,
        string outputFolder,
        CancellationToken cancellationToken = default);
}