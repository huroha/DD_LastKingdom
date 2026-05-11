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
    private NikkeInstance m_BoundInstance;

    public delegate void RightClickedHandler(NikkeInstance instance);
    public delegate void DroppedHereHandler(int slotIdx, NikkeInstance inst);
    public delegate void SwapHandler(int srcIdx, int tgtIdx);
    public delegate void ClearHandler(int slotIdx);

    public event RightClickedHandler OnRightClicked;
    public event DroppedHereHandler OnDroppedHere;
    public event SwapHandler OnSwapRequested;
    public event ClearHandler OnClearRequested;

    public bool IsEmpty => m_BoundInstance == null;
    public int SlotIndex => m_SlotIndex;

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
    public void Render(NikkeInstance inst)
    {
        m_BoundInstance = inst;
        bool has = inst != null;
        m_PortraitImage.sprite = has ? inst.Data.PortraitSprite : null;
        m_PortraitImage.gameObject.SetActive(has);
        m_EmptyVisual.SetActive(!has);
    }
    
    public void OnBeginDrag(PointerEventData e)
    {
        if (m_BoundInstance == null) { e.pointerDrag = null; return; }
        m_WasDragging = true;
        Canvas canvas = GetComponentInParent<Canvas>().rootCanvas;
        NikkeDragEvents.BeginGhost(canvas, m_BoundInstance.Data.PortraitSprite, m_PortraitImage.rectTransform.sizeDelta, e.position);
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
        if (m_BoundInstance != null)
            Render(m_BoundInstance);
    }
    public void OnDrop(PointerEventData e)
    {
        m_DropIndicator.SetActive(false);
        if (e.pointerDrag == null) return;

        NikkeCardView card = e.pointerDrag.GetComponent<NikkeCardView>();
        if (card != null) { OnDroppedHere?.Invoke(m_SlotIndex, card.BoundInstance); return; }

        PartySlotView src = e.pointerDrag.GetComponent<PartySlotView>();
        if (src == null || src == this) return;
        OnSwapRequested?.Invoke(src.SlotIndex, m_SlotIndex);
    }
    public void OnPointerClick(PointerEventData e)
    {
        if (m_WasDragging) { m_WasDragging = false; return; }
        if (m_BoundInstance == null) return;
        if (e.button == PointerEventData.InputButton.Right) { OnRightClicked?.Invoke(m_BoundInstance); return; }
        OnClearRequested?.Invoke(m_SlotIndex);
    }
    private void OnAnyDragStarted(NikkeDragEvents.Source source)
    {
        if (!IsEmpty) return;  // portrait 있는 슬롯은 indicator 표시 안 함
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
