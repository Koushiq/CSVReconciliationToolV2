namespace CSVReconciliationTool.Core.Models;

public class FilePairResult
{
    public string FileA { get; set; } = string.Empty;

    public string FileB { get; set; } = string.Empty;

    public int TotalInA { get; set; }

    public int TotalInB { get; set; }

    public int MatchedCount { get; set; }

    public int OnlyInACount { get; set; }

    public int OnlyInBCount { get; set; }

    public long ProcessingTimeMs { get; set; }

    public List<MatchedRecord> MatchedRecords { get; set; } = new();
  
    public List<CsvRecord> OnlyInA { get; set; } = new();

    public List<CsvRecord> OnlyInB { get; set; } = new();

    public List<ProcessingError> Errors { get; set; } = new();

    public bool FileAMissing { get; set; }

    public bool FileBMissing { get; set; }

    public List<string> AllHeaders { get; set; } = new();
}

public class MatchedRecord
{
    public string MatchKey { get; set; } = string.Empty;

    public CsvRecord RecordA { get; set; } = new();

    public CsvRecord RecordB { get; set; } = new();
}

public class ProcessingError
{
    public string SourceFile { get; set; } = string.Empty;

    public int LineNumber { get; set; }

    public string Message { get; set; } = string.Empty;

    public string RawLine { get; set; } = string.Empty;
}