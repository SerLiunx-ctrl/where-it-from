using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Reflection;
using WhereItFrom.Client.State;

namespace WhereItFrom.Client.Patches;

public class InventoryScreenClosePatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.FirstMethod(typeof(InventoryScreen), method => method.Name == nameof(InventoryScreen.Close));
    }

    [PatchPrefix]
    public static void PatchPrefix()
    {
        HoveredItemState.Clear();
    }
}
