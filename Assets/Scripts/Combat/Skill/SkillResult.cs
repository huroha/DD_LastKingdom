using System.Collections.Generic;
public struct SkillResult
{
    public CombatUnit       User;
    public BaseSkillData    Skill;
    public TargetResult[]   TargetResults;
    public AllyEffectResult[] AllyResults;
    public IReadOnlyList<StatusEffectData> SelfAppliedEffects;
}

public struct AllyEffectResult
{
    public CombatUnit Unit;
    public StatusEffectData[] AppliedEffects;
}
public struct TargetResult
{
    public CombatUnit Target;
    public UnitState PreviousState;
    public bool IsHit;
    public bool IsCrit;
    public bool WasBlocked;
    public int DamageDealt;
    public int HealAmount;
    public int EblaDamageDealt;
    public int EblaHealAmount;
    public int PreviousHp;
    public UnitState ResultState;
    public StatusEffectData[] AppliedEffects;
    public StatusEffectData[] ResistedEffects;
}
