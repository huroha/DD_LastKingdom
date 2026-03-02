// Combat 관련 이벤트 타입들 정의

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