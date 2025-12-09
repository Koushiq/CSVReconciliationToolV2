namespace CSVReconciliationTool.Core.Models;

public class ReconciliationConfiguration
{
    public required string FolderA { get; set; }

    public required string FolderB { get; set; }

    public string OutputFolder { get; set; } = ".";

    public required MatchingRuleConfiguration MatchingRule { get; set; }

    public int DegreeOfParallelism { get; set; } = 0;

    public char Delimiter { get; set; } = ',';

    public bool HasHeaderRow { get; set; } = true;

    public FilePairMode FilePairMode { get; set; } = FilePairMode.SingleFile;
}