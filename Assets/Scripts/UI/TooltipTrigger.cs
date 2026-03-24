
using UnityEngine;
using UnityEngine.EventSystems;

public class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private CombatTooltip m_Tooltip;
    private System.Func<string> m_ContentGetter;

    public void Initialize(CombatTooltip tooltip, System.Func<string> contentGetter)
    {
        m_Tooltip = tooltip;
        m_ContentGetter = contentGetter;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (m_ContentGetter == null)
            return;
        string text = m_ContentGetter();
        if (text == null || text.Length == 0)
            return;
        m_Tooltip.Show(text, eventData.position);
    }


    public void OnPointerExit(PointerEventData eventData)
    {
        m_Tooltip.Hide();
    }

}
