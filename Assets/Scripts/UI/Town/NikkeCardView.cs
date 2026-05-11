using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class NikkeCardView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    [SerializeField] private TextMeshProUGUI m_WeaponLvText;
    [SerializeField] private TextMeshProUGUI m_ArmorLvText;
    [SerializeField] private Image m_LevelIcon;
    [SerializeField] private Image m_LevelElement;
    [SerializeField] private Image m_SelectOverlay;
    [SerializeField] private Image m_DimOverlay;
    [SerializeField] private Image m_Portrait;
    [SerializeField] private TextMeshProUGUI m_NameText;

    [SerializeField] private Sprite[] m_LevelSprites;
    [SerializeField] private Sprite[] m_LevelElementSprites;

    private NikkeInstance m_BoundInstance;
    private RectTransform m_RectTransform;
    

    public delegate void CardClickHandler(NikkeCardView card);
    public delegate void CardDragHandler(NikkeCardView card);

    public event CardClickHandler OnClicked;
    public event CardClickHandler OnRightClicked;
    public event CardDragHandler OnDragBegan;

    public NikkeInstance BoundInstance => m_BoundInstance;

    private void Awake()
    {
        m_RectTransform = GetComponent<RectTransform>();
    }
    public void Bind(NikkeInstance instance)
    {
        if (instance == null) return;
        m_BoundInstance = instance;
        m_NameText.text = instance.DisplayName;
        m_Portrait.sprite = instance.Data.PortraitSprite;
        m_WeaponLvText.SetText("{0}", instance.WeaponLevel);
        m_ArmorLvText.SetText("{0}", instance.ArmorLevel);
        int lv = instance.Level;
        m_LevelIcon.sprite = m_LevelSprites[Mathf.Clamp(lv, 0, m_LevelSprites.Length - 1)];
        m_LevelElement.sprite = m_LevelElementSprites[Mathf.Clamp(lv, 0, m_LevelElementSprites.Length - 1)];

        SetEmbarked(false);
    }
    public void OnBeginDrag(PointerEventData e)
    {
        OnDragBegan?.Invoke(this);
        Canvas canvas = GetComponentInParent<Canvas>().rootCanvas;
        NikkeDragEvents.BeginGhost(canvas, m_Portrait.sprite, m_Portrait.rectTransform.sizeDelta, e.position);
        NikkeDragEvents.RaiseDragStarted(NikkeDragEvents.Source.Card);
    }
    public void OnDrag(PointerEventData e)
    {
        NikkeDragEvents.UpdateGhost(e.position);
    }
    public void OnEndDrag(PointerEventData e)
    {
        NikkeDragEvents.EndGhost();
        NikkeDragEvents.RaiseDragEnded(NikkeDragEvents.Source.Card);
    }
    public void OnPointerClick(PointerEventData e)
    {
        if (e.dragging) return;
        if (e.button == PointerEventData.InputButton.Left)
            OnClicked?.Invoke(this);
        else if (e.button == PointerEventData.InputButton.Right)
            OnRightClicked?.Invoke(this);
    }
    public void SetEmbarked(bool embarked)
    {
        m_DimOverlay.enabled = embarked;
        m_SelectOverlay.enabled = embarked;
    }
    public void ResetTransform()
    {
        m_RectTransform.anchoredPosition = Vector2.zero;
    }
}
