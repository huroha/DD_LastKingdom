using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class PartySlotView : MonoBehaviour, IDropHandler, IPointerClickHandler
{
    [SerializeField] private TextMeshProUGUI m_SlotIndexText;

    private int m_SlotIndex;
    private NikkeCardView m_AssignedCard;

    public delegate void SlotChangedHandler(int slotIndex, NikkeInstance instance, NikkeCardView card);
    public event SlotChangedHandler OnSlotChanged;

    public int SlotIndex => m_SlotIndex;
    public NikkeInstance AssignedInstance => m_AssignedCard?.BoundInstance;
    public bool IsEmpty => m_AssignedCard == null;

    public void Init(int slotIndex)
    {
        m_SlotIndex = slotIndex;
        m_SlotIndexText.text = slotIndex.ToString();
    }
    public void AssignCard(NikkeCardView card)
    {
        if (m_AssignedCard != null)
            ClearSlot();

        card.transform.SetParent(transform);
        card.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        m_AssignedCard = card;
        OnSlotChanged?.Invoke(m_SlotIndex, card.BoundInstance, card);

    }
    public NikkeCardView ClearSlot()
    {
        if (m_AssignedCard == null) return null;

        NikkeCardView card = m_AssignedCard;
        m_AssignedCard = null;
        OnSlotChanged?.Invoke(m_SlotIndex, null, card);
        return card;
    }
    public void OnDrop(PointerEventData e)
    {
        if (e.pointerDrag == null) return;
        NikkeCardView card = e.pointerDrag.GetComponent<NikkeCardView>();
        if (card == null) return;

        AssignCard(card);
    }
    public void OnPointerClick(PointerEventData e)
    {
        if (m_AssignedCard == null) return;

        NikkeCardView card = ClearSlot();
        // 추후 다시
    }
}
