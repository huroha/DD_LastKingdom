using UnityEngine;
using System.Collections.Generic;

public class EnemyAI
{
    private PositionSystem m_PositionSystem;
    private SkillExecutor m_SkillExecutor;

    private List<SkillData> m_ValidSkillsBuffer = new List<SkillData>();
    private List<CombatUnit> m_TargetsBuffer = new List<CombatUnit>();

    public EnemyAI(PositionSystem positionSystem, SkillExecutor skillExecutor)
    {
        m_PositionSystem = positionSystem;
        m_SkillExecutor = skillExecutor;
    }
    public EnemyAction DecideAction(CombatUnit enemy)
    {
        m_ValidSkillsBuffer.Clear();
        List<SkillData> validSkills = m_ValidSkillsBuffer;
        //  1. enemy.Skills를 순회하며 ValidateSkill이 true인 것만 validSkills 리스트에 수집
        for (int i=0; i<enemy.Skills.Count; ++i)
        {
            SkillData skill = enemy.Skills[i];
            if(skill != null && m_SkillExecutor.ValidateSkill(enemy, skill))
                validSkills.Add(skill);
        }
        //  2. validSkills가 비어있으면 EnemyAction.Pass 반환
        // 사용 가능 스킬이 없음
        if (validSkills.Count == 0)
            return EnemyAction.Pass;

        //  3. 랜덤 시작 인덱스(Random.Range) 뽑기
        // 어떤 스킬 쓸건지에 대한 랜덤값
        int startIndex = Random.Range(0, validSkills.Count);

        //  4. 시작 인덱스부터 순환 순회로 스킬 사용
        for ( int i =0; i < validSkills.Count; ++i)
        {
            int index = (startIndex +i) % validSkills.Count;
            SkillData skill = validSkills[index];

            if (skill.TargetType == TargetType.EnemySingle || skill.TargetType == TargetType.AllySingle)
            {
                m_PositionSystem.GetValidTargets(enemy, skill,m_TargetsBuffer);
                if (m_TargetsBuffer.Count == 0)
                    continue;

                CombatUnit target = m_TargetsBuffer[Random.Range(0, m_TargetsBuffer.Count)];
                return new EnemyAction(skill, target);
            }
            else
                return new EnemyAction(skill, null);
        }

        //5.루프 끝까지 성공 못 하면 EnemyAction.Pass 반환
        return EnemyAction.Pass;

    }
}
