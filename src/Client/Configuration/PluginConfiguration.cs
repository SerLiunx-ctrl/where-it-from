using BepInEx.Configuration;
using UnityEngine;
using WhereItFrom.Client.Data;

namespace WhereItFrom.Client.Configuration;

public enum TooltipPlacement
{
    Bottom,
    Top
}

public class PluginConfiguration
{
    private readonly ConfigFile _config;
    private readonly object _sourceModSync = new();
    private readonly Dictionary<string, SourceModConfiguration> _sourceMods = new(StringComparer.OrdinalIgnoreCase);

    public PluginConfiguration(ConfigFile config)
    {
        _config = config;

        Enabled = config.Bind(
            "General",
            "Enabled",
            true,
            "Append an item source line to hover tooltips.");

        ShowInItemDetails = config.Bind(
            "General",
            "ShowInItemDetails",
            true,
            "Append an item source line to the item details page.");

        ShowModifiedBy = config.Bind(
            "General",
            "ShowModifiedBy",
            true,
            "Show other mods that also define the same item template after the primary source.");

        Label = config.Bind(
            "General",
            "Label",
            string.Empty,
            "Text shown before the source mod name. Set it to empty to show only the mod name.");

        PrefixColor = config.Bind(
            "Prefix Style",
            "PrefixColorRGBA",
            DefaultPrefixColor,
            "Prefix color. ConfigurationManager shows RGBA sliders for this value.");

        PrefixBold = config.Bind(
            "Prefix Style",
            "PrefixBold",
            true,
            "Render the prefix in bold.");

        PrefixItalic = config.Bind(
            "Prefix Style",
            "PrefixItalic",
            false,
            "Render the prefix in italics.");

        PrefixUnderline = config.Bind(
            "Prefix Style",
            "PrefixUnderline",
            false,
            "Underline the prefix.");

        ModNameColor = config.Bind(
            "Mod Name Style",
            "ModNameColorRGBA",
            DefaultModNameColor,
            "Mod name color. ConfigurationManager shows RGBA sliders for this value.");

        ModNameBold = config.Bind(
            "Mod Name Style",
            "ModNameBold",
            true,
            "Render the mod name in bold.");

        ModNameItalic = config.Bind(
            "Mod Name Style",
            "ModNameItalic",
            false,
            "Render the mod name in italics.");

        ModNameUnderline = config.Bind(
            "Mod Name Style",
            "ModNameUnderline",
            false,
            "Underline the mod name.");

        ModNameMaxLength = config.Bind(
            "Mod Name Style",
            "ModNameMaxLength",
            24,
            new ConfigDescription(
                "Maximum displayed mod name length. Set to 0 to disable truncation.",
                new AcceptableValueRange<int>(0, 80)));

        ModifiedByLabel = config.Bind(
            "Mod Name Style",
            "ModifiedByLabel",
            "Modified by:",
            "Text shown before the list of mods that also define this item.");

        ModifiedByMaxCount = config.Bind(
            "Mod Name Style",
            "ModifiedByMaxCount",
            3,
            new ConfigDescription(
                "Maximum number of modified-by mods displayed. Set to 0 to show all.",
                new AcceptableValueRange<int>(0, 20)));

        PreventSourceLineWrapping = config.Bind(
            "Layout",
            "PreventSourceLineWrapping",
            true,
            "Prevent the source line from wrapping inside the tooltip.");

        TooltipMaxWidth = config.Bind(
            "Layout",
            "TooltipMaxWidth",
            420,
            new ConfigDescription(
                "Tooltip max width requested when the source line is shown. Set to 0 to keep the game's original width.",
                new AcceptableValueRange<int>(0, 1000)));

        Placement = config.Bind(
            "Layout",
            "Placement",
            TooltipPlacement.Bottom,
            "Where to place the source block in the tooltip.");

        SeparatorEnabled = config.Bind(
            "Layout",
            "SeparatorEnabled",
            true,
            "Add a separator line between the existing tooltip text and the source line.");

        SeparatorText = config.Bind(
            "Layout",
            "SeparatorText",
            string.Empty,
            "Text used as the separator line. Set it to empty or spaces to create a blank line.");

        SeparatorColor = config.Bind(
            "Layout",
            "SeparatorColorRGBA",
            DefaultSeparatorColor,
            "Separator color. Ignored visually when SeparatorText is empty or spaces.");

        ShowUnknown = config.Bind(
            "General",
            "ShowUnknown",
            true,
            "Show a tooltip line when the item source is not known.");

        UnknownText = config.Bind(
            "General",
            "UnknownText",
            "EscapeFromTarkov",
            "Text used when ShowUnknown is enabled and no source mod was found.");

        IncludeConfidence = config.Bind(
            "Debug",
            "IncludeConfidence",
            false,
            "Append the mapping confidence/source type for troubleshooting.");

        MigrateLegacyDefaults();
    }

