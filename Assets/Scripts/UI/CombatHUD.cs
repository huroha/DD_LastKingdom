using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class CombatHUD : MonoBehaviour
{
    [Header("Nikke Slots")]
    [SerializeField] private Slider[]           m_NikkeHpBars;    // 4개
    [SerializeField] private Slider[]           m_NikkeEblaBars;    // 4개
    [SerializeField] private TextMeshProUGUI[]  m_NikkeNames;    // 4개

    [Header("Enemy Slots")]
    [SerializeField] private Slider[]           m_EnemyHpBars;  // 4개
    [SerializeField] private TextMeshProUGUI[]  m_EnemyNames;   // 4개

    [Header("Turn Order")]
    [SerializeField] private TextMeshProUGUI m_TurnOrderText;


    [Header("References")]
    [SerializeField] private CombatStateMachine m_CombatStateMachine;

    private System.Text.StringBuilder m_TurnOrderBuilder = new System.Text.StringBuilder(128);

    private void Awake()
    {
        Initialize(m_CombatStateMachine);
    }

    private void OnEnable()
    {
        EventBus.Subscribe<BattleStartedEvent>(OnBattleStarted);
        EventBus.Subscribe<SkillExecutedEvent>(OnSkillExecuted);
        EventBus.Subscribe<UnitDiedEvent>(OnUnitDied);
        EventBus.Subscribe<UnitMovedEvent>(OnUnitMoved);

    }
    private void OnDisable()
    {
        EventBus.Unsubscribe<BattleStartedEvent>(OnBattleStarted);
        EventBus.Unsubscribe<SkillExecutedEvent>(OnSkillExecuted);
        EventBus.Unsubscribe<UnitDiedEvent>(OnUnitDied);
        EventBus.Unsubscribe<UnitMovedEvent>(OnUnitMoved);
    }
    public void Initialize(CombatStateMachine stateMachine)
    {
        if (stateMachine == null)
            return;
        m_CombatStateMachine = stateMachine;
    }

    private void OnBattleStarted(BattleStartedEvent e)
    {
        // 전체 숨기기
        for (int i=0; i<m_NikkeHpBars.Length; ++i)
        {
            m_NikkeHpBars[i].gameObject.SetActive(false);
            m_NikkeEblaBars[i].gameObject.SetActive(false);
            m_NikkeNames[i].gameObject.SetActive(false);
        }
        for (int i=0; i<m_EnemyHpBars.Length; ++i)
        {
            m_EnemyHpBars[i].gameObject.SetActive(false);
            m_EnemyNames[i].gameObject.SetActive(false);
        }

        // 데이터 있는 슬롯만 표시
        for (int i = 0; i < e.Nikkes.Count; ++i)
        {
            m_NikkeHpBars[i].gameObject.SetActive(true);
            m_NikkeEblaBars[i].gameObject.SetActive(true);
            m_NikkeNames[i].gameObject.SetActive(true);
            m_NikkeNames[i].text = e.Nikkes[i].UnitName;
            RefreshHpBar(e.Nikkes[i]);
        }

        for (int i=0; i< e.Enemies.Count; ++i)
        {
            m_EnemyHpBars[i].gameObject.SetActive(true);
            m_EnemyNames[i].gameObject.SetActive(true);
            m_EnemyNames[i].text = e.Enemies[i].UnitName;
            RefreshHpBar(e.Enemies[i]);
        }
        RefreshTurnOrder();
    }

    private void OnSkillExecuted(SkillExecutedEvent e)
    {
        if (e.Result.TargetResults == null)
            return;
        for (int i=0; i<e.Result.TargetResults.Length; ++i)
        {
            TargetResult result = e.Result.TargetResults[i];
            RefreshHpBar(result.Target);
        }
        RefreshTurnOrder();
    }

    private  void OnUnitDied(UnitDiedEvent e)
    {
        int index = e.Unit.SlotIndex;
        if (e.Unit.UnitType == CombatUnitType.Nikke)
            m_NikkeNames[index].text = "---";
        else
            m_EnemyNames[index].text = "---";
        RefreshHpBar(e.Unit);
    }

    private void RefreshHpBar(CombatUnit unit)
    {
        int index = unit.SlotIndex;
        if (unit.UnitType == CombatUnitType.Nikke)
        {
            m_NikkeHpBars[index].value = (float)unit.CurrentHp / unit.MaxHp;
            m_NikkeEblaBars[index].value = unit.Ebla / 200f;
        }
        else
            m_EnemyHpBars[index].value = (float)unit.CurrentHp / unit.MaxHp;
    }
    private void RefreshTurnOrder()
    {
        if (m_CombatStateMachine.TurnOrder == null) return;
        m_TurnOrderBuilder.Clear();
        for (int i = 0; i < m_CombatStateMachine.TurnOrder.Count; ++i)
        {
            m_TurnOrderBuilder.Append(m_CombatStateMachine.TurnOrder[i].UnitName);
            m_TurnOrderBuilder.Append(" -> ");
        }
        m_TurnOrderText.text = m_TurnOrderBuilder.ToString();
    }

    private void OnUnitMoved(UnitMovedEvent e)
    {
        RefreshNikkeSlots();
    }

    private void RefreshNikkeSlots()
    {
        for (int i=0; i< m_NikkeNames.Length; ++i)
        {
            CombatUnit unit = m_CombatStateMachine.PositionSystem.GetUnit(CombatUnitType.Nikke, i);
            if(unit == null)
            {
                m_NikkeNames[i].text = "";
                m_NikkeHpBars[i].value = 0;
                m_NikkeEblaBars[i].value = 0;
            }
            else
            {
                m_NikkeNames[i].text = unit.UnitName;
                RefreshHpBar(unit);
            }
        }
    }

}
