using System.Text.Json.Serialization;

namespace CSVReconciliationTool.Core.Models;

public class MatchingRuleConfiguration
{
    public MatchingRuleConfiguration()
    {
        MatchingFields = new List<string>();
    }
    
    [JsonPropertyName("matchingFields")]
    public IList<string>  MatchingFields { get; set; }
    
    [JsonPropertyName("caseSensitive")]
    public bool CaseSensitive { get; set; } = false;

    [JsonPropertyName("trim")] 
    public bool Trim { get; set; } = true;

}