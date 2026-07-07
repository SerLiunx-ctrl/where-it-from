using EFT.InventoryLogic;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Reflection;
using TMPro;
using WhereItFrom.Client.Data;
using WhereItFrom.Client.Services;

namespace WhereItFrom.Client.Patches;

public class ItemSpecificationPanelShowPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.FirstMethod(
            typeof(ItemSpecificationPanel),
            method => method.Name == nameof(ItemSpecificationPanel.Show));
    }

    [PatchPostfix]
    [HarmonyPriority(Priority.Last)]
    public static void PatchPostfix(ItemSpecificationPanel __instance)
    {
        try
        {
            if (!Plugin.Settings.Enabled.Value || !Plugin.Settings.ShowInItemDetails.Value)
            {
                return;
            }

            var item = CurrentItemField.GetValue(__instance) as Item;
            if (item is null)
            {
                return;
            }

            var source = ResolveSource(item);
            if (source is null && !Plugin.Settings.ShowUnknown.Value)
            {
                return;
            }

            var description = GetDescriptionLabel(__instance);
            if (description is null)
            {
                return;
            }

            var baseText = RemoveExistingSourceBlock(description.text);
            var sourceLine = SimpleTooltipShowPatch.BuildSourceLine(source);
            description.text = $"{baseText}{DetailMarkerStart}<br><br>{sourceLine}{DetailMarkerEnd}";
        }
        catch (Exception exception)
        {
            Plugin.Log.LogWarning($"WhereItFrom item details patch failed: {exception.Message}");
        }
    }

    private static ItemSourceEntry? ResolveSource(Item item)
    {
        var templateId = item.TemplateId.ToString();
        if (string.IsNullOrWhiteSpace(templateId))
        {
            return null;
        }

        return ItemSourceMapService.TryGetSource(templateId, out var source)
            ? source
            : null;
    }

    private static TMP_Text? GetDescriptionLabel(ItemSpecificationPanel panel)
    {
        var labels = ItemLabelsField.GetValue(panel);
        return labels is null
            ? null
            : DescriptionField.GetValue(labels) as TMP_Text;
    }

    private static string RemoveExistingSourceBlock(string text)
    {
        var markerIndex = text.IndexOf(DetailMarkerStart, StringComparison.Ordinal);
        return markerIndex < 0
            ? text
            : text.Substring(0, markerIndex);
    }

    private const string DetailMarkerStart = "<size=0>WhereItFromDetailStart</size>";
    private const string DetailMarkerEnd = "<size=0>WhereItFromDetailEnd</size>";

    private static readonly FieldInfo CurrentItemField = AccessTools.Field(typeof(ItemSpecificationPanel), "item_0");
    private static readonly FieldInfo ItemLabelsField = AccessTools.Field(typeof(ItemSpecificationPanel), "_itemLabels");
    private static readonly FieldInfo DescriptionField = AccessTools.Field(ItemLabelsField.FieldType, "_description");
}
