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
    





    public StatusEffectManager(StatusEffectData stunResistBuff)
    {
        m_StunResistBuff = stunResistBuff;
    }

    public List<DotTickResult> ProcessTurnStart(CombatUnit unit) 
    {
        List<DotTickResult> results = new List<DotTickResult>();
        
        if(!unit.IsAlive)
            return results;
        for(int i=0; i<unit.ActiveEffects.Count; ++i)
        {
            ActiveStatusEffect effect = unit.ActiveEffects[i];
            if(!IsDotEffect(effect.Data.EffectType))
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
            results.Add(result);

            if (unit.State == UnitState.Dead)
                break;
        }

        // Dot 지속시간 감소 + 만료제거
        for(int i= unit.ActiveEffects.Count-1; i>=0; --i)
        {
            ActiveStatusEffect effect = unit.ActiveEffects[i];
            if (!IsDotEffect(effect.Data.EffectType))
                continue;
            effect.RemainingTurns--;
            if(effect.RemainingTurns <= 0)
                unit.ActiveEffects.RemoveAt(i);
        }
        unit.RecalculateStats();
        return results;
    }

    public void ProcessTurnEnd(CombatUnit unit)
    {
        for(int i= unit.ActiveEffects.Count-1; i>=0; --i)
        {
            ActiveStatusEffect effect = unit.ActiveEffects[i];
            if (IsDotEffect(effect.Data.EffectType))
                continue;
            if (effect.Data.EffectType == StatusEffectType.Stun)
                continue;
            if (IsPermanent(effect))
                continue;
            effect.RemainingTurns--;
            if (effect.RemainingTurns <= 0)
                unit.ActiveEffects.RemoveAt(i);
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
                unit.ActiveEffects.RemoveAt(i);
                break;
            }
        }

        // Guard 해제
        RemoveEffectByType(unit, StatusEffectType.Guard);

        // 스턴 저항 버프 부여
        ActiveStatusEffect existing = null;
        for(int i=0; i< unit.ActiveEffects.Count; ++i)
        {
            if (unit.ActiveEffects[i].Data == m_StunResistBuff)
            {
                existing = unit.ActiveEffects[i];
                break;
            }
        }
        if (existing == null)
            unit.ActiveEffects.Add(new ActiveStatusEffect(m_StunResistBuff));
        unit.RecalculateStats();

    }
    public void RemoveEffectByType(CombatUnit unit, StatusEffectType type)
    {
        for (int i=unit.ActiveEffects.Count-1; i>=0; --i)
        {
            ActiveStatusEffect effect = unit.ActiveEffects[i];
            if (effect.Data.EffectType == type)
                unit.ActiveEffects.RemoveAt(i);
        }

        unit.RecalculateStats();
    }
    private bool IsDotEffect(StatusEffectType type)
    {
        return type == StatusEffectType.Poison || type == StatusEffectType.Bleed;
    }
    private bool IsPermanent(ActiveStatusEffect effect)
    {
        return effect.Data.Duration <= -1;
    }


}
