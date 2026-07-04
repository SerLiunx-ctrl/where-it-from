namespace WhereItFrom.Client.Data;

public class ItemSourceMapResponse
{
    public Dictionary<string, ItemSourceEntry> Items { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public DateTimeOffset GeneratedAt { get; set; }
    public int ScannedModCount { get; set; }
}
