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

    public void Show(SkillData skill, int skillLevel, Vector2 screenPosition, Vector2 offset)
    {
        if (m_CurrentSkill == skill && gameObject.activeSelf)
            return;
        SkillLevelData ld = skill.GetLevelData(skillLevel);
        m_CurrentSkill = skill;
        string prefix = (skill.TargetType == TargetType.AllyAll) ? "스쿼드 " : "";
        m_SB.Clear();
        // 스킬명 + Lv
        m_SB.Append("<b>").Append(skill.SkillName).Append(" </b>").Append(skillLevel).Append("\n"); 
        // 스킬 타입
        if(skill.IsEnemyTargeting)
            m_SB.Append(TooltipHelper.TAG_SKILLTYPE).Append(skill.SkillType == SkillType.Melee ? "근접" : "원거리").Append("\n").Append(TooltipHelper.TAG_COLOR_CLOSE);
        // 기본 스탯
        if(ld.accuracyMod != 0)
            m_SB.Append("명중 : ").Append(ld.accuracyMod).Append("\n");
        if(ld.damageMultiplier != 1 && ld.damageMultiplier != 0)
            m_SB.Append("피해 보정: ").Append((int)(ld.damageMultiplier * 100f) - 100).Append("%").Append("\n");
        if (ld.maxHeal != 0)
            m_SB.Append(prefix).Append(ld.minHeal).Append("-").Append(ld.maxHeal).Append(TooltipHelper.TAG_HEAL).Append("회복\n").Append(TooltipHelper.TAG_COLOR_CLOSE);
        if (ld.eblaHealAmount != 0)
            m_SB.Append(prefix).Append("에블라 : -").Append(ld.eblaHealAmount).Append("\n");
        if (ld.allyEblaAmount != 0)
        {
            string allyLabel = skill.ExcludeAllyEffect ? "자신 제외 스쿼드 에블라 : " : "스쿼드 에블라 : ";
            m_SB.Append(allyLabel).Append(ld.allyEblaAmount > 0 ? "+" : "").Append(ld.allyEblaAmount).Append("\n");
        }
        if (ld.critMod != 0)
            m_SB.Append("치명타 보정: ").Append((int)ld.critMod).Append("%\n");
        if (skill.MarkBonus)
            m_SB.Append(TooltipHelper.TAG_NORMAL_OPEN).Append("표식 추가 데미지: +").Append((int)(skill.MarkDamageBonus * 100f)).Append("%\n").Append(TooltipHelper.TAG_COLOR_CLOSE);
        if (skill.IsGuard)
            m_SB.Append("아군 보호 (").Append(skill.GuardDuration).Append("차례)\n");
        else if (skill.IsForceGuard)
            m_SB.Append("강제 보호 대상 지정 (").Append(skill.GuardDuration).Append("차례)\n");

        // OnHitEffects
        if (ld.onHitEffects != null && ld.onHitEffects.Length > 0)
        {
            m_SB.Append("\n").Append(prefix).Append("대상:\n");
            for (int i = 0; i < ld.onHitEffects.Length; ++i)
            {
                if (ld.onHitEffects[i] == null) continue;
                BuildEffectText(m_SB, ld.onHitEffects[i]);   // label 인자 생략
            }
        }

        // OnSelfEffects
        if (ld.onSelfEffects != null && ld.onSelfEffects.Length > 0)
        {
            m_SB.Append("\n자신:\n");
            for (int i = 0; i < ld.onSelfEffects.Length; ++i)
            {
                if (ld.onSelfEffects[i] == null) continue;
                BuildEffectText(m_SB, ld.onSelfEffects[i]);
            }
        }

        // OnAllyEffects
        if (ld.onAllyEffects != null && ld.onAllyEffects.Length > 0)
        {
            string header = skill.ExcludeAllyEffect ? "자신 제외 스쿼드:" : "스쿼드:";
            m_SB.Append("\n").Append(header).Append("\n");
            for (int i = 0; i < ld.onAllyEffects.Length; ++i)
            {
                if (ld.onAllyEffects[i] == null) continue;
                BuildEffectText(m_SB, ld.onAllyEffects[i]);
            }
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
        string effectColor = TooltipHelper.GetEffectColorTag(effect.EffectType);
        if (effect.EffectType == StatusEffectType.Block)
        {
            sb.Append("피해 차단 ").Append(effect.MaxStack).Append("회\n");
            return;
        }
        // 효과명 (기본 100%)
        sb.Append(effectColor).Append(effect.EffectName).Append(TooltipHelper.TAG_COLOR_CLOSE)
          .Append(" (기본 ").Append(effect.BaseApplyRate).Append("%)\n");

        // Description이 있으면 표시
        if (!string.IsNullOrEmpty(effect.Description))
            sb.Append(effect.Description).Append("\n");
        
        if (effect.TickDamage != 0)
            sb.Append("턴당 ").Append(effect.TickDamage).Append(" 피해,").Append(effect.Duration).Append("턴 지속");

        // 스탯 변화가 있으면
        TooltipHelper.AppendStatBlock(sb, effect.StatModifier);
    }
}
