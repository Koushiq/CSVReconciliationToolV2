using CSVReconciliationTool.Core.Logging;
using CSVReconciliationTool.Service.Csv.Interface;
using CSVReconciliationTool.Service.Reconciliation.Interfaces;

namespace CSVReconciliationTool.Service.Reconciliation;

public class ReconciliationEngineService : IReconciliationEngineService
{
    private readonly ICsvParser _csvParser;
    private readonly ILogger _logger;
    public ReconciliationEngineService()
    {
        
    }
    public Task RunAsync()
    {
        throw new NotImplementedException();
    }
}