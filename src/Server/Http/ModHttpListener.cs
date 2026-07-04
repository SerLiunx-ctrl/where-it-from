using Microsoft.AspNetCore.Http;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Servers.Http;
using WhereItFrom.Server.Services;

namespace WhereItFrom.Server.Http;

[Injectable(TypePriority = 0)]
public class ModHttpListener(
    ISptLogger<ModHttpListener> logger,
    ItemSourceMapService itemSourceMapService) : IHttpListener
{
    private static readonly PathString GetItemSourcesPath = new($"{Constants.RoutePrefix}{Constants.RouteGetItemSources}");

    public bool CanHandle(MongoId sessionId, HttpContext context)
    {
        return context.Request.Path.StartsWithSegments(Constants.RoutePrefix, StringComparison.OrdinalIgnoreCase);
    }

    public async Task Handle(MongoId sessionId, HttpContext context)
    {
        try
        {
            if (context.Request.Method == HttpMethods.Get
                && context.Request.Path.Equals(GetItemSourcesPath, StringComparison.OrdinalIgnoreCase))
            {
                await context.Response.WriteAsJsonAsync(itemSourceMapService.Get(), context.RequestAborted);
                return;
            }

            logger.Warning($"{Constants.LoggerPrefix}unknown route: {context.Request.Path}");
            context.Response.StatusCode = StatusCodes.Status404NotFound;
        }
        catch (Exception exception)
        {
            logger.Error($"{Constants.LoggerPrefix}route failed: {exception}");
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        }
    }
}
