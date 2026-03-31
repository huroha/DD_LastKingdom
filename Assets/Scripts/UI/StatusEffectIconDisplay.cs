using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Text;
public class StatusEffectIconDisplay : MonoBehaviour
{
    [SerializeField] private Transform m_IconContainer;     // HorizontalLayoutGroup 오브젝트
    [SerializeField] private GameObject m_IconPrefab;       // Image만 가진 간단한 프리팹
    [SerializeField] private int m_MaxDisplayCount;         // 최대 표기 개수

    private CombatTooltip m_Tooltip;

    private List<Image> m_IconPool;     // 오브젝트 풀링용
    private List<TooltipTrigger> m_TriggerPool;

    private void Awake()
    {
        m_IconPool = new List<Image>();
        m_TriggerPool = new List<TooltipTrigger>();
    }

    public void SetTooltip(CombatTooltip tooltip)
    {
        m_Tooltip = tooltip;
    }


    public void Refresh(IReadOnlyList<ActiveStatusEffect> activeEffect)
    {
        for(int i=0; i<m_IconPool.Count; ++i)
            m_IconPool[i].gameObject.SetActive(false);

        if (activeEffect == null || activeEffect.Count == 0)
            return;
        int displayCount = Mathf.Min(activeEffect.Count, m_MaxDisplayCount);
        Image image = null;
        for (int i=0; i< displayCount; ++i)
        {
            if(i < m_IconPool.Count)
            {
                image = m_IconPool[i];
            }
            else
            {
                GameObject obj = Instantiate(m_IconPrefab, m_IconContainer);
                image = obj.GetComponent<Image>();
                m_IconPool.Add(image);
                TooltipTrigger newTrigger = obj.AddComponent<TooltipTrigger>();
                m_TriggerPool.Add(newTrigger);
            }
            if (activeEffect[i].Data == null || activeEffect[i].Data.Icon == null)
                continue;
            // TooltipTrigger 초기화
            TooltipTrigger trigger = m_TriggerPool[i];
            ActiveStatusEffect effect = activeEffect[i];  // 클로저용 로컬 변수
            trigger.Initialize(m_Tooltip, (sb) => BuildEffectTooltip(sb, effect), new Vector2(0, -25));
            image.sprite = activeEffect[i].Data.Icon;
            image.gameObject.SetActive(true);
        }
    }
    public void Clear()
    {
        Refresh(null);
    }

    private void BuildEffectTooltip(StringBuilder sb,ActiveStatusEffect effect)
    {
        int turns = effect.RemainingTurns;

        if(effect.Data.ShowName)
            sb.Append(TooltipHelper.TAG_DEBUFF_OPEN).Append(effect.Data.EffectName).Append(TooltipHelper.TAG_COLOR_CLOSE).Append('\n');

        if (!string.IsNullOrEmpty(effect.Data.Description))
            sb.Append(TooltipHelper.TAG_NORMAL_OPEN).Append(effect.Data.Description).Append(TooltipHelper.TAG_COLOR_CLOSE);

        // 도트 피해
        if (effect.Data.TickDamage > 0)
        {
            sb.Append(TooltipHelper.TAG_DEBUFF_OPEN);
            sb.Append(effect.Data.TickDamage);
            sb.Append(" 피해 (");
            sb.Append(turns);
            sb.Append("차례)");
            sb.Append(TooltipHelper.TAG_COLOR_CLOSE);

        }

        // 스탯 변화
        StatBlock mod = effect.Data.StatModifier;
        if (mod.damageMultiplier != 0f)
            TooltipHelper.AppendStatPercent(sb, TooltipHelper.STAT_DAMAGE, (int)mod.damageMultiplier, turns);
        if (mod.accuracyMod != 0)
            TooltipHelper.AppendStat(sb, TooltipHelper.STAT_ACCURACY, mod.accuracyMod, turns);
        if (mod.critChance != 0f)
            TooltipHelper.AppendStat(sb, TooltipHelper.STAT_CRIT, (int)mod.critChance, turns);
        if (mod.defense != 0f)
            TooltipHelper.AppendStatPercent(sb, TooltipHelper.STAT_DEFENCE, (int)mod.defense, turns);
        if (mod.dodge != 0)
            TooltipHelper.AppendStat(sb, TooltipHelper.STAT_DODGE, mod.dodge, turns);
        if (mod.speed != 0)
            TooltipHelper.AppendStat(sb, TooltipHelper.STAT_SPEED, mod.speed, turns);
    }

}
