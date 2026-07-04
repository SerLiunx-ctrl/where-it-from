using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Utils;
using WhereItFrom.Server.Services;

namespace WhereItFrom.Server;

[Injectable(TypePriority = OnLoadOrder.PostSptModLoader + 1000)]
public class WhereItFromMod(
    ISptLogger<WhereItFromMod> logger,
    ItemSourceMapService itemSourceMapService) : IOnLoad
{
    public Task OnLoad()
    {
        var response = itemSourceMapService.Refresh();
        logger.Success($"{Constants.LoggerPrefix}mapped {response.Items.Count} item templates from {response.ScannedModCount} mods.");

        return Task.CompletedTask;
    }
}
