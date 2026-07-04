using BepInEx;
using BepInEx.Logging;
using WhereItFrom.Client.Configuration;
using WhereItFrom.Client.Patches;
using WhereItFrom.Client.Services;
using WhereItFrom.Client.State;

namespace WhereItFrom.Client;

[BepInPlugin("com.serliunx.whereitfrom", "WhereItFrom", "1.0.0")]
[BepInDependency("com.SPT.custom", "4.0.0")]
[BepInProcess("EscapeFromTarkov.exe")]
public class Plugin : BaseUnityPlugin
{
    public static PluginConfiguration Settings { get; private set; } = null!;
    public static ManualLogSource Log { get; private set; } = null!;

    private void Awake()
    {
        Log = Logger;
        Settings = new PluginConfiguration(Config);

        HoveredItemState.Clear();
        ItemSourceMapService.Initialize(Logger);
        ItemSourceMapService.RefreshInBackground();

        new GridItemOnPointerEnterPatch().Enable();
        new GridItemOnPointerExitPatch().Enable();
        new InventoryScreenClosePatch().Enable();
        new SimpleTooltipShowPatch().Enable();

        Logger.LogInfo("WhereItFrom loaded.");
    }
}
