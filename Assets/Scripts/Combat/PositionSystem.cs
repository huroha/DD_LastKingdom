using UnityEngine;
using System.Collections.Generic;

public class PositionSystem
{
    private CombatUnit[] m_NikkeSlots;
    private CombatUnit[] m_EnemySlots;



    // 초기화
    public void Initialize(List<CombatUnit> nikkes, List<CombatUnit> enemies)
    {
        m_NikkeSlots = new CombatUnit[nikkes.Count];
        m_EnemySlots = new CombatUnit[4]; 


        for(int i=0; i<nikkes.Count; ++i)
        {
            m_NikkeSlots[nikkes[i].SlotIndex] = nikkes[i];
        }

        for (int i=0; i< enemies.Count; ++i)
        {
            CombatUnit unit = enemies[i];
            m_EnemySlots[unit.SlotIndex] = unit;
            if (unit.SlotSize == 2)
                m_EnemySlots[unit.SlotIndex + 1] = unit;
        }
    }

    // 조회
    public CombatUnit GetUnit(CombatUnitType team, int slotIndex)
    {
        CombatUnit[] slots = GetTeamSlots(team);

        if (slotIndex < 0 || slotIndex >= slots.Length)
            return null;
        return slots[slotIndex];
    }

    public List<CombatUnit> GetAllUnits(CombatUnitType team)
    {
        List<CombatUnit> units = new List<CombatUnit>();
        CombatUnit[] slots = GetTeamSlots(team);
        for (int i=0; i< slots.Length; ++i)
        {
            if (slots[i] != null && slots[i].IsAlive)
                units.Add(slots[i]);
        }
        return units;
    }

    public List<CombatUnit> GetCorpses(CombatUnitType team)
    {
        List<CombatUnit> corpses = new List<CombatUnit>();
        CombatUnit[] slots = GetTeamSlots(team);
        for (int i = 0; i < slots.Length; ++i)
        {
            if (slots[i] != null && slots[i].State == UnitState.Corpse)
            {
                corpses.Add(slots[i]);
            }
        }
        return corpses;
    }

    // 스킬 판정
    public bool CanUseSkill(CombatUnit user, SkillData skill)
    {
        int index = user.SlotIndex;
        if (index >= skill.UsablePositions.Count)
            return false;
        return skill.UsablePositions[index];
    }

    public List<CombatUnit> GetValidTargets(CombatUnit user, SkillData skill)
    {
        List<CombatUnit> result = new List<CombatUnit>();

        if(skill.TargetType == TargetType.Self)
        {
            result.Add(user);
            return result;
        }

        // All 타입 : TargetPosition 무시하고 해당 진영 전체 반환
        if(skill.TargetType == TargetType.EnemyAll)
        {
            CombatUnitType enemyType = (user.UnitType == CombatUnitType.Nikke) ? CombatUnitType.Enemy : CombatUnitType.Nikke;
            return GetAllUnits(enemyType);
        }
        if (skill.TargetType == TargetType.AllyAll)
            return GetAllUnits(user.UnitType);

        CombatUnit[] targetSlots = GetTargetSlots(user, skill.TargetType);

        for(int i=0; i< targetSlots.Length; ++i)
        {
            if (i >= skill.TargetPositions.Count) break; // 범위 초과 접근 방지 기본적으로 4개지만 inspector에서 실수로줄일수있음
            if (!skill.TargetPositions[i]) continue;    // 스킬 타겟팅 검사

            CombatUnit target = targetSlots[i];
            if(target != null && target.State != UnitState.Dead && !result.Contains(target))
                result.Add(target);
        }
        return result;
    }

    public bool Swap(CombatUnit a, CombatUnit b)
    {
        if (a.UnitType != b.UnitType)
            return false;

        CombatUnit[] slots = GetTeamSlots(a.UnitType);
        int indexA = a.SlotIndex;
        int indexB = b.SlotIndex;

        if (indexA < 0 || indexA >= slots.Length)
            return false;
        if (indexB < 0 || indexB >= slots.Length)
            return false;

        slots[indexA] = b;
        slots[indexB] = a;

        int temp = a.SlotIndex;
        a.SlotIndex = b.SlotIndex;
        b.SlotIndex = temp;

        return true;
    }

