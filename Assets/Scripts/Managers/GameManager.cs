using UnityEngine;

public enum GameState
{
    Boot,
    Title,
    Town,
    Dungeon,
    Combat
}

public delegate void StateChangedHandler(GameState previous, GameState current);

// 게임 전체 흐름 상태를 관리하는 싱글톤 매니저 (Title/ Town / Dungeon / Combat)
public class GameManager : Singleton<GameManager>
{
    public event StateChangedHandler OnGameStateChanged;
    private GameState m_CurrentState;
    public GameState CurrentState => m_CurrentState;

    // 임시용
    private int m_Credit;
    private int m_BattleData;
    private int m_Core;
    private int m_Gems;
    private int[] m_Relics = new int[4];

    public int Credit => m_Credit;
    public int BattleData => m_BattleData;
    public int Core => m_Core;
    public int Gems => m_Gems;
    public int[] Relics => m_Relics;

    public void AddCredit(int amount) { m_Credit += amount; }
    public void AddBattleData(int amount) { m_BattleData += amount; }
    public void AddCore(int amount) { m_Core += amount; }
    public void AddGems(int amount) { m_Gems += amount; }

    public void AddRelics(RelicType type, int amount)
    {
        m_Relics[(int)type] += amount;
    }

    protected override void Awake()
    {
        base.Awake();
        m_CurrentState = GameState.Boot;
        Debug.Log("Awake  = [GameManager] Initialized. State : " + m_CurrentState);
    }

    public void ChangeState(GameState newState)
    {
        if (m_CurrentState == newState)
        {
            Debug.LogWarning("[GameManager] ChangeState called same State : " + newState);
            return;
        }

        GameState previousState = m_CurrentState;
        m_CurrentState = newState;

        Debug.Log("[GameManager] Staged Change " + previousState + "->" + newState);
        
        if (OnGameStateChanged != null)
            OnGameStateChanged.Invoke(previousState, newState);


    }
}
