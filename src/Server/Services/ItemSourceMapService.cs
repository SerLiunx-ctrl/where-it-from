using System.Reflection;
using System.Text.Json;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Servers;
using WhereItFrom.Server.Models;

namespace WhereItFrom.Server.Services;

[Injectable(InjectionType = InjectionType.Singleton)]
public class ItemSourceMapService(
    DatabaseServer databaseServer,
    ISptLogger<ItemSourceMapService> logger,
    ModHelper modHelper)
{
    private readonly object _sync = new();
    private ItemSourceMapResponse? _cachedResponse;

    public ItemSourceMapResponse Get()
    {
        lock (_sync)
        {
            return _cachedResponse ?? Refresh();
        }
    }

    public ItemSourceMapResponse Refresh()
    {
        lock (_sync)
        {
            _cachedResponse = BuildMap();
            return _cachedResponse;
        }
    }

    private ItemSourceMapResponse BuildMap()
    {
        var ownModPath = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
        var modsRoot = Directory.GetParent(ownModPath)?.FullName;

        if (modsRoot is null || !Directory.Exists(modsRoot))
        {
            logger.Warning($"{Constants.LoggerPrefix}could not locate user/mods root from {ownModPath}.");
            return new ItemSourceMapResponse();
        }

        var config = LoadOrCreateConfig(ownModPath);
        var finalTemplateIds = databaseServer
            .GetTables()
            .Templates
            .Items
            .Keys
            .Select(key => key.ToString())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var items = new Dictionary<string, ItemSourceEntry>(StringComparer.OrdinalIgnoreCase);
        var ownFullPath = Path.GetFullPath(ownModPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var scannedModCount = 0;

        foreach (var modDirectory in Directory.EnumerateDirectories(modsRoot))
        {
            var modFullPath = Path.GetFullPath(modDirectory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var modFolder = Path.GetFileName(modFullPath);

            if (string.Equals(modFullPath, ownFullPath, StringComparison.OrdinalIgnoreCase)
                || config.IgnoredModFolders.Contains(modFolder, StringComparer.OrdinalIgnoreCase))
            {
                continue;
            }

            scannedModCount++;
            var modName = ModMetadataReader.GetDisplayName(modFullPath);
            var ids = JsonItemTemplateScanner.Scan(modFullPath, config.IncludeDynamicIdHeuristics);

            foreach (var match in ids)
            {
                if (!finalTemplateIds.Contains(match.TemplateId))
                {
                    continue;
                }

                var contributor = new ItemSourceContributor
                {
                    ModName = modName,
                    ModFolder = modFolder,
                    SourceFile = Path.GetRelativePath(modFullPath, match.SourceFile),
                    Confidence = match.Confidence
                };

                if (items.TryGetValue(match.TemplateId, out var existing))
                {
                    AddModifiedBy(existing, contributor);
                    continue;
                }

                items[match.TemplateId] = new ItemSourceEntry
                {
                    TemplateId = match.TemplateId,
                    ModName = contributor.ModName,
                    ModFolder = contributor.ModFolder,
                    SourceFile = contributor.SourceFile,
                    Confidence = contributor.Confidence
                };
            }
        }

        foreach (var mapping in config.ManualMappings)
        {
            if (!finalTemplateIds.Contains(mapping.Key))
            {
                logger.Warning($"{Constants.LoggerPrefix}manual mapping skipped because template id is not in the loaded database: {mapping.Key}");
                continue;
            }

            items.TryGetValue(mapping.Key, out var existing);
            items[mapping.Key] = new ItemSourceEntry
            {
                TemplateId = mapping.Key,
                ModName = mapping.Value,
                ModFolder = "manual",
                SourceFile = "config.json",
                Confidence = "manual",
                ModifiedBy = existing?.ModifiedBy ?? []
            };
        }

        return new ItemSourceMapResponse
        {
            GeneratedAt = DateTimeOffset.UtcNow,
            ScannedModCount = scannedModCount,
            Items = items
        };
    }

    private static void AddModifiedBy(ItemSourceEntry entry, ItemSourceContributor contributor)
    {
        if (IsSameMod(entry.ModFolder, entry.ModName, contributor.ModFolder, contributor.ModName)
            || entry.ModifiedBy.Any(existing => IsSameMod(
                existing.ModFolder,
                existing.ModName,
                contributor.ModFolder,
                contributor.ModName)))
        {
            return;
        }

        entry.ModifiedBy.Add(contributor);
    }

    private static bool IsSameMod(string leftFolder, string leftName, string rightFolder, string rightName)
    {
        if (!string.IsNullOrWhiteSpace(leftFolder)
            && !string.IsNullOrWhiteSpace(rightFolder)
            && string.Equals(leftFolder, rightFolder, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return string.Equals(leftName, rightName, StringComparison.OrdinalIgnoreCase);
    }

    private ServerConfig LoadOrCreateConfig(string ownModPath)
    {
        var configPath = Path.Combine(ownModPath, "config.json");

        if (!File.Exists(configPath))
        {
            var defaultConfig = new ServerConfig();
            var defaultJson = JsonSerializer.Serialize(defaultConfig, JsonOptions);
            File.WriteAllText(configPath, defaultJson);

            return defaultConfig;
        }

        try
        {
            var json = File.ReadAllText(configPath);
            return JsonSerializer.Deserialize<ServerConfig>(json, JsonOptions) ?? new ServerConfig();
        }
        catch (Exception exception)
        {
            logger.Error($"{Constants.LoggerPrefix}failed to read config.json, using defaults: {exception.Message}");
            return new ServerConfig();
        }
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };
}
