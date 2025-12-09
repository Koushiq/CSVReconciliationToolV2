using CSVReconciliationTool.Core.Models;

namespace CSVReconciliationTool.Service.Csv.Interface;

public interface ICsvParser
{
    IAsyncEnumerable<CsvRecord> ParseFileAsync(
        string filePath, 
        char delimiter = ',', 
        bool hasHeader = true,
        CancellationToken cancellationToken = default);

    Task<List<string>> GetHeadersAsync(
        string filePath, 
        char delimiter = ',',
        CancellationToken cancellationToken = default);
}