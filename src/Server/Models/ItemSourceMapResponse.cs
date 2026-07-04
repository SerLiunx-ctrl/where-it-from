namespace WhereItFrom.Server.Models;

public record ItemSourceMapResponse
{
    public DateTimeOffset GeneratedAt { get; init; } = DateTimeOffset.UtcNow;
    public int ScannedModCount { get; init; }
    public Dictionary<string, ItemSourceEntry> Items { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}
