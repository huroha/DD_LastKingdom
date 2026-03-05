using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

    private void Awake()
    {
        Initialize(m_CombatStateMachine);
    }

    private void OnEnable()
    {
        EventBus.Subscribe<BattleStartedEvent>(OnBattleStarted);
        EventBus.Subscribe<SkillExecutedEvent>(OnSkillExecuted);
        EventBus.Subscribe<UnitDiedEvent>(OnUnitDied);

    }
    private void OnDisable()
    {
        EventBus.Unsubscribe<BattleStartedEvent>(OnBattleStarted);
        EventBus.Unsubscribe<SkillExecutedEvent>(OnSkillExecuted);
        EventBus.Unsubscribe<UnitDiedEvent>(OnUnitDied);
    }
    public void Initialize(CombatStateMachine stateMachine)
    {
        if (stateMachine == null)
            return;
        m_CombatStateMachine = stateMachine;
    }

    private void OnBattleStarted(BattleStartedEvent e)
    {
        for (int i = 0; i < e.Nikkes.Count; ++i)
        {
            m_NikkeNames[i].text = e.Nikkes[i].UnitName;
            RefreshHpBar(e.Nikkes[i]);
        }

        for (int i=0; i< e.Enemies.Count; ++i)
        {
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
        if(e.Unit.UnitType == CombatUnitType.Nikke)
        {
            m_NikkeNames[e.Unit.SlotIndex].text = "---";
            m_NikkeHpBars[e.Unit.SlotIndex].value = 0;
            m_NikkeEblaBars[e.Unit.SlotIndex].value = 0;
        }
        else
        {
            m_EnemyNames[e.Unit.SlotIndex].text = "---";
            m_EnemyHpBars[e.Unit.SlotIndex].value = 0;
        }
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
        if (m_CombatStateMachine.TurnOrder == null)
            return;
        string result = "";
        for(int i=0; i<m_CombatStateMachine.TurnOrder.Count; ++i)
        {
            CombatUnit unit = m_CombatStateMachine.TurnOrder[i];
            result += unit.UnitName + " -> ";
        }
        m_TurnOrderText.text = result;
    }
}
