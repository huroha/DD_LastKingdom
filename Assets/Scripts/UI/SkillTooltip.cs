using UnityEngine;
using TMPro;
using System.Text;
using UnityEngine.UI;

public class SkillTooltip : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI m_TooltipText;
    [SerializeField] private SkillPositionDisplay m_PositionDisplay;
    [SerializeField] private RectTransform m_RectTransform;
    [SerializeField] private Vector2 m_Padding;

    private RectTransform m_PositionDisplayRect;
    private StringBuilder m_SB = new StringBuilder(256);

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
        LayoutRebuilder.ForceRebuildLayoutImmediate(m_RectTransform);

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
        TooltipHelper.ClampToScreen(m_RectTransform);

    }

    public void Hide()
    {
        m_CurrentSkill = null;
        gameObject.SetActive(false);
    }

    private void BuildEffectText(StringBuilder sb, StatusEffectData effect)
    {
        // 효과명 (기본 100%)
        sb.Append(TooltipHelper.TAG_BUFF_OPEN).Append(effect.EffectName).Append(TooltipHelper.TAG_COLOR_CLOSE)
          .Append(" (기본 100%)\n");

        // Description이 있으면 표시
        if (!string.IsNullOrEmpty(effect.Description))
            sb.Append(effect.Description).Append("\n");

        // 스탯 변화가 있으면 약화: 항목 표시
        StatBlock mod = effect.StatModifier;
        bool hasStatMod = mod.damageMultiplier != 0 || mod.accuracyMod != 0
                       || mod.critChance != 0 || mod.defense != 0 || mod.dodge != 0 || mod.speed != 0;
        if (hasStatMod)
        {
            sb.Append("<b>약화:</b>\n");
            if (mod.damageMultiplier != 0)
                TooltipHelper.AppendStatPercent(sb, TooltipHelper.STAT_DAMAGE, (int)mod.damageMultiplier);
            if (mod.accuracyMod != 0)
                TooltipHelper.AppendStat(sb, TooltipHelper.STAT_ACCURACY, mod.accuracyMod);
            if (mod.defense != 0)
                TooltipHelper.AppendStatPercent(sb, TooltipHelper.STAT_DEFENCE, (int)mod.defense);
            if (mod.dodge != 0)
                TooltipHelper.AppendStat(sb, TooltipHelper.STAT_DODGE, mod.dodge);
            if (mod.speed != 0)
                TooltipHelper.AppendStat(sb, TooltipHelper.STAT_SPEED, mod.speed);
            if (mod.critChance != 0f)
                TooltipHelper.AppendStat(sb, TooltipHelper.STAT_CRIT, (int)mod.critChance);
        }
    }
}
