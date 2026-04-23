using UnityEngine;

public class CombatSlotPositionController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CombatStateMachine m_CombatStateMachine;
    [SerializeField] private CombatFieldView m_FieldView;
    [SerializeField] private Camera m_Camera;
    [SerializeField] private CombatTurnBarDisplay m_TurnBarDisplay;

    [Header("Slot Roots")]
    [SerializeField] private RectTransform[] m_NikkeSlotRoots;
    [SerializeField] private RectTransform[] m_EnemySlotRoots;
    [SerializeField] private RectTransform[] m_LargeEnemySlotRoots;

    private Vector3[] m_OriginalNikkeSlotPositions;
    private Vector3[] m_OriginalEnemySlotPositions;
    private Vector3[] m_OriginalLargeEnemySlotPositions;

    private void Awake()
    {
        m_OriginalNikkeSlotPositions = new Vector3[m_NikkeSlotRoots.Length];
        for (int i = 0; i < m_NikkeSlotRoots.Length; ++i)
            m_OriginalNikkeSlotPositions[i] = m_NikkeSlotRoots[i].position;

        m_OriginalEnemySlotPositions = new Vector3[m_EnemySlotRoots.Length];
        for (int i = 0; i < m_EnemySlotRoots.Length; ++i)
            m_OriginalEnemySlotPositions[i] = m_EnemySlotRoots[i].position;

        m_OriginalLargeEnemySlotPositions = new Vector3[m_LargeEnemySlotRoots.Length];
        for (int i = 0; i < m_LargeEnemySlotRoots.Length; ++i)
            m_OriginalLargeEnemySlotPositions[i] = m_LargeEnemySlotRoots[i].position;
    }

    public void UpdateSlotPositionsForTilt()
    {
        for (int i = 0; i < m_NikkeSlotRoots.Length; ++i)
        {
            CombatUnit unit = m_CombatStateMachine.PositionSystem.GetUnit(CombatUnitType.Nikke, i);
            if (unit == null)
                continue;
            Vector3 worldPos = m_FieldView.GetSlotPosition(unit);
            Vector3 screenPos = m_Camera.WorldToScreenPoint(worldPos);
            m_NikkeSlotRoots[i].position = screenPos;
        }

        for (int i = 0; i < m_EnemySlotRoots.Length; ++i)
        {
            CombatUnit unit = m_CombatStateMachine.PositionSystem.GetUnit(CombatUnitType.Enemy, i);
            if (unit == null)
                continue;
            if (unit.SlotSize == 2)
                continue;
            Vector3 worldPos = m_FieldView.GetSlotPosition(unit);
            Vector3 screenPos = m_Camera.WorldToScreenPoint(worldPos);
            m_EnemySlotRoots[i].position = screenPos;
        }

        for (int i = 0; i < m_LargeEnemySlotRoots.Length; ++i)
        {
            CombatUnit unit = m_CombatStateMachine.PositionSystem.GetUnit(CombatUnitType.Enemy, i);
            if (unit == null || unit.SlotSize != 2 || unit.SlotIndex != i)
                continue;
            Vector3 worldPos = m_FieldView.GetSlotPosition(unit);
            Vector3 screenPos = m_Camera.WorldToScreenPoint(worldPos);
            m_LargeEnemySlotRoots[i].position = screenPos;
        }
        m_TurnBarDisplay.Refresh();
    }

    public void ResetSlotPositions()
    {
        for (int i = 0; i < m_NikkeSlotRoots.Length; ++i)
            m_NikkeSlotRoots[i].position = m_OriginalNikkeSlotPositions[i];

        for (int i = 0; i < m_EnemySlotRoots.Length; ++i)
            m_EnemySlotRoots[i].position = m_OriginalEnemySlotPositions[i];

        for (int i = 0; i < m_LargeEnemySlotRoots.Length; ++i)
            m_LargeEnemySlotRoots[i].position = m_OriginalLargeEnemySlotPositions[i];
        m_TurnBarDisplay.Refresh();
    }
}
