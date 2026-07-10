using BepInEx.Logging;
using Newtonsoft.Json;
using SPT.Common.Http;
using WhereItFrom.Client.Data;

namespace WhereItFrom.Client.Services;

public static class ItemSourceMapService
{
    private static readonly object Sync = new();
    private static ManualLogSource? _logger;
    private static Dictionary<string, ItemSourceEntry> _items = new(StringComparer.OrdinalIgnoreCase);
    private static bool _refreshStarted;
    private static bool _loaded;

    public static void Initialize(ManualLogSource logger)
    {
        _logger = logger;
    }

    public static void RefreshInBackground()
    {
        lock (Sync)
        {
            if (_refreshStarted || _loaded)
            {
                return;
            }

            _refreshStarted = true;
        }

        _ = RefreshAsync();
    }

    public static bool TryGetSource(string templateId, out ItemSourceEntry? entry)
    {
        RefreshInBackground();
        return _items.TryGetValue(templateId, out entry);
    }

    private static async Task RefreshAsync()
    {
        try
        {
            var json = await RequestHandler.GetJsonAsync(Constants.RouteGetItemSources);
            var response = JsonConvert.DeserializeObject<ItemSourceMapResponse>(json);

            if (response?.Items is null)
            {
                return;
            }

            lock (Sync)
            {
                _items = new Dictionary<string, ItemSourceEntry>(response.Items, StringComparer.OrdinalIgnoreCase);
                _loaded = true;
            }

            var registeredModCount = Plugin.Settings.RegisterSourceMods(response.Items.Values);
            _logger?.LogInfo($"WhereItFrom loaded {response.Items.Count} item source mappings and registered {registeredModCount} mod configuration entries.");
        }
        catch (Exception exception)
        {
            _logger?.LogWarning($"WhereItFrom could not load item source mappings. Is the server mod installed? {exception.Message}");
        }
        finally
        {
            lock (Sync)
            {
                _refreshStarted = false;
            }
        }
    }
}
