using CSVReconciliationTool.Core.Models;

namespace CSVReconciliationTool.Service.Reconciliation.Interfaces;

public interface IReconciliationEngineService
{
    Task<ReconciliationSummary> RunAsync(
        ReconciliationConfiguration config,
        CancellationToken cancellationToken = default );
}