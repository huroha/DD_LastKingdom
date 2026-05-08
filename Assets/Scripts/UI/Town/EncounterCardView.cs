using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
public class EncounterCardView : MonoBehaviour, IPointerClickHandler
{

    [SerializeField] private Image m_BgIcon;
    [SerializeField] private Image m_Icon;
    [SerializeField] private Image m_SelectOverlay;

    private EncounterData m_Data;
    public delegate void ClickHandler(EncounterCardView card, EncounterData data);
    public event ClickHandler OnClicked;

    public void Bind(EncounterData data)
    {
        if (data == null) return;
        m_Data = data;
        m_BgIcon.sprite = data.BgIconSprite;
        m_Icon.sprite = data.IconSprite;
    }
    public void SetSelected(bool selected)
    {
        m_SelectOverlay.enabled = selected;
    }
    public void OnPointerClick(PointerEventData e) => OnClicked?.Invoke(this, m_Data);
}
