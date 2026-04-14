
public struct SkillResult
{
    public CombatUnit       User;
    public SkillData        Skill;
    public TargetResult[]   TargetResults;
}


public struct TargetResult
{
    public CombatUnit Target;
    public UnitState PreviousState;
    public bool IsHit;
    public bool IsCrit;
    public int DamageDealt;
    public int HealAmount;
    public int EblaDamageDealt;
    public int EblaHealAmount;
    public int PreviousHp;
    public UnitState ResultState;
    public StatusEffectData[] AppliedEffects;
    public StatusEffectData[] ResistedEffects;
}