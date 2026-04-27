using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Text;
public class StatusEffectIconDisplay : MonoBehaviour
{
    [SerializeField] private Transform m_IconContainer;     // HorizontalLayoutGroup 오브젝트
    [SerializeField] private GameObject m_IconPrefab;       // Image만 가진 간단한 프리팹
    [SerializeField] private int m_MaxDisplayCount;         // 최대 표기 개수
    [SerializeField] private Sprite m_GuardedIcon;

    private CombatTooltip m_Tooltip;

    private ActiveStatusEffect[] m_SlotEffects;     // 효과 슬롯
    private CombatUnit m_GuardSlotUnit;             // 가드 아이콘 슬롯의 unit
    private int m_GuardSlotIndex = -1;              // 가드가 차지한 슬롯 인덱스

    private List<Image> m_IconPool;     // 오브젝트 풀링용
    private List<TooltipTrigger> m_TriggerPool;

    private void Awake()
    {
        m_IconPool = new List<Image>();
        m_TriggerPool = new List<TooltipTrigger>();
        m_SlotEffects = new ActiveStatusEffect[m_MaxDisplayCount];
    }

    public void SetTooltip(CombatTooltip tooltip)
    {
        m_Tooltip = tooltip;
    }


    public void Refresh(IReadOnlyList<ActiveStatusEffect> activeEffect, CombatUnit unit)
    {
        for (int i = 0; i < m_IconPool.Count; ++i)
            m_IconPool[i].gameObject.SetActive(false);

        for (int i = 0; i < m_SlotEffects.Length; ++i)
            m_SlotEffects[i] = null;
        m_GuardSlotUnit = null;
        m_GuardSlotIndex = -1;

        int displayIndex = 0;

        if (activeEffect != null)
        {
            for (int i = 0; i < activeEffect.Count && displayIndex < m_MaxDisplayCount; ++i)
            {
                if (activeEffect[i].Data.EffectType == StatusEffectType.Stun)
                    continue;

                Image image = GetOrCreateIcon(displayIndex);

                if (activeEffect[i].Data == null || activeEffect[i].Data.Icon == null)
                {
                    ++displayIndex;
                    continue;
                }

                m_SlotEffects[displayIndex] = activeEffect[i];
                image.sprite = activeEffect[i].Data.Icon;
                image.gameObject.SetActive(true);
                ++displayIndex;
            }
        }

        if (unit != null && unit.GuardedBy != null && displayIndex < m_MaxDisplayCount)
        {
            Image image = GetOrCreateIcon(displayIndex);
            image.sprite = m_GuardedIcon;
            m_GuardSlotUnit = unit;
            m_GuardSlotIndex = displayIndex;
            image.gameObject.SetActive(true);
            ++displayIndex;
        }
    }
    public void Clear()
    {
        Refresh(null,null);
    }

    private void BuildEffectTooltip(StringBuilder sb,ActiveStatusEffect effect)
    {
        int turns = effect.RemainingTurns;

        if(effect.Data.ShowName)
            sb.Append(TooltipHelper.TAG_DEBUFF_OPEN).Append(effect.Data.EffectName).Append(TooltipHelper.TAG_COLOR_CLOSE).Append('\n');

        if (effect.Data.TickDamage == 0 && turns > 0)
            sb.Append(TooltipHelper.TAG_NORMAL_OPEN).Append(turns).Append("턴 남음").Append(TooltipHelper.TAG_COLOR_CLOSE);

        if (!string.IsNullOrEmpty(effect.Data.Description))
            sb.Append(TooltipHelper.TAG_NORMAL_OPEN).Append(effect.Data.Description).Append(TooltipHelper.TAG_COLOR_CLOSE);

        // 도트 피해
        if (effect.Data.TickDamage > 0)
        {
            sb.Append(TooltipHelper.TAG_DEBUFF_OPEN);
            sb.Append(effect.AccumulatedTickDamage);
            sb.Append(" 피해 (");
            sb.Append(turns);
            sb.Append("차례)");
            sb.Append(TooltipHelper.TAG_COLOR_CLOSE);

        }

        // 스탯 변화
        TooltipHelper.AppendStatBlock(sb, effect.Data.StatModifier, turns);
    }
    private Image GetOrCreateIcon(int displayIndex)
    {
        if (displayIndex < m_IconPool.Count)
            return m_IconPool[displayIndex];

        GameObject obj = Instantiate(m_IconPrefab, m_IconContainer);
        Image image = obj.GetComponent<Image>();
        m_IconPool.Add(image);

        TooltipTrigger trigger = obj.AddComponent<TooltipTrigger>();
        int slot = displayIndex;   // capture 1회
        trigger.Initialize(m_Tooltip, (sb) => BuildSlotTooltip(sb, slot), new Vector2(0, -25));
        m_TriggerPool.Add(trigger);
        return image;
    }
    private void BuildSlotTooltip(StringBuilder sb, int slot)
    {
        if (slot == m_GuardSlotIndex && m_GuardSlotUnit != null && m_GuardSlotUnit.GuardedBy != null)
        {
            CombatUnit guardian = m_GuardSlotUnit.GuardedBy;
            int turns = m_GuardSlotUnit.GuardTurnsRemaining;
            sb.Append(TooltipHelper.TAG_BUFF_OPEN).Append("보호받는중").Append(TooltipHelper.TAG_COLOR_CLOSE).Append('\n');
            sb.Append(TooltipHelper.TAG_NORMAL_OPEN).Append(guardian.UnitName).Append("(").Append(turns).Append("턴)").Append(TooltipHelper.TAG_COLOR_CLOSE);
            return;
        }
        ActiveStatusEffect effect = m_SlotEffects[slot];
        if (effect != null)
            BuildEffectTooltip(sb, effect);
    }
}
