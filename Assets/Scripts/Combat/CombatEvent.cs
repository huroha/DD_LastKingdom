// Combat 관련 이벤트 타입들 정의
using System.Collections.Generic;

public struct RoundStartedEvent
{
    public int Round;
    public RoundStartedEvent(int round) { Round = round; }
}

public struct RoundEndedEvent
{
    public int Round;
    public RoundEndedEvent(int round) { Round = round; }
}

public struct TurnStartedEvent
{
    public CombatUnit Unit;
    public TurnStartedEvent(CombatUnit unit) { Unit = unit; }
}

public struct TurnEndedEvent
{
    public CombatUnit Unit;
    public TurnEndedEvent(CombatUnit unit) { Unit = unit; }
}

public struct UnitDiedEvent
{
    public CombatUnit Unit;
    public UnitDiedEvent(CombatUnit unit) { Unit = unit; }
}

public struct BattleStartedEvent
{
    public List<CombatUnit> Nikkes;
    public List<CombatUnit> Enemies;

    public BattleStartedEvent(List<CombatUnit> nikkes, List<CombatUnit> enemies)
    {
        Nikkes = nikkes;
        Enemies = enemies;
    }
}

public struct BattleEndedEvent
{
    public bool IsVictory;

    public BattleEndedEvent(bool victory)
    {
        IsVictory = victory;    
    }
}

public struct SkillExecutedEvent
{
    public SkillResult Result;

    public SkillExecutedEvent(SkillResult skillresult)
    {
        Result = skillresult;
    }
}

public struct UnitStateChangedEvent
{
    public CombatUnit Unit;
    public UnitState OldState;
    public UnitState NewState;

    public UnitStateChangedEvent(CombatUnit unit, UnitState oldState, UnitState newState)
    {
        Unit = unit;
        OldState = oldState;
        NewState = newState;
    }
}

public struct UnitMovedEvent
{
    public CombatUnit UnitA;
    public CombatUnit UnitB;
    public UnitMovedEvent(CombatUnit a, CombatUnit b) { UnitA = a; UnitB = b; }

}

public struct AfflictionTriggeredEvent
{
    public CombatUnit Unit;
    public AfflictionTriggeredEvent(CombatUnit unit) { Unit = unit; }
}

public struct PermanentDeathEvent
{
    public CombatUnit Unit;
    public PermanentDeathEvent(CombatUnit unit) { Unit = unit; }
}
