using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class CombatHUD : MonoBehaviour
{
    [Header("Nikke Slots")]
    [SerializeField] private Slider[]           m_NikkeHpBars;    // 4개

    [System.Serializable]
    private struct EblaBarCells
    {
        public GameObject Root;
        public Image[] Cells;       // 10개
    }
    [SerializeField] private EblaBarCells[]     m_NikkeEblaBars;
    [SerializeField] private TextMeshProUGUI[]  m_NikkeNames;    // 4개


    [SerializeField] private Sprite m_EblaEmptySprite;
    [SerializeField] private Sprite m_EblaPhase1Sprite;
    [SerializeField] private Sprite m_EblaPhase2Sprite;


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
    public void Initialize(CombatStateMachine stateMachine)
    {
        if (stateMachine == null)
            return;
        m_CombatStateMachine = stateMachine;
    }



    private void OnEnable()
    {
        EventBus.Subscribe<BattleStartedEvent>(OnBattleStarted);
        EventBus.Subscribe<SkillExecutedEvent>(OnSkillExecuted);
        EventBus.Subscribe<UnitDiedEvent>(OnUnitDied);
        EventBus.Subscribe<UnitMovedEvent>(OnUnitMoved);
        EventBus.Subscribe<TurnEndedEvent>(OnTurnEnded);

    }
    private void OnDisable()
    {
        EventBus.Unsubscribe<BattleStartedEvent>(OnBattleStarted);
        EventBus.Unsubscribe<SkillExecutedEvent>(OnSkillExecuted);
        EventBus.Unsubscribe<UnitDiedEvent>(OnUnitDied);
        EventBus.Unsubscribe<UnitMovedEvent>(OnUnitMoved);
        EventBus.Unsubscribe<TurnEndedEvent>(OnTurnEnded);
    }

    // 이벤트 함수
    private void OnUnitDied(UnitDiedEvent e)
    {

        if (e.Unit.State == UnitState.Dead)
        {
            if (e.Unit.UnitType == CombatUnitType.Nikke)
                RefreshNikkeSlots();
            else
                RefreshEnemySlots();
        }
        else if (e.Unit.State == UnitState.Corpse)
            RefreshHpBar(e.Unit);
    }

    private void OnTurnEnded(TurnEndedEvent e)
    {
        for (int i = 0; i < m_NikkeHpBars.Length; ++i)
        {
            // 활성 니케 에블라 갱신
            if (!m_NikkeHpBars[i].gameObject.activeSelf)
                continue;
            CombatUnit unit = m_CombatStateMachine.PositionSystem.GetUnit(CombatUnitType.Nikke, i);
            if (unit != null)
                UpdateEblaBar(i, unit.Ebla);
        }
    }
    private void OnUnitMoved(UnitMovedEvent e)
    {
        RefreshNikkeSlots();
    }
    private void OnBattleStarted(BattleStartedEvent e)
    {
        // 전체 숨기기
        for (int i = 0; i < m_NikkeHpBars.Length; ++i)
        {
            m_NikkeHpBars[i].gameObject.SetActive(false);
            m_NikkeNames[i].gameObject.SetActive(false);
            m_NikkeEblaBars[i].Root.SetActive(false);
        }
        for (int i = 0; i < m_EnemyHpBars.Length; ++i)
        {
            m_EnemyHpBars[i].gameObject.SetActive(false);
            m_EnemyNames[i].gameObject.SetActive(false);
        }

        // 데이터 있는 슬롯만 표시
        for (int i = 0; i < e.Nikkes.Count; ++i)
        {
            m_NikkeHpBars[i].gameObject.SetActive(true);
            m_NikkeNames[i].gameObject.SetActive(true);
            m_NikkeEblaBars[i].Root.SetActive(true);
            m_NikkeNames[i].text = e.Nikkes[i].UnitName;
            RefreshHpBar(e.Nikkes[i]);
        }

        for (int i = 0; i < e.Enemies.Count; ++i)
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

        bool hasEnemyTarget = false;
        for (int i = 0; i < e.Result.TargetResults.Length; ++i)
        {
            TargetResult result = e.Result.TargetResults[i];
            if (result.Target.UnitType == CombatUnitType.Nikke)
                RefreshHpBar(result.Target);
            else
                hasEnemyTarget = true;
        }

        if (hasEnemyTarget)
            RefreshEnemySlots();

        RefreshTurnOrder();
    }



    private void RefreshHpBar(CombatUnit unit)
    {
        int index = unit.SlotIndex;
        if (unit.UnitType == CombatUnitType.Nikke)
        {
            m_NikkeHpBars[index].value = (float)unit.CurrentHp / unit.MaxHp;
            UpdateEblaBar(index, unit.Ebla);
        }
        else
        {
            if(unit.State == UnitState.Corpse)
                m_EnemyHpBars[index].value = (float)unit.CurrentHp / Mathf.Max(unit.EnemyData.CorpseHp, 1);
            else
                m_EnemyHpBars[index].value = (float)unit.CurrentHp / unit.MaxHp;
        }
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



    private void RefreshNikkeSlots()
    {
        for (int i=0; i< m_NikkeNames.Length; ++i)
        {
            CombatUnit unit = m_CombatStateMachine.PositionSystem.GetUnit(CombatUnitType.Nikke, i);
            if(unit == null)
            {
                m_NikkeNames[i].gameObject.SetActive(false);
                m_NikkeHpBars[i].gameObject.SetActive(false);
                m_NikkeEblaBars[i].Root.SetActive(false);
                UpdateEblaBar(i, 0);
            }
            else
            {
                m_NikkeNames[i].text = unit.UnitName;
                RefreshHpBar(unit);
            }
        }
    }

    private void RefreshEnemySlots()
    {
        for(int i=0; i< m_EnemyHpBars.Length; ++i)
        {
            CombatUnit unit = m_CombatStateMachine.PositionSystem.GetUnit(CombatUnitType.Enemy, i);
            if (unit == null)
            {
                m_EnemyHpBars[i].gameObject.SetActive(false);
                m_EnemyNames[i].gameObject.SetActive(false);
            }
            else
            {
                m_EnemyHpBars[i].gameObject.SetActive(true);
                m_EnemyNames[i].gameObject.SetActive(true);
                m_EnemyNames[i].text = unit.UnitName;
                RefreshHpBar(unit);
            }
        }
    }

    private void UpdateEblaBar(int index, int ebla)
    {
        Image[] cells = m_NikkeEblaBars[index].Cells;
        int phase1Count = Mathf.CeilToInt(Mathf.Min(ebla, 100) / 10f);
        int phase2Count = ebla > 100 ? Mathf.CeilToInt((ebla - 100) / 10f) : 0;

        for (int i=0; i< cells.Length; ++i)
        {
            if (i < phase2Count)
                cells[i].sprite = m_EblaPhase2Sprite;
            else if (i < phase1Count)
                cells[i].sprite = m_EblaPhase1Sprite;
            else
                cells[i].sprite = m_EblaEmptySprite;
        }
    }

}
