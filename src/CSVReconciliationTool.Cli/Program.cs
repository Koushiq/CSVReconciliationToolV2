
using CSVReconciliationTool.Core;
using CSVReconciliationTool.Core.Constants;
using CSVReconciliationTool.Core.Models;
using CSVReconciliationTool.Service;
using CSVReconciliationTool.Service.Csv;
using CSVReconciliationTool.Service.Logging;
using CSVReconciliationTool.Service.Reconciliation;

namespace CSVReconciliationTool.Cli;
public static class Program
{
    public static async Task Main(string[] args)
    {
        var rootFolder = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\..\"));
        
        var outputFolder = $"{rootFolder}\\{ReconciliationConstants.LogFilePath}";
        var configPath = $"{rootFolder}\\{ReconciliationConstants.CaseSentiveMatchJson}";
        var folderA = $"{rootFolder}\\{ReconciliationConstants.FolderA}";
        var folderB = $"{rootFolder}\\{ReconciliationConstants.FolderB}";
        var parallelism = 0;
        var delimiter = ',';
        
        await RunReconciliation(
            folderA, folderB, configPath, outputFolder,
            parallelism, delimiter, false, true);
        
    }

    private static async Task RunReconciliation(
        string folderA,
        string folderB,
        string configPath,
        string outputFolder,
        int parallelism,
        char delimiter,
        bool hasHeader,
        bool allToAll)
    {
        

        var logPath = Path.Combine(outputFolder, $"reconciliation-{DateTime.Now:yyyyMMdd-HHmmss}.log");
        
        using var logger = new Logger(logPath);
        
        try
        {
            logger.LogInfo("=== CSV Reconciliation Tool ===");
            logger.LogInfo("Logging Starting...");
            
            // Validate folders
            if (!Directory.Exists(folderA))
            {
                var errText = $"Folder A does not exist: {folderA}";
                logger.LogError(errText);
                throw new DirectoryNotFoundException(errText);
            }

            if (!Directory.Exists(folderB))
            {
                var errText = $"Folder B does not exist: {folderB}";
                logger.LogError(errText);
                throw new DirectoryNotFoundException(errText);
            }

            // Load configuration
            logger.LogInfo($"Loading configuration from: {configPath}");
            MatchingRuleConfiguration matchingRule = null;
            try
            {
                matchingRule = ConfigurationLoader.LoadFromFile(configPath);
                logger.LogInfo($"Matching fields: {string.Join(", ", matchingRule.MatchingFields)}");
                logger.LogInfo($"Case sensitive: {matchingRule.CaseSensitive}");
                logger.LogInfo($"Trim whitespace: {matchingRule.Trim}");
            }
            catch (Exception ex)
            {
                logger.LogError($"Failed to load configuration: {ex.Message}");
            }

            // Build options
            var options = new ReconciliationConfiguration()
            {
                FolderA = Path.GetFullPath(folderA),
                FolderB = Path.GetFullPath(folderB),
                OutputFolder = Path.GetFullPath(outputFolder),
                MatchingRule = matchingRule,
                DegreeOfParallelism = parallelism,
                Delimiter = delimiter,
                HasHeaderRow = hasHeader,
                FilePairMode = allToAll ? FilePairMode.AllFile : FilePairMode.SingleFile
            };

            // Create services
            var csvParser = new CsvParser(logger);
           
            //var reconciliationEngine = new ReconciliationEngineService();
            
            // Run reconciliation
            logger.LogInfo("");
            logger.LogInfo("Starting reconciliation process...");
            logger.LogInfo("");

            
            
            
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