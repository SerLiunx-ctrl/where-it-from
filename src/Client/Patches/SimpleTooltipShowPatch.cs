using EFT.InventoryLogic;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using WhereItFrom.Client.Configuration;
using WhereItFrom.Client.Data;
using WhereItFrom.Client.Services;
using WhereItFrom.Client.State;

namespace WhereItFrom.Client.Patches;

public class SimpleTooltipShowPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.FirstMethod(
            typeof(SimpleTooltip),
            method => method.Name == nameof(SimpleTooltip.Show)
                && method.GetParameters().Length > 0
                && method.GetParameters()[0].Name == "text");
    }

    [PatchPrefix]
    [HarmonyPriority(Priority.Last)]
    public static void PatchPrefix(ref string text, Vector2? offset, ref float delay, ref float? maxWidth)
    {
        try
        {
            if (!Plugin.Settings.Enabled.Value)
            {
                return;
            }

            var item = HoveredItemState.CurrentItem;
            if (item is null)
            {
                return;
            }

            var itemExamined = HoveredItemState.CurrentItemExamined;
            if (itemExamined == false || (itemExamined is null && !IsItemExamined(item)))
            {
                return;
            }

            var templateId = item.TemplateId.ToString();
            if (string.IsNullOrWhiteSpace(templateId))
            {
                return;
            }

            if (ItemSourceMapService.TryGetSource(templateId, out var source) && source is not null)
            {
                text = InsertSourceBlock(text, source);
                EnsureTooltipWidth(ref maxWidth);
                return;
            }

            if (Plugin.Settings.ShowUnknown.Value)
            {
                text = InsertSourceBlock(text, null);
                EnsureTooltipWidth(ref maxWidth);
            }
        }
        catch (Exception exception)
        {
            Plugin.Log.LogWarning($"WhereItFrom tooltip patch failed: {exception.Message}");
        }
    }

    private static string InsertSourceBlock(string text, ItemSourceEntry? source)
    {
        var sourceBlock = BuildSourceBlock(source);

        return Plugin.Settings.Placement.Value switch
        {
            TooltipPlacement.Top => $"{sourceBlock}<br>{text}",
            _ => $"{text}<br>{sourceBlock}"
        };
    }

    private static string BuildSourceBlock(ItemSourceEntry? source)
    {
        var line = BuildSourceLine(source);

        if (!Plugin.Settings.SeparatorEnabled.Value)
        {
            return line;
        }

        var separator = SanitizeRichText(Plugin.Settings.SeparatorText.Value);
        var separatorLine = string.IsNullOrWhiteSpace(separator)
            ? string.Empty
            : $"<color={ToHtmlColor(Plugin.Settings.SeparatorColor.Value)}>{separator}</color>";

        return Plugin.Settings.Placement.Value == TooltipPlacement.Top
            ? $"{line}<br>{separatorLine}"
            : $"{separatorLine}<br>{line}";
    }

    internal static string BuildSourceLine(ItemSourceEntry? source)
    {
        var label = SanitizeRichText(Plugin.Settings.Label.Value);
        var rawModName = source?.ModName ?? Plugin.Settings.UnknownText.Value;
        var modName = SanitizeRichText(TruncateText(rawModName, Plugin.Settings.ModNameMaxLength.Value));
        var styledLabel = ApplyStyle(
            label,
            Plugin.Settings.PrefixColor.Value,
            Plugin.Settings.PrefixBold.Value,
            Plugin.Settings.PrefixItalic.Value,
            Plugin.Settings.PrefixUnderline.Value);
        var styledModName = ApplyStyle(
            modName,
            Plugin.Settings.ModNameColor.Value,
            Plugin.Settings.ModNameBold.Value,
            Plugin.Settings.ModNameItalic.Value,
            Plugin.Settings.ModNameUnderline.Value);
        var confidence = Plugin.Settings.IncludeConfidence.Value && source is not null
            ? $" <size=75%>({SanitizeRichText(source.Confidence)})</size>"
            : string.Empty;
        var prefixGap = string.IsNullOrEmpty(label) ? string.Empty : " ";

        var line = $"{styledLabel}{prefixGap}{styledModName}{confidence}";
        return Plugin.Settings.PreventSourceLineWrapping.Value
            ? $"<nobr>{line}</nobr>"
            : line;
    }

    private static void EnsureTooltipWidth(ref float? maxWidth)
    {
        var tooltipMaxWidth = Plugin.Settings.TooltipMaxWidth.Value;
        if (tooltipMaxWidth <= 0)
        {
            return;
        }

        if (maxWidth is null || maxWidth.Value < tooltipMaxWidth)
        {
            maxWidth = tooltipMaxWidth;
        }
    }

    private static string TruncateText(string text, int maxLength)
    {
        if (maxLength <= 0 || text.Length <= maxLength)
        {
            return text;
        }

        if (maxLength <= Ellipsis.Length)
        {
            return Ellipsis.Substring(0, maxLength);
        }

        return text.Substring(0, maxLength - Ellipsis.Length) + Ellipsis;
    }

    private static bool IsItemExamined(Item item)
    {
        try
        {
            var templateId = item.TemplateId.ToString();

            if (TryCallExamined(item.Owner, item, templateId, 0, new HashSet<int>(), out var examined))
            {
                return examined;
            }

            if (TryReadExaminedState(item.Owner, templateId, 0, new HashSet<int>(), out examined))
            {
                return examined;
            }

            if (TryGetBooleanProperty(item.Template, "ExaminedByDefault", out var examinedByDefault))
            {
                return examinedByDefault;
            }
        }
        catch (Exception exception)
        {
            Plugin.Log.LogDebug($"WhereItFrom could not determine examined state: {exception.Message}");
        }

        return false;
    }

    private static bool TryCallExamined(
        object? candidate,
        Item item,
        string templateId,
        int depth,
        HashSet<int> visited,
        out bool examined)
    {
        examined = false;

        if (candidate is null || depth > 4)
        {
            return false;
        }

        var candidateId = RuntimeHelpers.GetHashCode(candidate);
        if (!visited.Add(candidateId))
        {
            return false;
        }

        var candidateType = candidate.GetType();
        foreach (var method in candidateType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (!ExaminedMethodNames.Contains(method.Name)
                || method.ReturnType != typeof(bool))
            {
                continue;
            }

            var parameters = method.GetParameters();
            if (parameters.Length == 1 && parameters[0].ParameterType.IsInstanceOfType(item))
            {
                examined = (bool)method.Invoke(candidate, [item])!;
                return true;
            }

            if (parameters.Length == 1 && parameters[0].ParameterType == typeof(string))
            {
                examined = (bool)method.Invoke(candidate, [templateId])!;
                return true;
            }
        }

        foreach (var propertyName in ProfilePropertyNames)
        {
            var property = candidateType.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property is null || property.GetIndexParameters().Length > 0)
            {
                continue;
            }

            var nestedCandidate = property.GetValue(candidate);
            if (TryCallExamined(nestedCandidate, item, templateId, depth + 1, visited, out examined))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryReadExaminedState(
        object? candidate,
        string templateId,
        int depth,
        HashSet<int> visited,
        out bool examined)
    {
        examined = false;

        if (candidate is null || depth > 4)
        {
            return false;
        }

        var candidateId = RuntimeHelpers.GetHashCode(candidate);
        if (!visited.Add(candidateId))
        {
            return false;
        }

        if (TryReadTemplateIdCollection(candidate, templateId, out examined))
        {
            return true;
        }

        var candidateType = candidate.GetType();
        foreach (var member in candidateType.GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (!ShouldInspectExaminedMember(member.Name))
            {
                continue;
            }

            var value = GetMemberValue(candidate, member);
            if (value is null)
            {
                continue;
            }

            if (TryReadTemplateIdCollection(value, templateId, out examined))
            {
                return true;
            }
        }

        foreach (var propertyName in ProfilePropertyNames)
        {
            var property = candidateType.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property is null || property.GetIndexParameters().Length > 0)
            {
                continue;
            }

            var nestedCandidate = property.GetValue(candidate);
            if (TryReadExaminedState(nestedCandidate, templateId, depth + 1, visited, out examined))
            {
                return true;
            }
        }

        return false;
    }

    private static object? GetMemberValue(object candidate, MemberInfo member)
    {
        try
        {
            return member switch
            {
                PropertyInfo property when property.GetIndexParameters().Length == 0 => property.GetValue(candidate),
                FieldInfo field => field.GetValue(candidate),
                _ => null
            };
        }
        catch
        {
            return null;
        }
    }

    private static bool ShouldInspectExaminedMember(string memberName)
    {
        return ExaminedStateMemberNames.Any(name =>
            memberName.Contains(name, StringComparison.OrdinalIgnoreCase));
    }

    private static bool TryReadTemplateIdCollection(object candidate, string templateId, out bool examined)
    {
        examined = false;

        if (candidate is IDictionary dictionary)
        {
            foreach (DictionaryEntry entry in dictionary)
            {
                if (!IsTemplateId(entry.Key, templateId))
                {
                    continue;
                }

                examined = entry.Value is not bool known || known;
                return true;
            }
        }

        if (candidate is IEnumerable enumerable && candidate is not string)
        {
            foreach (var entry in enumerable)
            {
                if (entry is null)
                {
                    continue;
                }

                if (IsTemplateId(entry, templateId))
                {
                    examined = true;
                    return true;
                }
            }
        }

        return false;
    }

    private static bool IsTemplateId(object value, string templateId)
    {
        return string.Equals(value.ToString(), templateId, StringComparison.Ordinal);
    }

    private static bool TryGetBooleanProperty(object? candidate, string propertyName, out bool value)
    {
        value = false;

        if (candidate is null)
        {
            return false;
        }

        var property = candidate.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (property?.PropertyType != typeof(bool) || property.GetIndexParameters().Length > 0)
        {
            return false;
        }

        value = (bool)property.GetValue(candidate)!;
        return true;
    }

    private static string ApplyStyle(
        string text,
        Color color,
        bool bold,
        bool italic,
        bool underline)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        var result = $"<color={ToHtmlColor(color)}>{text}</color>";

        if (underline)
        {
            result = $"<u>{result}</u>";
        }

        if (italic)
        {
            result = $"<i>{result}</i>";
        }

        if (bold)
        {
            result = $"<b>{result}</b>";
        }

        return result;
    }

    private static string ToHtmlColor(Color color)
    {
        return $"#{ColorUtility.ToHtmlStringRGBA(color)}";
    }

    private static string SanitizeRichText(string value)
    {
        return value
            .Replace("<", "(")
            .Replace(">", ")")
            .Replace("\r", " ")
            .Replace("\n", " ");
    }

    private static readonly string[] ProfilePropertyNames =
    [
        "Profile",
        "Owner",
        "Parent",
        "RootOwner",
        "Session"
    ];

    private static readonly string[] ExaminedMethodNames =
    [
        "Examined",
        "IsExamined",
        "IsItemExamined",
        "IsKnown",
        "IsItemKnown"
    ];

    private static readonly string[] ExaminedStateMemberNames =
    [
        "Encyclopedia",
        "Known",
        "Examined"
    ];

    private const string Ellipsis = "...";
}
