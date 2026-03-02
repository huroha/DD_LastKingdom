using UnityEngine;
using System.Collections.Generic;


public class TurnManager
{
    private List<CombatUnit>    m_AllUnits;
    private List<CombatUnit>    m_TurnOrder;
    private int                 m_CurrentIndex;
    private int                 m_RoundNumber;

    public CombatUnit                   CurrentUnit => m_TurnOrder[m_CurrentIndex];
    public int                          RoundNumber => m_RoundNumber;
    public IReadOnlyList<CombatUnit>    TurnOrder => m_TurnOrder;

    public void Initialize(List<CombatUnit> allUnits)
    {
        m_AllUnits = allUnits;
        m_TurnOrder = new List<CombatUnit>();
        m_RoundNumber = 0;
        StartNextRound();
    }

    public CombatUnit StartNextTurn()
    {
        AdvanceToNextAliveUnit();

        if(m_CurrentIndex >= m_TurnOrder.Count)
        {
            EventBus.Publish(new RoundEndedEvent(m_RoundNumber));
            StartNextRound();
            AdvanceToNextAliveUnit();

            if (m_CurrentIndex >= m_TurnOrder.Count)
                return null;
        }
        CombatUnit current = m_TurnOrder[m_CurrentIndex];
        EventBus.Publish(new TurnStartedEvent(current));
        return current;
    }

    public void EndCurrentTurn()
    {
        EventBus.Publish(new TurnEndedEvent(m_TurnOrder[m_CurrentIndex]));
        ++m_CurrentIndex;
    }
    
    private void StartNextRound()
    {
        ++m_RoundNumber;
        BuildTurnOrder();
        m_CurrentIndex = 0;
        EventBus.Publish(new RoundStartedEvent(m_RoundNumber));
    }

    private void BuildTurnOrder()
    {
        m_TurnOrder.Clear();
        for(int i =0; i< m_AllUnits.Count; ++i)
        {
            if (m_AllUnits[i].IsAlive)
            {
                m_TurnOrder.Add(m_AllUnits[i]);
            }
        }
        m_TurnOrder.Sort(CompareBySPD);     // List.Sort()에 넘기는 비교함수는 반환값에 의미가 정해져있음
                                            // 음수 -> a를 b보다 앞에 배치 0-> 그대로 유지, 양수 -> b를 a보다 앞에
    }

    private void AdvanceToNextAliveUnit()
    {
        while (m_CurrentIndex < m_TurnOrder.Count && !m_TurnOrder[m_CurrentIndex].IsAlive)
            ++m_CurrentIndex;
    }

    private static int CompareBySPD(CombatUnit a, CombatUnit b)
    {
        int spd = b.CurrentStats.speed.CompareTo(a.CurrentStats.speed);     // 내림차순 배치다. 
        if (spd != 0)
            return spd;

        if (a.UnitType == CombatUnitType.Nikke && b.UnitType == CombatUnitType.Enemy) return -1;
        if (a.UnitType == CombatUnitType.Enemy && b.UnitType == CombatUnitType.Nikke) return 1;

        // 같은 니케나 적이 같은 속도라면 랜덤 선택
        return Random.Range(0,2) == 0 ? -1 : 1;
    }

}