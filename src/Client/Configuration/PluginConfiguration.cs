using BepInEx.Configuration;
using UnityEngine;

namespace WhereItFrom.Client.Configuration;

public enum TooltipPlacement
{
    Bottom,
    Top
}

public class PluginConfiguration
{
    public PluginConfiguration(ConfigFile config)
    {
        Enabled = config.Bind(
            "General",
            "Enabled",
            true,
            "Append an item source line to hover tooltips.");

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
    public ConfigEntry<TooltipPlacement> Placement { get; }
    public ConfigEntry<bool> SeparatorEnabled { get; }
    public ConfigEntry<string> SeparatorText { get; }
    public ConfigEntry<Color> SeparatorColor { get; }
    public ConfigEntry<bool> ShowUnknown { get; }
    public ConfigEntry<string> UnknownText { get; }
    public ConfigEntry<bool> IncludeConfidence { get; }

    private static readonly Color DefaultPrefixColor = new(1f, 0.6667f, 1f, 1f);
    private static readonly Color DefaultModNameColor = new(0f, 0.5725f, 0.7608f, 1f);
    private static readonly Color DefaultSeparatorColor = new(0.4667f, 0.4667f, 0.4667f, 1f);
}
