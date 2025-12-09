namespace CSVReconciliationTool.Core.Models;

public class CsvRecord
{
    public int LineNumber { get; set; }

    public string RawLine { get; set; } = string.Empty;

    public Dictionary<string, string> Fields { get; set; } = new();

    public string SourceFile { get; set; } = string.Empty;
    public string GetMatchKey(IEnumerable<string> matchingFields, bool caseSensitive, bool trim)
    {
        var keyParts = new List<string>();
        
        foreach (var field in matchingFields)
        {
            var value = Fields.TryGetValue(field, out var v) ? v : string.Empty;
            
            if (trim)
                value = value.Trim();
            
            if (!caseSensitive)
                value = value.ToLowerInvariant();
            
            keyParts.Add(value);
        }
        
        return string.Join("|", keyParts);
    }
}