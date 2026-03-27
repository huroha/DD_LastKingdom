using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;

public class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public delegate void ContentBuilderHandler(StringBuilder sb);

    private CombatTooltip m_Tooltip;
    private ContentBuilderHandler m_ContentBuilder;
    private StringBuilder m_Sb = new StringBuilder(128);
    private Vector2 m_Offset;
    public void Initialize(CombatTooltip tooltip, ContentBuilderHandler contentGetter, Vector2 offset = default)
    {
        m_Tooltip = tooltip;
        m_ContentBuilder = contentGetter;
        m_Offset = offset;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (m_ContentBuilder == null) return;
        m_Sb.Clear();
        m_ContentBuilder(m_Sb);
        if (m_Sb.Length == 0) return;

        // eventData.position 대신 바의 위치 사용
        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(
            eventData.pressEventCamera,
            transform.position
        );
        m_Tooltip.Show(m_Sb, screenPos, m_Offset);
    }


    public void OnPointerExit(PointerEventData eventData)
    {
        if (m_Tooltip == null)
            return;
        m_Tooltip.Hide();
    }


}
