using UnityEngine;
using System.Collections.Generic;


public struct DotTickResult
{
    public CombatUnit Unit;
    public StatusEffectData Effect;
    public int Damage;
    public UnitState PreviousState;
    public UnitState ResultState;
}

public class StatusEffectManager
{
    private StatusEffectData m_StunResistBuff;
    
    private List<DotTickResult> m_DotResultsBuffer = new List<DotTickResult>();


    public StatusEffectManager(StatusEffectData stunResistBuff)
    {
        m_StunResistBuff = stunResistBuff;
    }

    public List<DotTickResult> ProcessTurnStart(CombatUnit unit) 
    {
        m_DotResultsBuffer.Clear();
        if (!unit.IsAlive)
            return m_DotResultsBuffer;
        for(int i=0; i<unit.ActiveEffects.Count; ++i)
        {
            ActiveStatusEffect effect = unit.ActiveEffects[i];
            if(!(effect.Data.EffectType.IsDot()))
                continue;
            int damage = effect.Data.TickDamage;
            UnitState previousState = unit.State;
            UnitState resultState = unit.TakeDamage(damage, isDot: true);

            DotTickResult result;
            result.Unit = unit;
            result.Effect = effect.Data;
            result.Damage = damage;
            result.PreviousState = previousState;
            result.ResultState = resultState;
            m_DotResultsBuffer.Add(result);

            if (unit.State == UnitState.Dead)
                break;
        }

        // Dot 지속시간 감소 + 만료제거
        for(int i= unit.ActiveEffects.Count-1; i>=0; --i)
        {
            ActiveStatusEffect effect = unit.ActiveEffects[i];
            if (!(effect.Data.EffectType.IsDot()))
                continue;
            effect.RemainingTurns--;
            if(effect.RemainingTurns <= 0)
                unit.RemoveEffectAt(i);
        }
        unit.RecalculateStats();
        return m_DotResultsBuffer;
    }

    public void ProcessTurnEnd(CombatUnit unit)
    {
        for(int i= unit.ActiveEffects.Count-1; i>=0; --i)
        {
            ActiveStatusEffect effect = unit.ActiveEffects[i];
            if ((effect.Data.EffectType.IsDot()))
                continue;
            if (effect.Data.EffectType == StatusEffectType.Stun)
                continue;
            if (IsPermanent(effect))
                continue;
            effect.RemainingTurns--;
            if (effect.RemainingTurns <= 0)
                unit.RemoveEffectAt(i);
        }
        unit.RecalculateStats();
    }
    public void RemoveStun(CombatUnit unit)
    {
        // 스턴 제거
        for (int i = unit.ActiveEffects.Count-1;i>=0; --i)
        {
            if(unit.ActiveEffects[i].Data.EffectType == StatusEffectType.Stun)
            {
                unit.RemoveEffectAt(i);
                break;
            }
        }

        // Guard 해제
        RemoveEffectByType(unit, StatusEffectType.Guard);

        // 스턴 저항 버프 부여
        ActiveStatusEffect existing = unit.FindEffect(m_StunResistBuff);
        if (existing == null)
            unit.AddEffect(new ActiveStatusEffect(m_StunResistBuff));
        unit.RecalculateStats();

    }
    public void RemoveEffectByType(CombatUnit unit, StatusEffectType type)
    {
        for (int i=unit.ActiveEffects.Count-1; i>=0; --i)
        {
            ActiveStatusEffect effect = unit.ActiveEffects[i];
            if (effect.Data.EffectType == type)
                unit.RemoveEffectAt(i);
        }

        unit.RecalculateStats();
    }
    private bool IsPermanent(ActiveStatusEffect effect)
    {
        return effect.Data.Duration <= -1;
    }


}
