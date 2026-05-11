using UnityEngine;

enum PlayerCommandKind
{
    None,
    SelectSkill,
    Pass,
    RequestMove,
    SelectTarget,
    Cancel,
}
struct PlayerCommand
{
    public PlayerCommandKind Kind;
    public SkillData Skill;         // null 이면 해당 없음
    public CombatUnit Target;
}
