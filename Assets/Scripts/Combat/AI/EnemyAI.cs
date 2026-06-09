using UnityEngine;
using System.Collections.Generic;

public class EnemyAI
{
    private PositionSystem m_PositionSystem;
    private SkillExecutor m_SkillExecutor;

    private List<EnemySkillData> m_ValidSkillsBuffer = new List<EnemySkillData>();
    private List<CombatUnit> m_TargetsBuffer = new List<CombatUnit>();
    private List<int> m_ValidIndexBuffer = new List<int>();

    public EnemyAI(PositionSystem positionSystem, SkillExecutor skillExecutor)
    {
        m_PositionSystem = positionSystem;
        m_SkillExecutor = skillExecutor;
    }
    public EnemyAction DecideAction(CombatUnit enemy)
    {
        m_ValidSkillsBuffer.Clear();
        m_ValidIndexBuffer.Clear();
        List<EnemySkillData> validSkills = m_ValidSkillsBuffer;

        // 1. 유효 + 쿨다운 아닌 스킬 수집 (원본 인덱스 같이 보관)
        for (int i = 0; i < enemy.EnemySkills.Count; ++i)
        {
            EnemySkillData skill = enemy.EnemySkills[i];
            if (skill != null && !enemy.IsSkillOnCooldown(i) && m_SkillExecutor.ValidateSkill(enemy, skill))
            {
                validSkills.Add(skill);
                m_ValidIndexBuffer.Add(i);
            }
        }

        // 2. 사용 가능 스킬 없음
        if (validSkills.Count == 0)
            return EnemyAction.Pass;

        // 3. 랜덤 시작 인덱스
        int startIndex = Random.Range(0, validSkills.Count);

        // 4. 순환 순회로 스킬 사용
        for (int i = 0; i < validSkills.Count; ++i)
        {
            int index = (startIndex + i) % validSkills.Count;
            EnemySkillData skill = validSkills[index];
            int originalIndex = m_ValidIndexBuffer[index];

            if (skill.TargetType == TargetType.EnemySingle || skill.TargetType == TargetType.AllySingle)
            {
                m_PositionSystem.GetValidTargets(enemy, skill, m_TargetsBuffer);
                if (m_TargetsBuffer.Count == 0)
                    continue;

                CombatUnit target = m_TargetsBuffer[Random.Range(0, m_TargetsBuffer.Count)];
                enemy.SetSkillCooldown(originalIndex, skill.Cooldown);
                return new EnemyAction(skill, target);
            }
            else
            {
                enemy.SetSkillCooldown(originalIndex, skill.Cooldown);
                return new EnemyAction(skill, null);
            }
        }

        // 5. 끝까지 실패 시 Pass
        return EnemyAction.Pass;
    }
}
