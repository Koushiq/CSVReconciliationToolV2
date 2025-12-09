using System.Runtime.CompilerServices;
using System.Text;
using CSVReconciliationTool.Core.Logging;
using CSVReconciliationTool.Core.Models;
using CSVReconciliationTool.Service.Csv.Interface;

namespace CSVReconciliationTool.Service.Csv;

public class CsvParser : ICsvParser
{
    private readonly ILogger _logger;

    public CsvParser(ILogger logger)
    {
        _logger = logger;
    }

    public async IAsyncEnumerable<CsvRecord> ParseFileAsync(
        string filePath,
        char delimiter = ',',
        bool hasHeader = true,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
        {
            _logger.LogWarning($"File not found: {filePath}");
            yield break;
        }

        using var reader = new StreamReader(filePath, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        
        var headers = new List<string>();
        int lineNumber = 0;

        // Read header line if present
        if (hasHeader)
        {
            var headerLine = await reader.ReadLineAsync(cancellationToken);
            if (headerLine != null)
            {
                lineNumber++;
                headers = ParseLine(headerLine, delimiter);
                _logger.LogDebug($"Parsed headers from {filePath}: {string.Join(", ", headers)}");
            }
        }

        // Read data lines
        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            lineNumber++;

            if (string.IsNullOrWhiteSpace(line))
                continue;

            CsvRecord? record = null;
            try
            {
                var values = ParseLine(line, delimiter);
                
                // If no headers, generate column names
                if (headers.Count == 0)
                {
                    headers = Enumerable.Range(0, values.Count)
                        .Select(i => $"Column{i + 1}")
                        .ToList();
                }

                var fields = new Dictionary<string, string>();
                for (int i = 0; i < values.Count; i++)
                {
                    var headerName = i < headers.Count ? headers[i] : $"Column{i + 1}";
                    fields[headerName] = values[i];
                }

                // Add empty values for missing fields
                foreach (var header in headers.Where(h => !fields.ContainsKey(h)))
                {
                    fields[header] = string.Empty;
                }

                record = new CsvRecord
                {
                    LineNumber = lineNumber,
                    RawLine = line,
                    Fields = fields,
                    SourceFile = filePath
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error parsing line {lineNumber} in {filePath}: {ex.Message}");
                // Skip malformed lines but continue processing
                continue;
            }

            if (record != null)
                yield return record;
        }
    }

    public async Task<List<string>> GetHeadersAsync(
        string filePath,
        char delimiter = ',',
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
        {
            _logger.LogWarning($"File not found: {filePath}");
            return new List<string>();
        }

        using var reader = new StreamReader(filePath, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var headerLine = await reader.ReadLineAsync(cancellationToken);
        
        return headerLine != null ? ParseLine(headerLine, delimiter) : new List<string>();
    }

    private List<string> ParseLine(string line, char delimiter)
    {
        var result = new List<string>();
        var currentField = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (inQuotes)
            {
                if (c == '"')
                {
                    // Check for escaped quote
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        currentField.Append('"');
                        i++; // Skip next quote
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    currentField.Append(c);
                }
            }
            else
            {
                if (c == '"')
                {
                    inQuotes = true;
                }
                else if (c == delimiter)
                {
                    result.Add(currentField.ToString());
                    currentField.Clear();
                }
                else
                {
                    currentField.Append(c);
                }
            }
        }

        // Add last field
        result.Add(currentField.ToString());

        return result;
    }
}