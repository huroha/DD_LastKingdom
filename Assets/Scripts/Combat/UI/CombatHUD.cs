using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class CombatHUD : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CombatStateMachine m_CombatStateMachine;
    [SerializeField] private CombatFieldView m_FieldView;
    [SerializeField] private Camera m_Camera;
    [SerializeField] private CombatTurnTickerDisplay m_TickerDisplay;
    [SerializeField] private CombatTurnBarDisplay m_TurnBarDisplay;

    [Header("Tooltip")]
    [SerializeField] private CombatTooltip m_CombatTooltip;
    [SerializeField] private FloatingLabel m_FloatingLabel;

    [Header("Round")]
    [SerializeField] private TextMeshProUGUI m_RoundText;
    [SerializeField] private int m_PendingRound;

    [Header("Status Effect Icons")]
    [SerializeField] private StatusEffectIconDisplay[] m_NikkeStatusIcons;          // 4개   
    [SerializeField] private StatusEffectIconDisplay[] m_EnemyStatusIcons;          // 4
    [SerializeField] private StatusEffectIconDisplay[] m_LargeEnemyStatusIcons;     // 3

    [Header("Root Slots")]
    [SerializeField] private GameObject m_NikkeHpBarRoot;
    [SerializeField] private GameObject m_EnemyHpBarRoot;
    [SerializeField] private GameObject m_SelectBar;

    [Header("Halo")]
    [SerializeField] private UnitHaloDisplay[] m_NikkeHalos;
    [SerializeField] private EnemyHaloDisplay[] m_EnemyHalos;
    [SerializeField] private EnemyHaloDisplay[] m_LargeEnemyHalos;

    [Header("Nikke Slots")]
    [SerializeField] private HpBarAnimator[] m_NikkeHpBars;    // 4개

    [System.Serializable]
    private struct EblaBarCells
    {
        public GameObject Root;
        public Image[] Cells;       // 10개
    }
    [SerializeField] private EblaBarCells[] m_NikkeEblaBars;
    [SerializeField] private TextMeshProUGUI[] m_NikkeNames;    // 4개


    [SerializeField] private Sprite m_EblaEmptySprite;
    [SerializeField] private Sprite m_EblaPhase1Sprite;
    [SerializeField] private Sprite m_EblaPhase2Sprite;


    [Header("Enemy Slots")]
    [SerializeField] private HpBarAnimator[] m_EnemyHpBars;  // 4개
    [SerializeField] private TextMeshProUGUI[] m_EnemyNames;   // 4개


    [Header("Large Enemy Slots")]
    [SerializeField] private HpBarAnimator[] m_LargeEnemyHpBars;

    [Header("Slot Roots")]
    [SerializeField] private RectTransform[] m_NikkeSlotRoots;      // 4개
    [SerializeField] private RectTransform[] m_EnemySlotRoots;      // 4개
    [SerializeField] private RectTransform[] m_LargeEnemySlotRoots; // 3개

    private Vector3[] m_OriginalNikkeSlotPositions;
    private Vector3[] m_OriginalEnemySlotPositions;
    private Vector3[] m_OriginalLargeEnemySlotPositions;

    [Header("Announce")]
    [SerializeField] private float m_AnnounceDisplayDuration = 1.2f;
    [SerializeField] private GameObject m_EnemySkillPanel;
    [SerializeField] private TextMeshProUGUI m_EnemySkillNameText;


    [Header("Enemy Info Panel")]
    [SerializeField] private EnemyInfoPanel m_EnemyInfoPanel;

    [SerializeField] private Image[] m_NTargetHighlights;  // 4개



    private CombatUnit[] m_CurrentEnemyBarUnits;
    private CombatUnit[] m_CurrentLargeEnemyBarUnits;
    private CombatUnit[] m_CurrentNikkeBarUnits;

    private CombatUnit m_HoveredUnit;

    private System.Text.StringBuilder m_TurnOrderBuilder = new System.Text.StringBuilder(128);

    public bool IsTickerAnimating => m_TickerDisplay.IsTickerAnimating;

    private void Awake()
    {
        Initialize(m_CombatStateMachine);

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
        EventBus.Subscribe<RoundStartedEvent>(OnRoundStarted);
        EventBus.Subscribe<TurnStartedEvent>(OnTurnStarted);
        if(m_CombatStateMachine !=null)
            m_CombatStateMachine.OnStateChanged += OnCombatStateChanged;

    }
    private void OnDisable()
    {
        EventBus.Unsubscribe<BattleStartedEvent>(OnBattleStarted);
        EventBus.Unsubscribe<SkillExecutedEvent>(OnSkillExecuted);
        EventBus.Unsubscribe<UnitDiedEvent>(OnUnitDied);
        EventBus.Unsubscribe<UnitMovedEvent>(OnUnitMoved);
        EventBus.Unsubscribe<TurnEndedEvent>(OnTurnEnded);
        EventBus.Unsubscribe<RoundStartedEvent>(OnRoundStarted);
        EventBus.Unsubscribe<TurnStartedEvent>(OnTurnStarted);
        if (m_CombatStateMachine != null)
            m_CombatStateMachine.OnStateChanged -= OnCombatStateChanged;
    }

    // 이벤트 함수
    private void OnUnitDied(UnitDiedEvent e)
    {

        m_TickerDisplay.RefreshTurnTickers();
        if (e.Unit == m_HoveredUnit)
            HideEnemyInfo();
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
        m_TickerDisplay.RefreshTurnTickers();
        m_TurnBarDisplay.Hide();
    }
    private void OnTurnStarted(TurnStartedEvent e)
    {
        m_TickerDisplay.HideOneTicker(e.Unit);
        m_TurnBarDisplay.Snap(e.Unit);
        m_FieldView.PlayPopScale(e.Unit);
        HideStunHalo(e.Unit);
    }
    private void OnUnitMoved(UnitMovedEvent e)
    {
        RefreshNikkeSlots();
        m_TickerDisplay.RefreshTurnTickers();
    }
    private void OnBattleStarted(BattleStartedEvent e)
    {
        // 전체 숨기기
        m_TickerDisplay.HideAllTickers();
        HideEnemyTargetHighlights();
        HideEnemySkillName();
        m_CurrentEnemyBarUnits = new CombatUnit[m_EnemyHpBars.Length];
        m_CurrentLargeEnemyBarUnits = new CombatUnit[m_LargeEnemyHpBars.Length];
        m_CurrentNikkeBarUnits = new CombatUnit[m_NikkeHpBars.Length];

        for (int i = 0; i < m_NikkeStatusIcons.Length; ++i)
            m_NikkeStatusIcons[i].SetTooltip(m_CombatTooltip);
        for (int i = 0; i < m_EnemyStatusIcons.Length; ++i)
            m_EnemyStatusIcons[i].SetTooltip(m_CombatTooltip);
        for (int i = 0; i < m_LargeEnemyStatusIcons.Length; ++i)
            m_LargeEnemyStatusIcons[i].SetTooltip(m_CombatTooltip);

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

        for (int i = 0; i < m_LargeEnemyHpBars.Length; ++i)
        {
            m_LargeEnemyHpBars[i].gameObject.SetActive(false);

        }

        // 데이터 있는 슬롯만 표시
        for (int i = 0; i < e.Nikkes.Count; ++i)
        {
            m_NikkeHpBars[i].gameObject.SetActive(true);
            m_NikkeNames[i].gameObject.SetActive(true);
            m_NikkeEblaBars[i].Root.SetActive(true);
            m_NikkeNames[i].text = e.Nikkes[i].UnitName;
            InitHpBar(e.Nikkes[i]);
            m_CurrentNikkeBarUnits[i] = e.Nikkes[i];
            int nikkeIndex = i;
            SetupTooltipTrigger(m_NikkeHpBars[i].gameObject, (sb) =>
            {
                CombatUnit u = m_CombatStateMachine.PositionSystem.GetUnit(CombatUnitType.Nikke, nikkeIndex);
                if (u == null) return;
                sb.Append("<color=#BF1313>체력: ").Append(u.CurrentHp).Append(" / ").Append(u.MaxHp)
                  .Append(" </color>\n")
                  .Append("<color=white>에블라: ").Append(u.Ebla).Append(" / 200</color>");
            },new Vector2(0, 85));
        }

        for (int i = 0; i < e.Enemies.Count; ++i)
        {
            CombatUnit enemy = e.Enemies[i];
            if (enemy.SlotSize == 2)
            {
                int largeIndex = enemy.SlotIndex;
                m_LargeEnemyHpBars[largeIndex].gameObject.SetActive(true);
                m_CurrentLargeEnemyBarUnits[enemy.SlotIndex] = enemy;
            }
            else
            {
                m_EnemyHpBars[enemy.SlotIndex].gameObject.SetActive(true);
                m_EnemyNames[enemy.SlotIndex].gameObject.SetActive(true);
                m_EnemyNames[enemy.SlotIndex].text = enemy.UnitName;
                m_CurrentEnemyBarUnits[enemy.SlotIndex] = enemy;
            }

            InitHpBar(enemy);
            int enemyIndex = enemy.SlotIndex;
            GameObject barObj = enemy.SlotSize == 2
                ? m_LargeEnemyHpBars[enemyIndex].gameObject
                : m_EnemyHpBars[enemyIndex].gameObject;
            SetupTooltipTrigger(barObj, (sb) =>
            {
                IReadOnlyList<CombatUnit> order = m_CombatStateMachine.TurnOrder;
                if (order == null) return;
                int count = 0;
                int currentIdx = m_CombatStateMachine.CurrentTurnIndex;
                for (int t = currentIdx + 1; t < order.Count; ++t)
                {
                    if (order[t] == m_CombatStateMachine.PositionSystem.GetUnit(CombatUnitType.Enemy, enemyIndex))
                        ++count;
                }
                if (count == 0) return;
                sb.Append("남은 행동:").Append(count);
            },new Vector2(0, 0));
        }

        m_TickerDisplay.ShowAllTickersAnimated();
        m_TurnBarDisplay.Hide();
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

    }

    private void OnRoundStarted(RoundStartedEvent e)
    {
        m_PendingRound = e.Round;
        m_TickerDisplay.HideAllTickers();
        m_TickerDisplay.ShowAllTickersAnimated();
    }

    private void OnCombatStateChanged(CombatState newState)
    {
        if (newState != CombatState.PlayerSelectTarget)
            m_EnemyInfoPanel.HidePreviewSection();
    }

    // 헬퍼들
    private void SetupTooltipTrigger(GameObject target, TooltipTrigger.ContentBuilderHandler contentBuilder, Vector2 offset)
    {
        TooltipTrigger trigger = target.GetComponent<TooltipTrigger>();
        if (trigger == null)
            trigger = target.AddComponent<TooltipTrigger>();
        trigger.Initialize(m_CombatTooltip, contentBuilder, offset);
    }
    private void RefreshHpBar(CombatUnit unit)
    {
        int index = unit.SlotIndex;
        if (unit.UnitType == CombatUnitType.Nikke)
        {
            m_NikkeHpBars[index].SetHp(unit.CurrentHp, unit.MaxHp);
            UpdateEblaBar(index, unit.Ebla);
        }
        else
        {
            HpBarAnimator bar = unit.SlotSize == 2 ? m_LargeEnemyHpBars[unit.SlotIndex] : m_EnemyHpBars[unit.SlotIndex];
            if (unit.State == UnitState.Corpse)
                bar.InitHp(unit.CurrentHp, Mathf.Max(unit.EnemyData.CorpseHp, 1));
            else
                bar.SetHp(unit.CurrentHp, unit.MaxHp);
        }
        RefreshStatusIcons(unit);
    }
    private void InitHpBar(CombatUnit unit)
    {
        int index = unit.SlotIndex;
        if(unit.UnitType == CombatUnitType.Nikke)
        {
            m_NikkeHpBars[index].InitHp(unit.CurrentHp, unit.MaxHp);
            UpdateEblaBar(index, unit.Ebla);
        }
        else
        {
            HpBarAnimator bar = unit.SlotSize == 2 ? m_LargeEnemyHpBars[unit.SlotIndex] : m_EnemyHpBars[unit.SlotIndex];
            if (unit.State == UnitState.Corpse)
                bar.InitHp(unit.CurrentHp, Mathf.Max(unit.EnemyData.CorpseHp, 1));
            else
                bar.InitHp(unit.CurrentHp, unit.MaxHp);
        }
        RefreshStatusIcons(unit);
    }




    private void RefreshNikkeSlots()
    {
        for (int i = 0; i < m_NikkeNames.Length; ++i)
        {
            CombatUnit unit = m_CombatStateMachine.PositionSystem.GetUnit(CombatUnitType.Nikke, i);
            if (unit == null)
            {
                m_NikkeNames[i].gameObject.SetActive(false);
                m_NikkeHpBars[i].gameObject.SetActive(false);
                m_NikkeEblaBars[i].Root.SetActive(false);
                m_NikkeStatusIcons[i].Clear();
                UpdateEblaBar(i, 0);
            }
            else
            {
                if (m_CurrentNikkeBarUnits[i] != unit)
                {
                    m_CurrentNikkeBarUnits[i] = unit;
                    m_NikkeNames[i].text = unit.UnitName;
                    InitHpBar(unit);
                }
                else
                    RefreshStatusIcons(unit);
            }
        }
    }

    private void RefreshEnemySlots()
    {
        for (int i = 0; i < m_EnemyHpBars.Length; ++i)
        {
            CombatUnit unit = m_CombatStateMachine.PositionSystem.GetUnit(CombatUnitType.Enemy, i);

            if (unit == null)
            {
                m_EnemyHpBars[i].gameObject.SetActive(false);
                m_EnemyNames[i].gameObject.SetActive(false);
                m_EnemyStatusIcons[i].Clear();
                m_CurrentEnemyBarUnits[i] = null;
            }
            else if (unit.SlotSize == 2)
            {
                m_EnemyHpBars[i].gameObject.SetActive(false);
                m_EnemyNames[i].gameObject.SetActive(false);
                m_CurrentEnemyBarUnits[i] = null;
            }
            else
            {
                m_EnemyHpBars[i].gameObject.SetActive(true);
                m_EnemyNames[i].gameObject.SetActive(true);
                m_EnemyNames[i].text = unit.UnitName;
                if (m_CurrentEnemyBarUnits[i] != unit)
                {
                    m_CurrentEnemyBarUnits[i] = unit;
                    InitHpBar(unit);
                }
                else
                    RefreshStatusIcons(unit);
            }
        }

        for (int i = 0; i < m_LargeEnemyHpBars.Length; ++i)
        {
            CombatUnit unit = m_CombatStateMachine.PositionSystem.GetUnit(CombatUnitType.Enemy, i);
            if (unit != null && unit.SlotSize == 2 && unit.SlotIndex == i)
            {
                m_LargeEnemyHpBars[i].gameObject.SetActive(true);
                if (m_CurrentLargeEnemyBarUnits[i] != unit)
                {
                    m_CurrentLargeEnemyBarUnits[i] = unit;
                    InitHpBar(unit);
                }
                else
                    RefreshStatusIcons(unit);
            }
            else
            {
                m_LargeEnemyHpBars[i].gameObject.SetActive(false);
                m_LargeEnemyStatusIcons[i].Clear();
                m_CurrentLargeEnemyBarUnits[i] = null;
            }
        }
    }

    private void UpdateEblaBar(int index, int ebla)
    {
        Image[] cells = m_NikkeEblaBars[index].Cells;
        int phase1Count = Mathf.CeilToInt(Mathf.Min(ebla, CombatUnit.EblaPhaseThreshold) / (float)CombatUnit.EblaCellValue);
        int phase2Count = ebla > CombatUnit.EblaPhaseThreshold ? Mathf.CeilToInt((ebla - CombatUnit.EblaPhaseThreshold) / (float)CombatUnit.EblaCellValue) : 0;

        for (int i = 0; i < cells.Length; ++i)
        {
            if (i < phase2Count)
                cells[i].sprite = m_EblaPhase2Sprite;
            else if (i < phase1Count)
                cells[i].sprite = m_EblaPhase1Sprite;
            else
                cells[i].sprite = m_EblaEmptySprite;
        }
    }
    public void ShowEnemyInfo(CombatUnit unit)
    {
        m_HoveredUnit = unit;
        m_EnemyInfoPanel.gameObject.SetActive(true);
        m_EnemyInfoPanel.Populate(unit);
        if (m_CombatStateMachine.CurrentState == CombatState.PlayerSelectTarget
             && m_CombatStateMachine.SelectedSkill != null
             && m_CombatStateMachine.IsValidTarget(unit))
        {
            AttackPreview preview = m_CombatStateMachine.PreviewAttack(unit);
            m_EnemyInfoPanel.PopulatePreview(preview);
            m_EnemyInfoPanel.ShowPreviewSection();
        }
        else
            m_EnemyInfoPanel.HidePreviewSection();
    }

    public void HideEnemyInfo()
    {
        m_HoveredUnit = null;
        m_EnemyInfoPanel.Hide();
    }


    public void ShowEnemyTargetHighlight(int slotIndex)
    {
        HideEnemyTargetHighlights();
        m_NTargetHighlights[slotIndex].gameObject.SetActive(true);
    }

    public void HideEnemyTargetHighlights()
    {
        for (int i = 0; i < m_NTargetHighlights.Length; ++i)
        {
            m_NTargetHighlights[i].gameObject.SetActive(false);
        }
    }

    public void ShowEnemySkillName(string skillName)
    {
        m_EnemySkillNameText.text = skillName;
        m_EnemySkillPanel.SetActive(false);
        m_EnemySkillPanel.SetActive(true);
    }
    public Coroutine ShowPassLabel(CombatUnit unit)
    {
        RectTransform anchor = m_TurnBarDisplay.GetAnchor(unit);
        return m_FloatingLabel.Show("넘기기", anchor);
    }
    public void HidePassLabel() => m_FloatingLabel.Hide();
    public void HideEnemySkillName()
    {
        m_EnemySkillPanel.SetActive(false);
    }
    // Round 표기
    public void ApplyRoundText()
    {
        m_RoundText.SetText("{0}", m_PendingRound);
    }
    public void RefreshHoveredPreview()
    {
        if (m_HoveredUnit != null)
            ShowEnemyInfo(m_HoveredUnit);
    }

    private void RefreshStatusIcons(CombatUnit unit)
    {
        if (unit.UnitType == CombatUnitType.Nikke)
            m_NikkeStatusIcons[unit.SlotIndex].Refresh(unit.ActiveEffects);
        else if(unit.UnitType==CombatUnitType.Enemy && unit.SlotSize == 2)
            m_LargeEnemyStatusIcons[unit.SlotIndex].Refresh(unit.ActiveEffects);
        else
            m_EnemyStatusIcons[unit.SlotIndex].Refresh(unit.ActiveEffects);
    }

    public void SetHpBarsVisible(bool visible)
    {
        m_NikkeHpBarRoot.SetActive(visible);
        m_EnemyHpBarRoot.SetActive(visible);
        m_SelectBar.SetActive(visible);
    }

    public Coroutine PlayEnemySkillAnnounce(string skillName)
    {
        return StartCoroutine(EnemySkillAnnounceRoutine(skillName));
    }

    private IEnumerator EnemySkillAnnounceRoutine(string skillName)
    {
        ShowEnemySkillName(skillName);
        yield return new WaitForSecondsRealtime(m_AnnounceDisplayDuration);
        HideEnemySkillName();
    }
    public void RefreshUnit(CombatUnit unit)
    {
        RefreshHpBar(unit);
        RefreshStatusIcons(unit);
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
    public void PrepareHpGhost(CombatUnit unit, int previousHp)
    {
        if (unit.State == UnitState.Dead)   // Corpse 조건 제거
            return;
        HpBarAnimator bar = GetHpBarAnimator(unit);
        if (bar == null)
            return;
        int maxHp = unit.State == UnitState.Corpse ? Mathf.Max(unit.EnemyData.CorpseHp, 1) : unit.MaxHp;
        bar.PrepareGhost(previousHp , unit.CurrentHp, maxHp);
    }

    public void StartHpGhostDrain(CombatUnit unit)
    {
        HpBarAnimator bar = GetHpBarAnimator(unit);
        if (bar == null)
            return;
        if (!bar.gameObject.activeSelf)
            bar.gameObject.SetActive(true);
        bar.StartGhostDrain();
    }

    private HpBarAnimator GetHpBarAnimator(CombatUnit unit)
    {
        if (unit.UnitType == CombatUnitType.Nikke)
            return m_NikkeHpBars[unit.SlotIndex];
        return unit.SlotSize == 2
            ? m_LargeEnemyHpBars[unit.SlotIndex]
            : m_EnemyHpBars[unit.SlotIndex];
    }
    public void SnapNikkeHpBarsToSlots()
    {
        RefreshNikkeSlots();
    }

    // Halo 관련
    private UnitHaloDisplay GetHaloDisplay(CombatUnit unit)
    {
        if (unit.UnitType != CombatUnitType.Nikke)
            return null;
        return m_NikkeHalos[unit.SlotIndex];
    }
    public void ShowStunHalo(CombatUnit unit)
    {
        GetStunHaloDisplay(unit)?.ShowStunHalo();
    }
    public void HideStunHalo(CombatUnit unit)
    {
        GetStunHaloDisplay(unit)?.HideStunHalo();
    }
    public void PopupDeathsDoorHalo(CombatUnit unit)
    {
        GetHaloDisplay(unit)?.PopupDeathsDoor();
    }
    public void PopupEblaHalo(CombatUnit unit, int delta)
    {
        if (delta > 0)
            GetHaloDisplay(unit)?.PopupEblaUp(delta);
        else if(delta < 0)
            GetHaloDisplay(unit)?.PopupEblaDown(delta);
    }
    private IStunHaloDisplay GetStunHaloDisplay(CombatUnit unit)
    {
        if (unit.UnitType == CombatUnitType.Nikke)
            return m_NikkeHalos[unit.SlotIndex];
        return unit.SlotSize == 2
            ? m_LargeEnemyHalos[unit.SlotIndex]
            : m_EnemyHalos[unit.SlotIndex];
    }
}
