
public struct EnemyAction
{
    public EnemySkillData Skill;
    public CombatUnit Target;
    public bool IsPass;


    public EnemyAction(EnemySkillData skill, CombatUnit target)
    {
        Skill = skill;
        Target = target;
        IsPass = false;
    }

    public static EnemyAction Pass
    {
        get { return new EnemyAction { IsPass = true }; }
    }
}

