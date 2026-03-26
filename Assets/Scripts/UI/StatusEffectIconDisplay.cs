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
            trigger.Initialize(m_Tooltip, (sb) => BuildEffectTooltip(sb, effect), new Vector2(0, -50));
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
            sb.Append("<color=#BF1313>").Append(effect.Data.EffectName).Append("</color>\n");

        if (!string.IsNullOrEmpty(effect.Data.Description))
            sb.Append("<color=#DBDBD0>").Append(effect.Data.Description).Append("</color>");

        // 도트 피해
        if (effect.Data.TickDamage > 0)
        {
            sb.Append("<color=#BF1313>매 차례마다\n");
            sb.Append(effect.Data.TickDamage);
            sb.Append(" 피해 (");
            sb.Append(turns);
            sb.Append("차례)</color>");
        }

        // 스탯 변화
        StatBlock mod = effect.Data.StatModifier;
        if (mod.damageMultiplier != 0f)
            AppendStatPercent(sb, "피해", (int)mod.damageMultiplier, turns);
        if (mod.accuracyMod != 0)
            AppendStat(sb, "명중", mod.accuracyMod, turns);
        if (mod.critChance != 0f)
            AppendStat(sb, "치명타", (int)mod.critChance, turns);
        if (mod.defense != 0f)
            AppendStatPercent(sb, "방어력", (int)mod.defense, turns);
        if (mod.dodge != 0)
            AppendStat(sb, "회피", mod.dodge, turns);
        if (mod.speed != 0)
            AppendStat(sb, "속도", mod.speed, turns);

    }

    private void AppendStat(StringBuilder sb,string statName ,int value, int turns)
    {
        sb.Append('\n');
        sb.Append(statName);
        sb.Append(' ');
        if (value > 0)
            sb.Append('+');
        sb.Append(value);
        if(turns > 0)
        {
            sb.Append('(');
            sb.Append(turns);
            sb.Append("차례)");
        }
    }

    private void AppendStatPercent(StringBuilder sb, string statName, int value, int turns)
    {
        sb.Append('\n');
        sb.Append(statName);
        sb.Append(' ');
        if (value > 0)
            sb.Append('+');
        sb.Append(value);
        if(turns >0)
        {
            sb.Append("%(");
            sb.Append(turns);
            sb.Append("차례)");
        }

    }
}
