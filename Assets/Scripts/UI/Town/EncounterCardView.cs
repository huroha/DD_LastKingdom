using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
public class EncounterCardView : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private TextMeshProUGUI m_NameText;
    [SerializeField] private Image m_Icon;

    private EncounterData m_Data;
    public delegate void ClickHandler(EncounterData data);
    public event ClickHandler OnClicked;

    public void Bind(EncounterData data)
    {
        m_Data = data;
        m_NameText.text = data.EncounterName;
        m_Icon.sprite = data.Icon;
        m_Icon.enabled = data.Icon != null;
    }
    public void OnPointerClick(PointerEventData e) => OnClicked?.Invoke(m_Data);
}
