using UnityEngine;
using System.Collections.Generic;


public class TurnManager
{
    private List<CombatUnit>    m_AllUnits;
    private List<CombatUnit>    m_TurnOrder;
    private int                 m_CurrentIndex;
    private int                 m_RoundNumber;

    private List<CombatUnit> m_PhaseBuffer = new List<CombatUnit>();

    public CombatUnit                   CurrentUnit => m_TurnOrder[m_CurrentIndex];
    public int                          RoundNumber => m_RoundNumber;
    public IReadOnlyList<CombatUnit>    TurnOrder => m_TurnOrder;
    public int CurrentTurnIndex => m_CurrentIndex;

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

        for (int i = 0; i < m_AllUnits.Count; ++i)
            if (m_AllUnits[i].IsAlive)
                m_AllUnits[i].TurnOrderTieBreaker = Random.value;

        int maxActions = 1;
        for (int i = 0; i < m_AllUnits.Count; ++i)
            if (m_AllUnits[i].IsAlive && m_AllUnits[i].ActionsPerRound > maxActions)
                maxActions = m_AllUnits[i].ActionsPerRound;

  
        for (int action = 1; action <= maxActions; ++action)
        {
            m_PhaseBuffer.Clear();
            for (int i = 0; i < m_AllUnits.Count; ++i)
                if (m_AllUnits[i].IsAlive && m_AllUnits[i].ActionsPerRound >= action)
                    m_PhaseBuffer.Add(m_AllUnits[i]);

            m_PhaseBuffer.Sort(CompareBySPD);
            for (int i = 0; i < m_PhaseBuffer.Count; ++i)
                m_TurnOrder.Add(m_PhaseBuffer[i]);
        }
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

        // 같은 니케나 적이 같은 속도라면 TieBreaker사용
        return a.TurnOrderTieBreaker.CompareTo(b.TurnOrderTieBreaker);
    }

    
}