using EFT.InventoryLogic;

namespace WhereItFrom.Client.State;

public static class HoveredItemState
{
    private static volatile Item? _currentItem;
    private static bool? _currentItemExamined;

    public static Item? CurrentItem => _currentItem;
    public static bool? CurrentItemExamined => _currentItemExamined;

    public static void Set(Item? item, bool? examined)
    {
        _currentItem = item;
        _currentItemExamined = examined;
    }

    public static void Clear()
    {
        _currentItem = null;
        _currentItemExamined = null;
    }
}