    public bool Move(CombatUnit unit, int steps)
    {
        // 양수 = 후진, 음수 = 전진

        if (steps == 0)
            return false;

        CombatUnit[] slots = GetTeamSlots(unit.UnitType);
        int lastOccupied = 0;
        for (int i = slots.Length - 1; i >= 0; --i)
        {
            if (slots[i] != null)
            {
                lastOccupied = i;
                break;
            }
        }

        if (unit.SlotSize == 1)
        {

            int oldIndex = unit.SlotIndex;
            int newIndex = Mathf.Clamp(oldIndex + steps, 0, lastOccupied);

            if (newIndex == oldIndex)
                return false;
            if (newIndex > oldIndex)
            {
                for (int i = oldIndex; i < newIndex; ++i)
                {
                    slots[i] = slots[i + 1];
                    if (slots[i] != null && (i == 0 || slots[i - 1] != slots[i]))
                        slots[i].SlotIndex = i;
                }
            }
            else
            {
                for (int i = oldIndex; i > newIndex; --i)
                {
                    slots[i] = slots[i - 1];
                    if (slots[i] != null)
                        slots[i].SlotIndex = i;
                }
            }

            slots[newIndex] = unit;
            unit.SlotIndex = newIndex;
            return true;
        }
        else
        {
            return MoveLargeUnit(unit, steps, slots);
        }


    }
    private bool MoveLargeUnit(CombatUnit unit, int steps, CombatUnit[] slots)
    {
        int oldIndex = unit.SlotIndex;
        int size = unit.SlotSize;
        int newIndex = oldIndex + steps;

        if (newIndex < 0 || newIndex + size - 1 >= slots.Length)
            return false;

        int numDisplaced = Mathf.Abs(steps);
        CombatUnit[] displaced = new CombatUnit[numDisplaced];
        int[] targetSlots = new int[numDisplaced];

        for (int k = 0; k < numDisplaced; ++k)
        {
            int neededSlot = steps < 0 ? newIndex + k : oldIndex + size + k;
            int freedSlot = steps < 0 ? newIndex + size + k : oldIndex + k;
            displaced[k] = slots[neededSlot];
            targetSlots[k] = freedSlot;
        }

        // size-2 displaced 유닛이 freed 슬롯보다 크면 → 이동 확장 (스왑)
        for (int k = 0; k < numDisplaced; ++k)
        {
            if (displaced[k] == null || displaced[k].SlotSize <= numDisplaced) continue;
            bool first = true;
            for (int j = 0; j < k; ++j)
                if (displaced[j] == displaced[k]) { first = false; break; }
            if (!first) continue;

            int extendedSteps = steps < 0 ? -displaced[k].SlotSize : displaced[k].SlotSize;
            int extendedNewIndex = oldIndex + extendedSteps;
            if (extendedNewIndex < 0 || extendedNewIndex + size - 1 >= slots.Length)
                return false;
            return MoveLargeUnit(unit, extendedSteps, slots);
        }

        // unit 기존 슬롯 비우기
        for (int i = oldIndex; i < oldIndex + size; ++i)
            slots[i] = null;

        // displaced 유닛들 기존 슬롯 비우기 (size-2 대응)
        for (int k = 0; k < numDisplaced; ++k)
        {
            if (displaced[k] == null) continue;
            bool first = true;
            for (int j = 0; j < k; ++j)
                if (displaced[j] == displaced[k]) { first = false; break; }
            if (!first) continue;
            for (int s = 0; s < displaced[k].SlotSize; ++s)
                slots[displaced[k].SlotIndex + s] = null;
        }

        // displaced 유닛들 새 슬롯에 배치
        for (int k = 0; k < numDisplaced; ++k)
        {
            if (displaced[k] == null) continue;
            bool first = true;
            for (int j = 0; j < k; ++j)
                if (displaced[j] == displaced[k]) { first = false; break; }
            if (!first) continue;
            displaced[k].SlotIndex = targetSlots[k];
            for (int s = 0; s < displaced[k].SlotSize; ++s)
                slots[targetSlots[k] + s] = displaced[k];
        }

        // unit 새 슬롯에 배치
        for (int i = newIndex; i < newIndex + size; ++i)
            slots[i] = unit;
        unit.SlotIndex = newIndex;

        return true;

    }
    public void RemoveUnit(CombatUnit unit)
    {
        CombatUnit[] slots = GetTeamSlots(unit.UnitType);
        int index = unit.SlotIndex;
        int size = unit.SlotSize;

        if (index < 0 || index >= slots.Length)
            return;

        for(int i= index; i< slots.Length - size; ++i)
        {
            slots[i] = slots[i + size];
            if (slots[i] != null)
                slots[i].SlotIndex = i;
        }
        
        for (int i= slots.Length - size; i<slots.Length; ++i)
            slots[i] = null;
    }

    private CombatUnit[] GetTeamSlots(CombatUnitType team)
    {
        if (team == CombatUnitType.Nikke)
            return m_NikkeSlots;
        else
            return m_EnemySlots;
    }

    private CombatUnit[] GetTargetSlots(CombatUnit user, TargetType targetType)
    {
        // 사용자의 기술타입이 Enemy가 들어있으면 true 버프기면 false가 된다
        bool targetEnemy = targetType == TargetType.EnemySingle || targetType == TargetType.EnemyAll 
                         || targetType == TargetType.EnemyMulti;

        // targetEnemy는 무조건 true 혹은 false니 그게 어떤 타입이 사용했는지만 알면됨
        if (user.UnitType == CombatUnitType.Nikke)
            return targetEnemy ? m_EnemySlots : m_NikkeSlots;  
        else
            return targetEnemy ? m_NikkeSlots : m_EnemySlots;
    }




}