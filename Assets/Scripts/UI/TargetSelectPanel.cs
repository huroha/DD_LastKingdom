using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class TargetSelectPanel : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CombatStateMachine m_CombatStateMachine;

    [Header("Target Buttons")]
    [SerializeField] private Button[]       m_EnemyButtons;   // 4개
    [SerializeField] private Button[]       m_NikkeButtons;   // 4개

    [Header("Unit Names")]
    [SerializeField] private TextMeshProUGUI[] m_EnemyNames;
    [SerializeField] private TextMeshProUGUI[] m_NikkeNames;

    [Header("Cancel Button")]
    [SerializeField] private Button m_CancelButton;


    public delegate void TargetSelectedHandler(CombatUnit target);
    public delegate void CancelHandler();


    private TargetSelectedHandler m_OnTargetSelected;
    private CancelHandler m_OnCancel;

    private List<CombatUnit> m_ValidTargets;

    private void Awake()
    {
        for (int i = 0; i < m_EnemyButtons.Length; ++i)
        {
            int index = i; // 루프 변수를 별도 변수에 캡처
            m_EnemyButtons[i].onClick.AddListener(() =>
            {
                CombatUnit unit = m_CombatStateMachine.PositionSystem.GetUnit(CombatUnitType.Enemy, index);
                OnTargetButtonClicked(unit);
            });
        }
        for (int i=0; i<m_NikkeButtons.Length; ++i)
        {
            int index = i;
            m_NikkeButtons[i].onClick.AddListener(() =>
            {
                CombatUnit unit = m_CombatStateMachine.PositionSystem.GetUnit(CombatUnitType.Nikke, index);
                OnTargetButtonClicked(unit);
            });
        }

        m_CancelButton.onClick.AddListener(OnCancelButtonClicked);
        EventBus.Subscribe<BattleStartedEvent>(OnBattleStarted);
        EventBus.Subscribe<UnitMovedEvent>(OnUnitMoved);
        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        EventBus.Unsubscribe<BattleStartedEvent>(OnBattleStarted);
        EventBus.Unsubscribe<UnitMovedEvent>(OnUnitMoved);
    }

    public void Show(List<CombatUnit> validTargets, TargetSelectedHandler onTargetSelected, CancelHandler onCancel)
    { 
        m_ValidTargets = validTargets;
        m_OnTargetSelected = onTargetSelected;
        m_OnCancel = onCancel;
        RefreshButtons();
        gameObject.SetActive(true);

    }
    public void Hide() 
    {
        for (int i = 0; i < m_NikkeButtons.Length; ++i)
            m_NikkeButtons[i].interactable = false;
        for (int i = 0; i < m_EnemyButtons.Length; ++i)
            m_EnemyButtons[i].interactable = false;
        gameObject.SetActive(false);
    }

    private void RefreshButtons()
    {
        for (int i=0; i< m_EnemyButtons.Length; ++i)
        {
            CombatUnit unit =m_CombatStateMachine.PositionSystem.GetUnit(CombatUnitType.Enemy, i);
            m_EnemyButtons[i].interactable = m_ValidTargets.Contains(unit);
            if (unit != null)
                m_EnemyNames[i].text = unit.UnitName;
        }

        for (int i = 0; i < m_NikkeButtons.Length; ++i)
        {
            CombatUnit unit = m_CombatStateMachine.PositionSystem.GetUnit(CombatUnitType.Nikke, i);
            m_NikkeButtons[i].interactable = m_ValidTargets.Contains(unit);
            if (unit != null)
                m_NikkeNames[i].text = unit.UnitName;
        }
    }
    private void OnTargetButtonClicked(CombatUnit target)
    {
        if (target == null)
            return;

        if (m_OnTargetSelected == null) return;

        m_OnTargetSelected(target);
        Hide();
    }
    private void OnCancelButtonClicked() 
    {
        if (m_OnCancel == null)
            return;  
        m_OnCancel();
        Hide();
    }

    private void OnBattleStarted(BattleStartedEvent e)
    {
        for (int i = 0; i < m_NikkeButtons.Length; ++i)
        {
            m_NikkeButtons[i].gameObject.SetActive(i < e.Nikkes.Count);
            if (i < e.Nikkes.Count)
                m_NikkeNames[i].text = e.Nikkes[i].UnitName;
        }

        for (int i = 0; i < m_EnemyButtons.Length; ++i)
        {
            m_EnemyButtons[i].gameObject.SetActive(i < e.Enemies.Count);
            if (i < e.Enemies.Count)
                m_EnemyNames[i].text = e.Enemies[i].UnitName;
        }
    }



    private void OnUnitMoved(UnitMovedEvent e)
    {
        if (!gameObject.activeSelf)
            return;
        for (int i = 0; i < m_NikkeButtons.Length; ++i)
        {
            CombatUnit unit = m_CombatStateMachine.PositionSystem.GetUnit(CombatUnitType.Nikke, i);
            if (unit != null) m_NikkeNames[i].text = unit.UnitName;
        }
    }
}
