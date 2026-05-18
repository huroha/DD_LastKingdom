using UnityEngine;
using UnityEngine.EventSystems;

public class SkillTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{

    private SkillTooltip m_Tooltip;
    private SkillData m_Skill;
    private int m_SkillLevel;
    private Vector2 m_Offset;

    public void Initialize(SkillTooltip tooltip, SkillData skillData, int skillLevel, Vector2 offset)
    {
        m_Tooltip = tooltip;
        m_Skill = skillData;
        m_SkillLevel = skillLevel;
        m_Offset = offset;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (m_Skill == null) return;
        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(eventData.pressEventCamera, transform.position);
        m_Tooltip.Show(m_Skill, m_SkillLevel, screenPos, m_Offset);
    }
    public void OnPointerExit(PointerEventData eventData)
    {
        if (m_Tooltip == null) return;
        m_Tooltip.Hide();
    }
}
