using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;


public class PartySlotView : MonoBehaviour, IDropHandler, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler
{
    private int m_SlotIndex;
    [SerializeField] private Image m_PortraitImage;   // 슬롯에 표시할 초상화
    [SerializeField] private GameObject m_EmptyVisual; // 비어있을 때 표시
    [SerializeField] private GameObject m_DropIndicator;

    private bool m_WasDragging;

    private NikkeCardView m_AssignedCard;  // SetEmbarked 호출용으로만 보관

    public delegate void SlotChangedHandler();
    public event SlotChangedHandler OnSlotChanged;

    public int SlotIndex => m_SlotIndex;
    public NikkeInstance AssignedInstance => m_AssignedCard?.BoundInstance;
    public bool IsEmpty => m_AssignedCard == null;
    public NikkeCardView AssignedCard => m_AssignedCard;


    public void Init(int slotIndex)
    {
        m_SlotIndex = slotIndex;
    }
    private void OnEnable()
    {
        NikkeDragEvents.OnDragStarted += OnAnyDragStarted;
        NikkeDragEvents.OnDragEnded += OnAnyDragEnded;
    }

    private void OnDisable()
    {
        NikkeDragEvents.OnDragStarted -= OnAnyDragStarted;
        NikkeDragEvents.OnDragEnded -= OnAnyDragEnded;
    }
    public void AssignCard(NikkeCardView card)
    {
        if (card.CurrentSlot != null && card.CurrentSlot != this)
            card.CurrentSlot.ClearSlot();
        if (m_AssignedCard != null)
            ClearSlot();
        m_AssignedCard = card;
        m_PortraitImage.sprite = card.BoundInstance.Data.PortraitSprite;
        m_PortraitImage.gameObject.SetActive(true);
        m_EmptyVisual.SetActive(false);
        card.SetEmbarked(true);
        card.SetCurrentSlot(this);
        OnSlotChanged?.Invoke();
    }
    public NikkeCardView ClearSlot()
    {
        if (m_AssignedCard == null) return null;

        NikkeCardView card = m_AssignedCard;
        m_AssignedCard = null;
        m_PortraitImage.gameObject.SetActive(false);
        m_EmptyVisual.SetActive(true);
        card.SetEmbarked(false);
        card.SetCurrentSlot(null);
        OnSlotChanged?.Invoke();
        return card;
    }
    public void OnBeginDrag(PointerEventData e)
    {
        if (m_AssignedCard == null) { e.pointerDrag = null; return; }
        m_WasDragging = true;
        Canvas canvas = GetComponentInParent<Canvas>().rootCanvas;
        NikkeDragEvents.BeginGhost(canvas, m_AssignedCard.BoundInstance.Data.PortraitSprite, m_PortraitImage.rectTransform.sizeDelta, e.position);
        m_EmptyVisual.SetActive(true);
        m_PortraitImage.gameObject.SetActive(false);
        NikkeDragEvents.RaiseDragStarted(NikkeDragEvents.Source.Slot);
    }
    public void OnDrag(PointerEventData e)
    {
        NikkeDragEvents.UpdateGhost(e.position);
    }
    public void OnEndDrag(PointerEventData e)
    {
        NikkeDragEvents.EndGhost();
        NikkeDragEvents.RaiseDragEnded(NikkeDragEvents.Source.Slot);

        // 드롭 실패시 원복
        if (m_AssignedCard != null)
        {
            m_PortraitImage.gameObject.SetActive(true);
            m_EmptyVisual.SetActive(false);
        }
    }
    public void OnDrop(PointerEventData e)
    {
        m_DropIndicator.SetActive(false);
        if (e.pointerDrag == null) return;

        NikkeCardView card = e.pointerDrag.GetComponent<NikkeCardView>();
        if (card != null) { AssignCard(card); return; }

        PartySlotView sourceSlot = e.pointerDrag.GetComponent<PartySlotView>();
        if (sourceSlot == null || sourceSlot == this) return;
        NikkeCardView sourceCard = sourceSlot.m_AssignedCard;
        NikkeCardView myCard = m_AssignedCard;
        sourceSlot.ClearSlot();
        ClearSlot();
        AssignCard(sourceCard);
        if (myCard != null) sourceSlot.AssignCard(myCard);
    }
    public void OnPointerClick(PointerEventData e)
    {
        if (m_WasDragging) { m_WasDragging = false; return; }
        if (m_AssignedCard == null) return;
        ClearSlot();
    }
    private void OnAnyDragStarted(NikkeDragEvents.Source source)
    {
        if (source == NikkeDragEvents.Source.Card && !IsEmpty) return;
        m_DropIndicator.SetActive(true);
    }
    private void OnAnyDragEnded(NikkeDragEvents.Source source)
    {
        m_DropIndicator.SetActive(false);
    }
    public void OnPointerDown(PointerEventData e)
    {
        m_WasDragging = false;
    }

}
