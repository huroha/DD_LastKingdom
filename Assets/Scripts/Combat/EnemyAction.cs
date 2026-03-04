
public struct EnemyAction
{
    public SkillData Skill;
    public CombatUnit Target;
    public bool IsPass;


    public EnemyAction(SkillData skill, CombatUnit target)
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

