using System.Text.Json;
using System.Text.RegularExpressions;

namespace WhereItFrom.Server.Services;

public static partial class JsonItemTemplateScanner
{
    public static IEnumerable<ItemTemplateMatch> Scan(string modDirectory, bool includeDynamicIdHeuristics)
    {
        foreach (var file in Directory.EnumerateFiles(modDirectory, "*.json", SearchOption.AllDirectories))
        {
            if (IsMetadataFile(file))
            {
                continue;
            }

            foreach (var match in ScanFile(file, includeDynamicIdHeuristics))
            {
                yield return match;
            }
        }
    }

    private static IEnumerable<ItemTemplateMatch> ScanFile(string file, bool includeDynamicIdHeuristics)
    {
        JsonDocument document;

        try
        {
            using var stream = File.OpenRead(file);
            document = JsonDocument.Parse(stream, JsonOptions);
        }
        catch
        {
            yield break;
        }

        using (document)
        {
            var matches = new Dictionary<string, ItemTemplateMatch>(StringComparer.OrdinalIgnoreCase);
            VisitElement(document.RootElement, file, includeDynamicIdHeuristics, matches);

            foreach (var match in matches.Values)
            {
                yield return match;
            }
        }
    }

    private static void VisitElement(
        JsonElement element,
        string file,
        bool includeDynamicIdHeuristics,
        Dictionary<string, ItemTemplateMatch> matches)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                VisitObject(element, file, includeDynamicIdHeuristics, matches);
                break;

            case JsonValueKind.Array:
                foreach (var child in element.EnumerateArray())
                {
                    VisitElement(child, file, includeDynamicIdHeuristics, matches);
                }

                break;
        }
    }

    private static void VisitObject(
        JsonElement obj,
        string file,
        bool includeDynamicIdHeuristics,
        Dictionary<string, ItemTemplateMatch> matches)
    {
        if (TryGetString(obj, "_id", out var objectId) && IsMongoId(objectId) && LooksLikeItemTemplate(obj))
        {
            Add(matches, objectId, file, "json-template");
        }

        if (includeDynamicIdHeuristics && TryGetDynamicItemId(obj, out var dynamicId))
        {
            Add(matches, dynamicId, file, "dynamic-id-heuristic");
        }

        foreach (var property in obj.EnumerateObject())
        {
            if (IsMongoId(property.Name)
                && property.Value.ValueKind == JsonValueKind.Object
                && LooksLikeItemTemplate(property.Value))
            {
                Add(matches, property.Name, file, "json-template-key");
            }

            if (IsMongoId(property.Name)
                && property.Value.ValueKind == JsonValueKind.Object
                && LooksLikeCloneDefinition(property.Value))
            {
                Add(matches, property.Name, file, "json-clone-definition-key");
            }

            VisitElement(property.Value, file, includeDynamicIdHeuristics, matches);
        }
    }

    private static bool LooksLikeItemTemplate(JsonElement obj)
    {
        return obj.TryGetProperty("_props", out _)
            && (obj.TryGetProperty("_parent", out _) || obj.TryGetProperty("_type", out _));
    }

    private static bool LooksLikeCloneDefinition(JsonElement obj)
    {
        return HasPropertyIgnoreCase(obj, "itemTplToClone")
            && HasPropertyIgnoreCase(obj, "parentId")
            && HasPropertyIgnoreCase(obj, "handbookParentId");
    }

    private static bool TryGetDynamicItemId(JsonElement obj, out string templateId)
    {
        templateId = string.Empty;

        var hasCloneHint = HasPropertyIgnoreCase(obj, "itemTplToClone")
            || HasPropertyIgnoreCase(obj, "parentId")
            || HasPropertyIgnoreCase(obj, "handbookParentId");

        if (!hasCloneHint)
        {
            return false;
        }

        foreach (var name in DynamicIdPropertyNames)
        {
            if (TryGetString(obj, name, out var id) && IsMongoId(id))
            {
                templateId = id;
                return true;
            }
        }

        return false;
    }

    private static bool TryGetString(JsonElement obj, string propertyName, out string value)
    {
        foreach (var property in obj.EnumerateObject())
        {
            if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase)
                && property.Value.ValueKind == JsonValueKind.String)
            {
                value = property.Value.GetString() ?? string.Empty;
                return !string.IsNullOrWhiteSpace(value);
            }
        }

        value = string.Empty;
        return false;
    }

    private static bool HasPropertyIgnoreCase(JsonElement obj, string propertyName)
    {
        return obj.EnumerateObject()
            .Any(property => string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase));
    }

    private static void Add(
        Dictionary<string, ItemTemplateMatch> matches,
        string templateId,
        string sourceFile,
        string confidence)
    {
        matches.TryAdd(templateId, new ItemTemplateMatch(templateId, sourceFile, confidence));
    }

    private static bool IsMongoId(string value)
    {
        return MongoIdRegex().IsMatch(value);
    }

    private static bool IsMetadataFile(string file)
    {
        var name = Path.GetFileName(file);
        return name.Equals("package.json", StringComparison.OrdinalIgnoreCase)
            || name.Equals("config.json", StringComparison.OrdinalIgnoreCase)
            || name.Equals("manifest.json", StringComparison.OrdinalIgnoreCase)
            || name.EndsWith(".deps.json", StringComparison.OrdinalIgnoreCase)
            || name.EndsWith(".runtimeconfig.json", StringComparison.OrdinalIgnoreCase);
    }

    private static readonly JsonDocumentOptions JsonOptions = new()
    {
        CommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    private static readonly string[] DynamicIdPropertyNames =
    [
        "newId",
        "newItemId",
        "templateId",
        "id"
    ];

    [GeneratedRegex("^[a-fA-F0-9]{24}$", RegexOptions.CultureInvariant)]
    private static partial Regex MongoIdRegex();
}

public record ItemTemplateMatch(string TemplateId, string SourceFile, string Confidence);
