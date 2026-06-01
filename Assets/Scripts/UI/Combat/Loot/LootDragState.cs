public enum DragSource {  Inventory, Ground }
public static class LootDragState
{
    public static bool IsDragging;
    public static LootItem Item;
    public static DragSource From;
    public static int InventoryIndex;      // from == Inventory 일때
    public static LootSlot GroundSlot;      // from == ground 일때

    public static void Clear()
    {
        IsDragging = false;
        GroundSlot = null;
    }

}
