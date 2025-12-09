namespace CSVReconciliationTool.Core;

public class MatchingRuleModel
{
    public MatchingRuleModel()
    {
        MatchingFields = new List<string>();
    }

    public IList<string>  MatchingFields { get; set; }
    
    public bool CaseSensitive { get; set; }
    
    public bool Trim { get; set; }
    
}