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
    private GameState m_currentState;
    public GameState CurrentState => m_currentState;
    
    protected override void Awake()
    {
        base.Awake();
        m_currentState = GameState.Title;
        Debug.Log("[GameManager] Initialized. State : " + m_currentState);
    }

    public void ChangeState(GameState newState)
    {
        if (m_currentState == newState)
        {
            Debug.LogWarning("[GameManager] ChangeState called same State : " + newState);
            return;
        }

        GameState previousState = m_currentState;
        m_currentState = newState;

        Debug.Log("[GameManager] Staged Change " + previousState + "->" + newState);
        
        if (OnGameStateChanged != null)
            OnGameStateChanged.Invoke(previousState, newState);


    }
}
