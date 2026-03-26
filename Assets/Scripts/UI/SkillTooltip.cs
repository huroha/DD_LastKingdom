using UnityEngine;
using TMPro;
using System.Text;

public class SkillTooltip : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI m_TooltipText;
    [SerializeField] private SkillPositionDisplay m_PositionDisplay;
    [SerializeField] private RectTransform m_RectTransform;
    [SerializeField] private Vector2 m_Padding;

    private RectTransform m_PositionDisplayRect;
    private StringBuilder m_SB = new StringBuilder(256);
    private Vector3[] m_Corners = new Vector3[4];
    private Camera m_UICamera;

    private SkillData m_CurrentSkill;

    private void Awake()
    {
        m_PositionDisplayRect = m_PositionDisplay.GetComponent<RectTransform>();
        m_UICamera = null;
        gameObject.SetActive(false);
    }

    public void Show(SkillData skill, Vector2 screenPosition, Vector2 offset)
    {
        if (m_CurrentSkill == skill && gameObject.activeSelf)
            return;
        m_CurrentSkill = skill;
        m_SB.Clear();
        // 스킬명 + Lv
        m_SB.Append("<b>").Append(skill.SkillName).Append("</b>1\n"); // 숫자는 추후 skilldata 멤버 추가해서 가져올것.

        // 스킬 타입
        m_SB.Append(skill.SkillType == SkillType.Melee ? "근접" : "원거리").Append("\n\n");

        // 기본 스탯
        m_SB.Append("기본 명중: ").Append(skill.AccuracyMod).Append("\n");
        m_SB.Append("피해 보정: ").Append((int)(skill.DamageMultiplier * 100f) - 100).Append("%\n");

        if (skill.CritMod > 0)
            m_SB.Append("치명타 보정: +").Append((int)skill.CritMod).Append("%\n");

        // OnHitEffects
        if (skill.OnHitEffects != null && skill.OnHitEffects.Count > 0)
        {
            m_SB.Append("\n");
            for (int i = 0; i < skill.OnHitEffects.Count; ++i)
                BuildEffectText(m_SB, skill.OnHitEffects[i]);
        }

        m_TooltipText.SetText(m_SB);

        m_RectTransform.sizeDelta = Vector2.zero;
        gameObject.SetActive(true);
        Canvas.ForceUpdateCanvases();

        m_PositionDisplay.Refresh(skill);

        float totalWidth = Mathf.Max(m_TooltipText.preferredWidth,
                                     m_PositionDisplayRect.rect.width) + m_Padding.x;
        float totalHeight = m_TooltipText.preferredHeight
                          + m_PositionDisplayRect.rect.height
                          + m_Padding.y;
        m_RectTransform.sizeDelta = new Vector2(totalWidth, totalHeight);

        RectTransform parentRect = m_RectTransform.parent as RectTransform;
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPosition, m_UICamera, out localPoint);
        m_RectTransform.anchoredPosition = localPoint + offset;
        ClampToScreen();

    }

    public void Hide()
    {
        m_CurrentSkill = null;
        gameObject.SetActive(false);
    }

    private void ClampToScreen() 
    {

        m_RectTransform.GetWorldCorners(m_Corners);

        float offsetX = 0f;
        float offsetY = 0f;

        // 오른쪽 빠져나감
        if (m_Corners[2].x > Screen.width)
            offsetX = Screen.width - m_Corners[2].x;
        if (m_Corners[0].x < 0f)
            offsetX = -m_Corners[0].x;
        if (m_Corners[1].y > Screen.height)
            offsetY = Screen.height - m_Corners[1].y;
        if (m_Corners[0].y < 0f)
            offsetY = -m_Corners[0].y;

        m_RectTransform.anchoredPosition += new Vector2(offsetX, offsetY);
    }

    private void BuildEffectText(StringBuilder sb, StatusEffectData effect)
    {
        // 효과명 (기본 100%)
        sb.Append("<color=#FF4444>").Append(effect.EffectName).Append("</color>")
          .Append(" (기본 100%)\n");

        // Description이 있으면 표시
        if (!string.IsNullOrEmpty(effect.Description))
            sb.Append(effect.Description).Append("\n");

        // 스탯 변화가 있으면 약화: 항목 표시
        StatBlock mod = effect.StatModifier;
        bool hasStatMod = mod.damageMultiplier != 0 || mod.accuracyMod != 0
                       || mod.defense != 0 || mod.dodge != 0 || mod.speed != 0;
        if (hasStatMod)
        {
            sb.Append("<b>약화:</b>\n");
            if (mod.damageMultiplier != 0)
                sb.Append("피해").Append(mod.damageMultiplier > 0 ? "+":"").Append((int)mod.damageMultiplier).Append("%\n");
            if (mod.accuracyMod != 0)
                sb.Append("명중 ").Append(mod.accuracyMod > 0 ? "+" : "").Append(mod.accuracyMod).Append("\n");
            if (mod.defense != 0)
                sb.Append("방어").Append(mod.defense > 0 ? "+" : "").Append((int)mod.defense).Append("%\n");
            if (mod.dodge != 0)
                sb.Append("회피").Append(mod.dodge > 0 ? "+" : "").Append(mod.dodge).Append("\n");
            if (mod.speed != 0)
                sb.Append("속도").Append(mod.speed > 0 ? "+" : "").Append(mod.speed).Append("\n");
        }
    }
}
