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

    [Header("Cursor")]
    [SerializeField] private Texture2D m_CursorTexture;
    [SerializeField] private Vector2 m_CursorHotspot;

    public GameState CurrentState => m_CurrentState;

    protected override void Awake()
    {
        base.Awake();
        m_CurrentState = GameState.Boot;
        Debug.Log("Awake = [GameManager] Initialized. State : " + m_CurrentState);
        if (m_CursorTexture != null)
            Cursor.SetCursor(m_CursorTexture, m_CursorHotspot, CursorMode.Auto);
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

        if (OnGameStateChanged != null)
            OnGameStateChanged.Invoke(previousState, newState);


    }
}
