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
        gameObject.SetActive(false);
    }

    private void RefreshButtons()
    {
        for (int i=0; i< m_EnemyButtons.Length; ++i)
        {
            CombatUnit unit =m_CombatStateMachine.PositionSystem.GetUnit(CombatUnitType.Enemy, i);
            m_EnemyButtons[i].interactable = m_ValidTargets.Contains(unit);
        }

        for (int i = 0; i < m_NikkeButtons.Length; ++i)
        {
            CombatUnit unit = m_CombatStateMachine.PositionSystem.GetUnit(CombatUnitType.Nikke, i);
            m_NikkeButtons[i].interactable = m_ValidTargets.Contains(unit);
        }
    }
    private void OnTargetButtonClicked(CombatUnit target)
    {
        if (target == null)
            return;
        m_OnTargetSelected(target);
        Hide();
    }
    private void OnCancelButtonClicked() 
    {
        m_OnCancel();
        Hide();
    }
}
