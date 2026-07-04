namespace WhereItFrom.Server.Models;

public record ItemSourceEntry
{
    public required string TemplateId { get; init; }
    public required string ModName { get; init; }
    public required string ModFolder { get; init; }
    public string? SourceFile { get; init; }
    public required string Confidence { get; init; }
}
