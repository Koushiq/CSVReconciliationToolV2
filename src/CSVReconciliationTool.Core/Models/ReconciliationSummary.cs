using System.Text.Json.Serialization;

namespace CSVReconciliationTool.Core.Models;

public class ReconciliationSummary
{
    [JsonPropertyName("startTime")]
    public DateTime StartTime { get; set; }

    [JsonPropertyName("endTime")]
    public DateTime EndTime { get; set; }

    [JsonPropertyName("totalProcessingTimeMs")]
    public long TotalProcessingTimeMs { get; set; }

    [JsonPropertyName("folderA")]
    public string FolderA { get; set; } = string.Empty;

    [JsonPropertyName("folderB")]
    public string FolderB { get; set; } = string.Empty;

    [JsonPropertyName("comparisonMode")]
    public string ComparisonMode { get; set; } = string.Empty;

    [JsonPropertyName("matchingRuleConfiguration")]
    public MatchingRuleConfiguration? MatchingRuleConfiguration { get; set; }

    [JsonPropertyName("totalFilePairs")]
    public int TotalFilePairs { get; set; }

    [JsonPropertyName("missingFiles")]
    public int MissingFiles { get; set; }

    [JsonPropertyName("totalRecordsInA")]
    public int TotalRecordsInA { get; set; }

    [JsonPropertyName("totalRecordsInB")]
    public int TotalRecordsInB { get; set; }

    [JsonPropertyName("totalMatched")]
    public int TotalMatched { get; set; }

    [JsonPropertyName("totalOnlyInA")]
    public int TotalOnlyInA { get; set; }

    [JsonPropertyName("totalOnlyInB")]
    public int TotalOnlyInB { get; set; }

    [JsonPropertyName("totalErrors")]
    public int TotalErrors { get; set; }

    [JsonPropertyName("filePairResults")]
    public List<FilePairSummary> FilePairResults { get; set; } = new();
}


public class FilePairSummary
{
    [JsonPropertyName("fileA")]
    public string FileA { get; set; } = string.Empty;

    [JsonPropertyName("fileB")]
    public string FileB { get; set; } = string.Empty;

    [JsonPropertyName("fileAMissing")]
    public bool FileAMissing { get; set; }

    [JsonPropertyName("fileBMissing")]
    public bool FileBMissing { get; set; }

    [JsonPropertyName("totalInA")]
    public int TotalInA { get; set; }

    [JsonPropertyName("totalInB")]
    public int TotalInB { get; set; }

    [JsonPropertyName("matched")]
    public int Matched { get; set; }

    [JsonPropertyName("onlyInA")]
    public int OnlyInA { get; set; }

    [JsonPropertyName("onlyInB")]
    public int OnlyInB { get; set; }

    [JsonPropertyName("errors")]
    public int Errors { get; set; }

    [JsonPropertyName("processingTimeMs")]
    public long ProcessingTimeMs { get; set; }
}