using EFT.UI.DragAndDrop;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Reflection;
using UnityEngine.EventSystems;
using WhereItFrom.Client.State;

namespace WhereItFrom.Client.Patches;

public class GridItemOnPointerEnterPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.FirstMethod(typeof(GridItemView), method => method.Name == nameof(GridItemView.OnPointerEnter));
    }

    [PatchPrefix]
    public static void PatchPrefix(GridItemView __instance, PointerEventData eventData)
    {
        HoveredItemState.Set(__instance?.Item, __instance?.Examined);
    }
}
