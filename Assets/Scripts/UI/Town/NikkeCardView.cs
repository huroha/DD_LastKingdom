using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class NikkeCardView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    [SerializeField] private Image m_Portrait;
    [SerializeField] private TextMeshProUGUI m_NameText;

    private NikkeInstance m_BoundInstance;
    private Transform m_OriginalParent;
    private int m_OriginalSiblingIndex;
    private Canvas m_RootCanvas;

    public delegate void CardClickHandler(NikkeCardView card);
    public event CardClickHandler OnClicked;

    public NikkeInstance BoundInstance => m_BoundInstance;

    private void Awake()
    {
        m_RootCanvas = GetComponentInParent<Canvas>().rootCanvas;
    }
    public void Bind(NikkeInstance instance)
    {
        m_BoundInstance = instance;
        m_NameText.text = instance.DisplayName;
        m_Portrait.sprite = instance.Data.PortraitSprite;
    }
    public void OnBeginDrag(PointerEventData e)
    {
        m_OriginalParent = transform.parent;
        m_OriginalSiblingIndex = transform.GetSiblingIndex();
        transform.SetParent(m_RootCanvas.transform);
        transform.SetAsLastSibling();
    }
    public void OnDrag(PointerEventData e)
    {
        transform.position = e.position;
    }
    public void OnEndDrag(PointerEventData e)
    {
        // 드롭 성공 시 partyslot view.Ondrop이 부모를 슬롯으로 바꿔줌
        // 도달 실패시 원래 자리 복귀
        if (transform.parent == m_RootCanvas.transform)
        {
            transform.SetParent(m_OriginalParent);
            transform.SetSiblingIndex(m_OriginalSiblingIndex);
        }
    }
    public void OnPointerClick(PointerEventData e)
    {
        if (e.dragging) return;
        OnClicked?.Invoke(this);
    }
}
