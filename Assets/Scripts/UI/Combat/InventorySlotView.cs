using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;


public class InventorySlotView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    [SerializeField] private Image m_Icon;
    [SerializeField] private TextMeshProUGUI m_QuantityText;
    [SerializeField] private GameObject m_EmptyOverlay; // 빈 슬롯 표시용

    private LootItem m_Item;
    private int m_SlotIndex;

    public LootItem Item => m_Item;
    public int SlotIndex => m_SlotIndex;

    public void Setup(int slotIndex, LootItem item, Sprite icon)
    {
        m_SlotIndex = slotIndex;
        m_Item = item;
        bool hasItem = item.Quantity > 0;
        m_Icon.sprite = icon;
        m_Icon.enabled = hasItem;
        m_QuantityText.enabled = hasItem;
        m_QuantityText.SetText(hasItem && item.Quantity > 1 ? "{0}" : "", item.Quantity);
        m_EmptyOverlay.SetActive(!hasItem);
    }
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (m_Item.Quantity == 0)
        {
            eventData.pointerDrag = null;
            return;
        }
        LootDragState.IsDragging = true;
        LootDragState.Item = m_Item;
        LootDragState.From = DragSource.Inventory;
        LootDragState.InventoryIndex = m_SlotIndex;
        LootDragHelper.BeginVisual(m_Icon, m_QuantityText, eventData.position);
    }

    public void OnDrag(PointerEventData eventData)
    {
        LootDragHelper.Move(eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        LootDragHelper.EndVisual(m_Icon, m_QuantityText);
    }
    public void OnDrop(PointerEventData eventData)
    {
        if (!LootDragState.IsDragging) return;

        ExpeditionInventory inv = ExpeditionManager.Instance.Inventory;
        InventoryConfig cfg = DataManager.Instance.InventoryConfig;

        if (LootDragState.From == DragSource.Ground)
        {
            LootSlot groundSlot = LootDragState.GroundSlot;
            LootItem dragItem = LootDragState.Item;

            if (m_Item.Quantity == 0)
            {
                // 빈 슬롯 → 바로 배치
                inv.PlaceAt(m_SlotIndex, dragItem);
                groundSlot.ConsumeWithoutEvent();
            }
            else
            {
                // 점유 슬롯 → swap
                LootItem displaced = inv.TakeAt(m_SlotIndex);
                inv.PlaceAt(m_SlotIndex, dragItem);
                groundSlot.Setup(displaced, cfg.Icon(displaced.Type, displaced.Relic));
            }
        }
        else  // DragSource.Inventory → 가방 내 재배치
        {
            int fromIndex = LootDragState.InventoryIndex;
            if (fromIndex == m_SlotIndex) { LootDragState.IsDragging = false; return; }

            inv.Swap(fromIndex, m_SlotIndex);
        }

        LootDragState.IsDragging = false;
    }
}