    public int RegisterSourceMods(IEnumerable<ItemSourceEntry> entries)
    {
        var addedCount = 0;

        lock (_sourceModSync)
        {
            foreach (var mod in CollectSourceMods(entries))
            {
                var key = GetModKey(mod.ModFolder, mod.ModName);
                if (_sourceMods.ContainsKey(key))
                {
                    continue;
                }

                _sourceMods[key] = BindSourceModConfiguration(mod);
                addedCount++;
            }
        }

        if (addedCount > 0)
        {
            _config.Save();
        }

        return addedCount;
    }

    public bool IsModHidden(string? modFolder, string? modName)
    {
        lock (_sourceModSync)
        {
            return TryGetSourceModConfiguration(modFolder, modName, out var mod)
                && mod.Hidden.Value;
        }
    }

    public string GetModDisplayName(string? modFolder, string? modName)
    {
        var fallback = string.IsNullOrWhiteSpace(modName)
            ? modFolder ?? string.Empty
            : modName;

        lock (_sourceModSync)
        {
            if (!TryGetSourceModConfiguration(modFolder, modName, out var mod))
            {
                return fallback;
            }

            var alias = mod.Alias.Value?.Trim() ?? string.Empty;
            return string.IsNullOrWhiteSpace(alias)
                ? fallback
                : alias;
        }
    }

    private void MigrateLegacyDefaults()
    {
        if (string.Equals(Label.Value, "\u93c9\u30e6\u7c2e", StringComparison.Ordinal)
            || string.Equals(Label.Value, "\u6765\u6e90", StringComparison.Ordinal))
        {
            Label.Value = string.Empty;
        }

        if (string.Equals(UnknownText.Value, "\u9358\u71ba\u5897\u93b4\u6828\u6e6d\u7487\u55d7\u57c6", StringComparison.Ordinal)
            || string.Equals(UnknownText.Value, "\u539f\u7248\u6216\u672a\u8bc6\u522b", StringComparison.Ordinal))
        {
            UnknownText.Value = "EscapeFromTarkov";
        }
    }

    public ConfigEntry<bool> Enabled { get; }
    public ConfigEntry<bool> ShowInItemDetails { get; }
    public ConfigEntry<bool> ShowModifiedBy { get; }
    public ConfigEntry<string> Label { get; }
    public ConfigEntry<Color> PrefixColor { get; }
    public ConfigEntry<bool> PrefixBold { get; }
    public ConfigEntry<bool> PrefixItalic { get; }
    public ConfigEntry<bool> PrefixUnderline { get; }
    public ConfigEntry<Color> ModNameColor { get; }
    public ConfigEntry<bool> ModNameBold { get; }
    public ConfigEntry<bool> ModNameItalic { get; }
    public ConfigEntry<bool> ModNameUnderline { get; }
    public ConfigEntry<int> ModNameMaxLength { get; }
    public ConfigEntry<string> ModifiedByLabel { get; }
    public ConfigEntry<int> ModifiedByMaxCount { get; }
    public ConfigEntry<bool> PreventSourceLineWrapping { get; }
    public ConfigEntry<int> TooltipMaxWidth { get; }
    public ConfigEntry<TooltipPlacement> Placement { get; }
    public ConfigEntry<bool> SeparatorEnabled { get; }
    public ConfigEntry<string> SeparatorText { get; }
    public ConfigEntry<Color> SeparatorColor { get; }
    public ConfigEntry<bool> ShowUnknown { get; }
    public ConfigEntry<string> UnknownText { get; }
    public ConfigEntry<bool> IncludeConfidence { get; }

