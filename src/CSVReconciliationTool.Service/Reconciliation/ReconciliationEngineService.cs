using CSVReconciliationTool.Core.Logging;
using CSVReconciliationTool.Service.Csv.Interface;
using CSVReconciliationTool.Service.Reconciliation.Interfaces;

namespace CSVReconciliationTool.Service.Reconciliation;

public class ReconciliationEngineService : IReconciliationEngineService
{
    private readonly ICsvParser _csvParser;
    private readonly IResultGenerator _resultGenerator;
    private readonly ILogger _logger;
    public ReconciliationEngineService(ICsvParser csvParser, IResultGenerator resultGenerator, ILogger logger)
    {
        _csvParser = csvParser;
        _resultGenerator = resultGenerator;
        _logger = logger;
    }
    public Task RunAsync()
    {
        throw new NotImplementedException();
    }
}