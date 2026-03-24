using UnityEngine;

public class EblaSystem
{
    private StatusEffectData m_AfflictionDebuff;
    public EblaSystem(StatusEffectData afflictionDebuff)
    {
        m_AfflictionDebuff = afflictionDebuff;
    }

    // 반환값 : true = 200 도달로 사망
    public bool ModifyEbla(CombatUnit unit, int amount)
    {
        if (unit.UnitType != CombatUnitType.Nikke || !unit.IsAlive)
            return false;
        int previousEbla = unit.Ebla;
        if (amount > 0)
        {
            amount = (int)(amount * (1f + unit.CurrentStats.eblaMultiplier / 100f));
            if (previousEbla < 100 && previousEbla + amount > 100)
                amount = 100 - previousEbla;
        }
        unit.AddEbla(amount);
        if(unit.Ebla >= 200)
        {
            unit.Kill();
            EventBus.Publish(new PermanentDeathEvent(unit));
            return true;
        }
        if(previousEbla < 100 && unit.Ebla >= 100)
            TriggerAffliction(unit);
        else if(previousEbla >= 100 && unit.Ebla < 100)
            RemoveAffliction(unit);

        return false;
    }

    private void TriggerAffliction(CombatUnit unit)
    {
        unit.SetEblaState(EblaState.Afflicted);
        unit.ActiveEffects.Add(new ActiveStatusEffect(m_AfflictionDebuff));
        unit.RecalculateStats();
        EventBus.Publish(new AfflictionTriggeredEvent(unit));
    }

    private void RemoveAffliction(CombatUnit unit)
    {
        for (int i = unit.ActiveEffects.Count - 1; i >= 0; --i)
        {
            if (unit.ActiveEffects[i].Data == m_AfflictionDebuff)
            {
                unit.ActiveEffects.RemoveAt(i);
                break;
            }
        }
        unit.SetEblaState(EblaState.Normal);
        unit.RecalculateStats();
    }
}
