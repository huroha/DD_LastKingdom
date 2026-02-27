using UnityEngine;

public enum GameState
{
    Title,
    Town,
    Dungeon,
    Combat
}

public delegate void StateChangedHandler(GameState previous, GameState current);


public class GameManager : Singleton<GameManager>
{
    public event StateChangedHandler OnGameStateChanged;
    private GameState m_CurrentState;
    public GameState CurrentState => m_CurrentState;
    
    protected override void Awake()
    {
        base.Awake();
        m_CurrentState = GameState.Title;
        Debug.Log("[GameManager] Initialized. State : " + m_CurrentState);
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
