using System.Text.Json;

namespace WhereItFrom.Server.Services;

public static class ModMetadataReader
{
    public static string GetDisplayName(string modDirectory)
    {
        foreach (var fileName in MetadataFileNames)
        {
            var path = Path.Combine(modDirectory, fileName);
            if (!File.Exists(path))
            {
                continue;
            }

            var name = TryReadName(path);
            if (!string.IsNullOrWhiteSpace(name))
            {
                return name;
            }
        }

        return Path.GetFileName(modDirectory);
    }

    private static string? TryReadName(string path)
    {
        try
        {
            using var stream = File.OpenRead(path);
            using var document = JsonDocument.Parse(stream, JsonOptions);

            foreach (var propertyName in NamePropertyNames)
            {
                if (TryGetString(document.RootElement, propertyName, out var value))
                {
                    return value;
                }
            }
        }
        catch
        {
            return null;
        }

        return null;
    }

    private static bool TryGetString(JsonElement obj, string propertyName, out string value)
    {
        value = string.Empty;

        if (obj.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        foreach (var property in obj.EnumerateObject())
        {
            if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase)
                && property.Value.ValueKind == JsonValueKind.String)
            {
                value = property.Value.GetString() ?? string.Empty;
                return !string.IsNullOrWhiteSpace(value);
            }
        }

        return false;
    }

    private static readonly string[] MetadataFileNames =
    [
        "package.json",
        "manifest.json",
        "mod.json"
    ];

    private static readonly string[] NamePropertyNames =
    [
        "displayName",
        "name",
        "Name",
        "modName"
    ];

    private static readonly JsonDocumentOptions JsonOptions = new()
    {
        CommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };
}
