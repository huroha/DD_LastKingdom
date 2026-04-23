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
    [SerializeField] private CombatTurnTickerDisplay m_TickerDisplay;
    [SerializeField] private CombatTurnBarDisplay m_TurnBarDisplay;
    [SerializeField] private CombatHaloController m_HaloController;
    [SerializeField] private CombatStatusIconController m_StatusIconController;
    [SerializeField] private CombatHpBarController m_HpBarController;

    [Header("Tooltip")]
    [SerializeField] private CombatTooltip m_CombatTooltip;
    [SerializeField] private FloatingLabel m_FloatingLabel;

    [Header("Round")]
    [SerializeField] private TextMeshProUGUI m_RoundText;
    [SerializeField] private int m_PendingRound;

    [Header("Root Slots")]
    [SerializeField] private GameObject m_NikkeHpBarRoot;
    [SerializeField] private GameObject m_EnemyHpBarRoot;
    [SerializeField] private GameObject m_SelectBar;


    [System.Serializable]
    private struct EblaBarCells
    {
        public GameObject Root;
        public Image[] Cells;       // 10개
    }
    [Header("Nikke EblaBar")]
    [SerializeField] private EblaBarCells[] m_NikkeEblaBars;
    [SerializeField] private TextMeshProUGUI[] m_NikkeNames;    // 4개

    [SerializeField] private Sprite m_EblaEmptySprite;
    [SerializeField] private Sprite m_EblaPhase1Sprite;
    [SerializeField] private Sprite m_EblaPhase2Sprite;

    [Header("Announce")]
    [SerializeField] private float m_AnnounceDisplayDuration = 1.2f;
    [SerializeField] private GameObject m_EnemySkillPanel;
    [SerializeField] private TextMeshProUGUI m_EnemySkillNameText;

    [Header("Enemy Info Panel")]
    [SerializeField] private EnemyInfoPanel m_EnemyInfoPanel;

    [SerializeField] private Image[] m_NTargetHighlights;  // 4개

    [Header("Ebla Resolution")]
    [SerializeField] private AfflictionNarrationPanel m_NarrationPanel;



    private CombatUnit[] m_CurrentEnemyBarUnits;
    private CombatUnit[] m_CurrentLargeEnemyBarUnits;
    private CombatUnit[] m_CurrentNikkeBarUnits;

    private CombatUnit m_HoveredUnit;
    private HashSet<CombatUnit> m_HiddenTickers;


    private System.Text.StringBuilder m_TurnOrderBuilder = new System.Text.StringBuilder(128);

    public bool IsTickerAnimating => m_TickerDisplay.IsTickerAnimating;

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
        RefreshTickers();
        if (e.Unit == m_HoveredUnit)
            HideEnemyInfo();
        if (e.Unit.State == UnitState.Dead)
        {
            m_HpBarController.RemoveHidden(e.Unit);
            m_HiddenTickers.Remove(e.Unit);
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
        for (int i = 0; i < m_HpBarController.NikkeCount; ++i)
        {
            // 활성 니케 에블라 갱신
            if (!m_HpBarController.GetNikkeBar(i).gameObject.activeSelf)
                continue;
            CombatUnit unit = m_CombatStateMachine.PositionSystem.GetUnit(CombatUnitType.Nikke, i);
            if (unit != null)
                UpdateEblaBar(i, unit.Ebla);

        }
        RefreshTickers();
        m_TurnBarDisplay.Hide();
    }
    private void OnTurnStarted(TurnStartedEvent e)
    {
        m_TickerDisplay.HideOneTicker(e.Unit);
        m_TurnBarDisplay.Snap(e.Unit);
        m_FieldView.PlayPopScale(e.Unit);
        m_HaloController.HideStunHalo(e.Unit);
    }
    private void OnUnitMoved(UnitMovedEvent e)
    {
        RefreshNikkeSlots();
        RefreshEnemySlots();
        RefreshTickers();
    }
    private void OnBattleStarted(BattleStartedEvent e)
    {
        m_TickerDisplay.HideAllTickers();
        HideEnemyTargetHighlights();
        HideEnemySkillName();
        m_CurrentEnemyBarUnits = new CombatUnit[m_HpBarController.EnemyCount];
        m_CurrentLargeEnemyBarUnits = new CombatUnit[m_HpBarController.LargeEnemyCount];
        m_CurrentNikkeBarUnits = new CombatUnit[m_HpBarController.NikkeCount];

        m_HpBarController.ClearHidden();
        if (m_HiddenTickers == null)
            m_HiddenTickers = new HashSet<CombatUnit>();
        else
            m_HiddenTickers.Clear();

        m_StatusIconController.SetupTooltips(m_CombatTooltip);

        m_HpBarController.HideAllBars();
        for (int i = 0; i < m_NikkeNames.Length; ++i)
        {
            m_NikkeNames[i].gameObject.SetActive(false);
            m_NikkeEblaBars[i].Root.SetActive(false);
        }

        // 실제로 있는 슬롯만 표시
        for (int i = 0; i < e.Nikkes.Count; ++i)
        {
            m_HpBarController.GetNikkeBar(i).gameObject.SetActive(true);
            m_NikkeNames[i].gameObject.SetActive(true);
            m_NikkeEblaBars[i].Root.SetActive(true);
            m_NikkeNames[i].text = e.Nikkes[i].UnitName;
            InitHpBar(e.Nikkes[i]);
            m_CurrentNikkeBarUnits[i] = e.Nikkes[i];
            int nikkeIndex = i;
            SetupTooltipTrigger(m_HpBarController.GetNikkeBar(i).gameObject, (sb) =>
            {
                CombatUnit u = m_CombatStateMachine.PositionSystem.GetUnit(CombatUnitType.Nikke, nikkeIndex);
                if (u == null) return;
                sb.Append("<color=#BF1313>체력: ").Append(u.CurrentHp).Append(" / ").Append(u.MaxHp)
                  .Append(" </color>\n")
                  .Append("<color=white>에블라: ").Append(u.Ebla).Append(" / 200</color>");
            }, new Vector2(0, 85));
        }

        for (int i = 0; i < e.Enemies.Count; ++i)
        {
            CombatUnit enemy = e.Enemies[i];
            if (enemy.SlotSize == 2)
            {
                int largeIndex = enemy.SlotIndex;
                m_HpBarController.GetLargeEnemyBar(largeIndex).gameObject.SetActive(true);
                m_CurrentLargeEnemyBarUnits[enemy.SlotIndex] = enemy;
            }
            else
            {
                m_HpBarController.GetEnemyBar(enemy.SlotIndex).gameObject.SetActive(true);
                m_CurrentEnemyBarUnits[enemy.SlotIndex] = enemy;
            }

            InitHpBar(enemy);
            int enemyIndex = enemy.SlotIndex;
            GameObject barObj = enemy.SlotSize == 2
                ? m_HpBarController.GetLargeEnemyBar(enemyIndex).gameObject
                : m_HpBarController.GetEnemyBar(enemyIndex).gameObject;
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
            }, new Vector2(0, 0));
        }

        m_TickerDisplay.ShowAllTickersAnimated();
        m_TurnBarDisplay.Hide();
    }

    private void OnSkillExecuted(SkillExecutedEvent e)
    {
        if (e.Result.TargetResults == null)
            return;

        for (int i = 0; i < e.Result.TargetResults.Length; ++i)
        {
            TargetResult result = e.Result.TargetResults[i];
            if (result.Target.UnitType == CombatUnitType.Nikke)
                RefreshHpBar(result.Target);
        }
        // 스킬이 MoveUserAmount / MoveTargetAmount로 양 진영 모두를 이동시킬수 있음. 둘다 새로고침
        RefreshNikkeSlots();
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
        m_HpBarController.RefreshBar(unit);
        if (unit.UnitType == CombatUnitType.Nikke)
            UpdateEblaBar(unit.SlotIndex, unit.Ebla);
        RefreshStatusIcons(unit);
    }
    private void InitHpBar(CombatUnit unit)
    {
        m_HpBarController.InitBar(unit);
        if (unit.UnitType == CombatUnitType.Nikke)
            UpdateEblaBar(unit.SlotIndex, unit.Ebla);
        RefreshStatusIcons(unit);
    }
    public void ShowHpBarForCorpse(CombatUnit unit)
    {
        m_HiddenTickers.Remove(unit);
        RefreshTickers();
        m_HpBarController.ShowForCorpse(unit);
    }
    private void RefreshTickers()
    {
        m_TickerDisplay.RefreshTurnTickers();
        foreach(CombatUnit unit in m_HiddenTickers)
        {
            m_TickerDisplay.HideUnitTickers(unit);
        }
    }
    public void HideTicker(CombatUnit unit)
    {
        m_HiddenTickers.Add(unit);
        m_TickerDisplay.HideUnitTickers(unit);
    }
    private void RefreshNikkeSlots()
    {
        for (int i = 0; i < m_NikkeNames.Length; ++i)
        {
            CombatUnit unit = m_CombatStateMachine.PositionSystem.GetUnit(CombatUnitType.Nikke, i);
            if (unit == null)
            {
                m_NikkeNames[i].gameObject.SetActive(false);
                m_HpBarController.GetNikkeBar(i).gameObject.SetActive(false);
                m_NikkeEblaBars[i].Root.SetActive(false);
                m_StatusIconController.ClearNikke(i);
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
        for (int i = 0; i < m_HpBarController.EnemyCount; ++i)
        {
            CombatUnit unit = m_CombatStateMachine.PositionSystem.GetUnit(CombatUnitType.Enemy, i);

            if (unit == null)
            {
                m_HpBarController.GetEnemyBar(i).gameObject.SetActive(false);
                m_StatusIconController.ClearEnemy(i);
                m_CurrentEnemyBarUnits[i] = null;
            }
            else if (unit.SlotSize == 2)
            {
                m_HpBarController.GetEnemyBar(i).gameObject.SetActive(false);
                m_StatusIconController.ClearEnemy(i);
                m_CurrentEnemyBarUnits[i] = null;
            }
            else
            {
                if (m_HpBarController.IsHidden(unit))
                {
                    m_HpBarController.GetEnemyBar(i).gameObject.SetActive(false);
                    continue;
                }
                m_HpBarController.GetEnemyBar(i).gameObject.SetActive(true);
                if (m_CurrentEnemyBarUnits[i] != unit)
                {
                    m_CurrentEnemyBarUnits[i] = unit;
                    InitHpBar(unit);
                }
                else
                    RefreshStatusIcons(unit);
            }
        }

        for (int i = 0; i < m_HpBarController.LargeEnemyCount; ++i)
        {
            CombatUnit unit = m_CombatStateMachine.PositionSystem.GetUnit(CombatUnitType.Enemy, i);
            if (unit != null && unit.SlotSize == 2 && unit.SlotIndex == i)
            {
                if (m_HpBarController.IsHidden(unit))
                {
                    m_HpBarController.GetLargeEnemyBar(i).gameObject.SetActive(false);
                    continue;
                }
                m_HpBarController.GetLargeEnemyBar(i).gameObject.SetActive(true);
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
                if (unit != null && m_HpBarController.IsHidden(unit))
                {
                    m_HpBarController.GetLargeEnemyBar(i).gameObject.SetActive(false);
                    continue;
                }
                m_HpBarController.GetLargeEnemyBar(i).gameObject.SetActive(false);
                m_StatusIconController.ClearLargeEnemy(i);
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
        if (m_HpBarController.IsHidden(unit)) return;
        m_StatusIconController.Refresh(unit);
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

    public IEnumerator PlayNarration(PendingEblaResolution pending)
    {
        if (m_NarrationPanel == null)
            yield break;
        yield return m_NarrationPanel.Play(pending);
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
    public void SnapNikkeHpBarsToSlots()
    {
        RefreshNikkeSlots();
    }

}
