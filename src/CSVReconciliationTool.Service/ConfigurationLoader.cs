using System.Text.Json;
using CSVReconciliationTool.Core.Models;

namespace CSVReconciliationTool.Service;

public static class ConfigurationLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    public static MatchingRuleConfiguration LoadFromFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Configuration file not found: {filePath}");
        }

        var json = File.ReadAllText(filePath);
        return LoadFromJson(json);
    }

  
    public static MatchingRuleConfiguration LoadFromJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new ArgumentException("JSON configuration cannot be empty", nameof(json));
        }

        var config = JsonSerializer.Deserialize<MatchingRuleConfiguration>(json, JsonOptions)
            ?? throw new InvalidOperationException("Failed to deserialize configuration");

        Validate(config);

        return config;
    }

    public static void Validate(MatchingRuleConfiguration config)
    {
        if (config.MatchingFields == null || config.MatchingFields.Count == 0)
        {
            throw new ArgumentException("At least one matching field must be specified");
        }

        if (config.MatchingFields.Any(string.IsNullOrWhiteSpace))
        {
            throw new ArgumentException("Matching field names cannot be empty or whitespace");
        }

        // Check for duplicate field names
        var duplicates = config.MatchingFields
            .GroupBy(f => f, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicates.Any())
        {
            throw new ArgumentException($"Duplicate matching fields: {string.Join(", ", duplicates)}");
        }
    }
}

