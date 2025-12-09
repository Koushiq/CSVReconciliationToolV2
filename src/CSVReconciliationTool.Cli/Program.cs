
using CSVReconciliationTool.Core.Constants;
using CSVReconciliationTool.Service.Logging;

namespace CSVReconciliationTool.Cli;
public static class Program
{
    public static void Main(string[] args)
    {
        var rootFolder = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\..\"));
        var outputFolder = $"{rootFolder}\\{ReconciliationConstants.LogFilePath}";

        var logPath = Path.Combine(outputFolder, $"reconciliation-{DateTime.Now:yyyyMMdd-HHmmss}.log");
        
        using var logger = new Logger(logPath);

        try
        {
            logger.LogInfo("=== CSV Reconciliation Tool ===");
            logger.LogInfo("Logging Starting...");
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Operation was cancelled");
        }
        catch (Exception ex)
        {
            logger.LogError($"error: {ex.Message}", ex);
        }
    }
}