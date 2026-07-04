namespace WhereItFrom.Client.Data;

public class ItemSourceEntry
{
    public string TemplateId { get; set; } = string.Empty;
    public string ModName { get; set; } = string.Empty;
    public string ModFolder { get; set; } = string.Empty;
    public string? SourceFile { get; set; }
    public string Confidence { get; set; } = string.Empty;
}
