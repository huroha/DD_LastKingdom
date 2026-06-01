using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class LootSlot : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private Image m_Icon;
    [SerializeField] private TextMeshProUGUI m_QuantityText;
    [SerializeField] private Button m_Button;

    private LootItem m_Item;
    private bool m_Collected;

    public delegate void CollectHandler(LootSlot slot);
    public event CollectHandler OnCollected;

    public LootItem Item => m_Item;
    public bool IsCollected => m_Collected;

    private void Awake()
    {
        m_Button.onClick.AddListener(OnClick);
    }
    public void Setup(LootItem item, Sprite iconSprite)
    {
        m_Item = item;
        m_Collected = false;
        m_Icon.sprite = iconSprite;
        m_QuantityText.SetText(item.Quantity > 1 ? "{0}" : "", item.Quantity);
        gameObject.SetActive(true);
    }
    private void OnClick()
    {
        if (m_Collected) return;
        m_Collected = true;
        gameObject.SetActive(false);
        if (OnCollected != null) OnCollected(this);
    }
    public void ForceCollect()
    {
        if (m_Collected) return;
        OnClick();
    }
    public void Restore()
    {
        m_Collected = false;
        gameObject.SetActive(true);
    }
    public void ConsumeWithoutEvent()
    {
        if (m_Collected) return;
        m_Collected = true;
        gameObject.SetActive(false);
    }
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (m_Collected || m_Item.Quantity == 0)
        {
            eventData.pointerDrag = null;
            return;
        }
        LootDragState.IsDragging = true;
        LootDragState.Item = m_Item;
        LootDragState.From = DragSource.Ground;
        LootDragState.GroundSlot = this;
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
    private void OnDisable()
    {
        if (LootDragState.GroundSlot != this) return;
        LootDragGhost.Instance.Hide();
        LootDragState.Clear();
    }
}
