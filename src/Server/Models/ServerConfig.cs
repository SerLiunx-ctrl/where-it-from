namespace WhereItFrom.Server.Models;

public record ServerConfig
{
    public bool IncludeDynamicIdHeuristics { get; init; }
    public Dictionary<string, string> ManualMappings { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public List<string> IgnoredModFolders { get; init; } = [];
}
