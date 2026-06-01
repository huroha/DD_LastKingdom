using UnityEngine;
using UnityEngine.EventSystems;
public class CombatInventoryPanel : MonoBehaviour, IDropHandler
{
    [SerializeField] private InventorySlotView m_SlotPrefab;
    [SerializeField] private Transform m_SlotRoot;

    private InventorySlotView[] m_Views;

    private void Awake()
    {
        InventoryConfig cfg = DataManager.Instance.InventoryConfig;
        m_Views = new InventorySlotView[cfg.SlotCount];
        for(int i = 0; i < cfg.SlotCount; ++i)
        {
            m_Views[i] = Instantiate(m_SlotPrefab, m_SlotRoot);
        }
    }

    private void OnEnable()
    {
        ExpeditionInventory inv = ExpeditionManager.Instance.Inventory;
        if (inv == null) return;
        inv.OnChanged += Refresh;
        Refresh();
    }
    private void OnDisable()
    {
        ExpeditionInventory inv = ExpeditionManager.Instance.Inventory;
        if (inv == null) return;
        inv.OnChanged -= Refresh;
    }
    private void Refresh()
    {
        ExpeditionInventory inv = ExpeditionManager.Instance.Inventory;
        InventoryConfig cfg = DataManager.Instance.InventoryConfig;
        for (int i = 0; i < m_Views.Length; ++i)
        {
            LootItem item = inv.Slots[i];
            Sprite icon = item.Quantity > 0 ? cfg.Icon(item.Type, item.Relic) : null;
            m_Views[i].Setup(i, item, icon);
        }
    }
    public void OnDrop(PointerEventData eventData)
    {
        if (!LootDragState.IsDragging) return;
        if (LootDragState.From != DragSource.Ground) return;

        bool ok = ExpeditionManager.Instance.Inventory.TryAdd(LootDragState.Item);
        if (ok) LootDragState.GroundSlot.ConsumeWithoutEvent();
        LootDragState.IsDragging = false;
    }
}