    private SourceModConfiguration BindSourceModConfiguration(SourceModDescriptor mod)
    {
        var section = $"Mods - {SanitizeConfigName(mod.ModName)}";
        var folder = SanitizeConfigName(mod.ModFolder);

        if (!string.IsNullOrWhiteSpace(folder)
            && !string.Equals(folder, section.Replace("Mods - ", string.Empty), StringComparison.OrdinalIgnoreCase))
        {
            section = $"{section} [{folder}]";
        }

        var hidden = _config.Bind(
            section,
            "Hidden",
            false,
            $"Hide source lines for this mod. Folder: {mod.ModFolder}");

        var alias = _config.Bind(
            section,
            "Alias",
            string.Empty,
            $"Optional display name override. Leave empty to show: {mod.ModName}");

        return new SourceModConfiguration(mod.ModFolder, mod.ModName, hidden, alias);
    }

    private bool TryGetSourceModConfiguration(
        string? modFolder,
        string? modName,
        out SourceModConfiguration mod)
    {
        var primaryKey = GetModKey(modFolder, modName);
        if (_sourceMods.TryGetValue(primaryKey, out mod))
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(modName)
            && _sourceMods.TryGetValue(GetModKey(null, modName), out mod))
        {
            return true;
        }

        mod = null!;
        return false;
    }

    private static IEnumerable<SourceModDescriptor> CollectSourceMods(IEnumerable<ItemSourceEntry> entries)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in entries)
        {
            if (TryAdd(entry.ModFolder, entry.ModName, seen, out var mod))
            {
                yield return mod;
            }

            foreach (var contributor in entry.ModifiedBy ?? new List<ItemSourceContributor>())
            {
                if (TryAdd(contributor.ModFolder, contributor.ModName, seen, out mod))
                {
                    yield return mod;
                }
            }
        }
    }

    private static bool TryAdd(
        string? modFolder,
        string? modName,
        HashSet<string> seen,
        out SourceModDescriptor mod)
    {
        mod = default;

        if (string.IsNullOrWhiteSpace(modFolder) && string.IsNullOrWhiteSpace(modName))
        {
            return false;
        }

        var key = GetModKey(modFolder, modName);
        if (!seen.Add(key))
        {
            return false;
        }

        mod = new SourceModDescriptor(
            modFolder?.Trim() ?? string.Empty,
            modName?.Trim() ?? modFolder?.Trim() ?? string.Empty);
        return true;
    }

    private static string GetModKey(string? modFolder, string? modName)
    {
        return string.IsNullOrWhiteSpace(modFolder)
            ? $"name:{modName?.Trim() ?? string.Empty}"
            : $"folder:{modFolder.Trim()}";
    }

    private static string SanitizeConfigName(string value)
    {
        var sanitized = value
            .Replace("\r", " ")
            .Replace("\n", " ")
            .Replace("[", "(")
            .Replace("]", ")")
            .Trim();

        if (string.IsNullOrWhiteSpace(sanitized))
        {
            return "Unknown";
        }

        return sanitized.Length <= 80
            ? sanitized
            : sanitized.Substring(0, 80);
    }

    private static readonly Color DefaultPrefixColor = new(1f, 0.6667f, 1f, 1f);
    private static readonly Color DefaultModNameColor = new(0f, 0.5725f, 0.7608f, 1f);
    private static readonly Color DefaultSeparatorColor = new(0.4667f, 0.4667f, 0.4667f, 1f);
}

internal readonly struct SourceModDescriptor
{
    public SourceModDescriptor(string modFolder, string modName)
    {
        ModFolder = modFolder;
        ModName = modName;
    }

    public string ModFolder { get; }
    public string ModName { get; }
}

internal sealed class SourceModConfiguration
{
    public SourceModConfiguration(
        string modFolder,
        string modName,
        ConfigEntry<bool> hidden,
        ConfigEntry<string> alias)
    {
        ModFolder = modFolder;
        ModName = modName;
        Hidden = hidden;
        Alias = alias;
    }

    public string ModFolder { get; }
    public string ModName { get; }
    public ConfigEntry<bool> Hidden { get; }
    public ConfigEntry<string> Alias { get; }
}
