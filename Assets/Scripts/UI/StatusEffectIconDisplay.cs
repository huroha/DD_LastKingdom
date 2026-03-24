using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
public class StatusEffectIconDisplay : MonoBehaviour
{
    [SerializeField] private Transform m_IconContainer;     // HorizontalLayoutGroup 오브젝트
    [SerializeField] private GameObject m_IconPrefab;       // Image만 가진 간단한 프리팹
    [SerializeField] private int m_MaxDisplayCount;         // 최대 표기 개수

    private CombatTooltip m_Tooltip;

    private List<Image> m_IconPool;     // 오브젝트 풀링용

    private System.Text.StringBuilder m_TooltipBuilder = new System.Text.StringBuilder(64);

    public void SetTooltip(CombatTooltip tooltip)
    {
        m_Tooltip = tooltip;
    }


    public void Refresh(List<ActiveStatusEffect> activeEffect)
    {
        if(m_IconPool == null)
            m_IconPool = new List<Image>();
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
            }
            if (activeEffect[i].Data == null || activeEffect[i].Data.Icon == null)
                continue;
            // TooltipTrigger 초기화
            TooltipTrigger trigger = image.GetComponent<TooltipTrigger>();
            if (trigger == null)
                trigger = image.gameObject.AddComponent<TooltipTrigger>();
            ActiveStatusEffect effect = activeEffect[i];  // 클로저용 로컬 변수
            trigger.Initialize(m_Tooltip, () => BuildEffectTooltip(effect));
            image.sprite = activeEffect[i].Data.Icon;
            image.gameObject.SetActive(true);
        }
    }
    public void Clear()
    {
        Refresh(null);
    }

    private string BuildEffectTooltip(ActiveStatusEffect effect)
    {
        m_TooltipBuilder.Clear();
        m_TooltipBuilder.Append(effect.Data.EffectName);

        int turns = effect.RemainingTurns;

        // 도트 피해
        if (effect.Data.TickDamage > 0)
        {
            m_TooltipBuilder.Append("\n매 차례마다 ");
            m_TooltipBuilder.Append(effect.Data.TickDamage);
            m_TooltipBuilder.Append("피해 (");
            m_TooltipBuilder.Append(turns);
            m_TooltipBuilder.Append("차례)");
        }

        // 스탯 변화
        StatBlock mod = effect.Data.StatModifier;
        if (mod.damageMultiplier != 0f)
            AppendStatPercent("피해", (int)mod.damageMultiplier, turns);
        if (mod.accuracyMod != 0)
            AppendStat("명중", mod.accuracyMod, turns);
        if (mod.critChance != 0f)
            AppendStat("치명타", (int)mod.critChance, turns);
        if (mod.defense != 0f)
            AppendStatPercent("방어력", (int)mod.defense, turns);
        if (mod.dodge != 0)
            AppendStat("회피", mod.dodge, turns);
        if (mod.speed != 0)
            AppendStat("속도", mod.speed, turns);

        return m_TooltipBuilder.ToString();
    }

    private void AppendStat(string statName, int value, int turns)
    {
        m_TooltipBuilder.Append('\n');
        m_TooltipBuilder.Append(statName);
        m_TooltipBuilder.Append(' ');
        if (value > 0)
            m_TooltipBuilder.Append('+');
        m_TooltipBuilder.Append(value);
        m_TooltipBuilder.Append('(');
        m_TooltipBuilder.Append(turns);
        m_TooltipBuilder.Append("차례)");
    }

    private void AppendStatPercent(string statName, int value, int turns)
    {
        m_TooltipBuilder.Append('\n');
        m_TooltipBuilder.Append(statName);
        m_TooltipBuilder.Append(' ');
        if (value > 0)
            m_TooltipBuilder.Append('+');
        m_TooltipBuilder.Append(value);
        m_TooltipBuilder.Append("%(");
        m_TooltipBuilder.Append(turns);
        m_TooltipBuilder.Append("차례)");
    }
}
