using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;






public class CombatHUD : MonoBehaviour
{
    [Header("Tooltip")]
    [SerializeField] private CombatTooltip m_CombatTooltip;

    [Header("Round")]
    [SerializeField] private TextMeshProUGUI m_RoundText;
    [SerializeField] private GameObject m_RoundBg;
    [SerializeField] private int m_PendingRound;

    [Header("Status Effect Icons")]
    [SerializeField] private StatusEffectIconDisplay[] m_NikkeStatusIcons;          // 4개   
    [SerializeField] private StatusEffectIconDisplay[] m_EnemyStatusIcons;          // 4
    [SerializeField] private StatusEffectIconDisplay[] m_LargeEnemyStatusIcons;     // 3



    [Header("Nikke Slots")]
    [SerializeField] private Slider[] m_NikkeHpBars;    // 4개

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
    [SerializeField] private Slider[] m_EnemyHpBars;  // 4개
    [SerializeField] private TextMeshProUGUI[] m_EnemyNames;   // 4개
    [SerializeField] private GameObject m_EnemySkillPanel;
    [SerializeField] private TextMeshProUGUI m_EnemySkillNameText;


    [Header("Large Enemy Slots")]
    [SerializeField] private Slider[] m_LargeEnemyHpBars;
    [SerializeField] private RectTransform[] m_LargeEnemyBarAnchors;

    [Header("Enemy Info Panel")]
    [SerializeField] private EnemyInfoPanel m_EnemyInfoPanel;

    [System.Serializable]
    private struct TickerGroup
    {
        public Image[] Tickers;     // 슬롯당 최대 ActionsPerRound  갯수
        [System.NonSerialized] public Animator[] Animators;
    }
    [Header("Turn Tickers")]
    [SerializeField] private TickerGroup[] m_NikkeTurnTickerGroups;     // 4개
    [SerializeField] private TickerGroup[] m_EnemyTurnTickerGroups;     // 4개
    [SerializeField] private TickerGroup[] m_LargeEnemyTickerGroups;   // 3개
    [SerializeField] private float m_TickerAnimDuration = 0.5f;


    [Header("Active turn Bar")]
    [SerializeField] private Image m_ActiveTurnBar;
    [SerializeField] private Image m_LargeActiveTurnBar;
    [SerializeField] private RectTransform[] m_NikkeBarAnchor;
    [SerializeField] private RectTransform[] m_EnemyBarAnchor;
    [SerializeField] private Image[] m_NTargetHighlights;  // 4개


    [Header("Turn Order")]
    [SerializeField] private TextMeshProUGUI m_TurnOrderText;


    [Header("References")]
    [SerializeField] private CombatStateMachine m_CombatStateMachine;
    private CombatUnit m_HoveredUnit;

    private System.Text.StringBuilder m_TurnOrderBuilder = new System.Text.StringBuilder(128);

    private Dictionary<CombatUnit, int> m_TickerCountCache = new Dictionary<CombatUnit, int>(); // 턴 refresh용

    public bool IsTickerAnimating { get; private set; }

    private void Awake()
    {
        CacheTickerAnimators();
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

        RefreshTurnTickers();
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
        RefreshTurnTickers();
        m_ActiveTurnBar.gameObject.SetActive(false);
        m_LargeActiveTurnBar.gameObject.SetActive(false);
    }
    private void OnTurnStarted(TurnStartedEvent e)
    {
        HideOneTicker(e.Unit);
        SnapTurnBar(e.Unit);
    }
    private void OnUnitMoved(UnitMovedEvent e)
    {
        RefreshNikkeSlots();
        RefreshTurnTickers();
    }
    private void OnBattleStarted(BattleStartedEvent e)
    {
        // 전체 숨기기
        HideAllTickers();
        HideEnemyTargetHighlights();
        HideEnemySkillName();

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
            RefreshHpBar(e.Nikkes[i]);
            int nikkeIndex = i;
            SetupTooltipTrigger(m_NikkeHpBars[i].gameObject, (sb) =>
            {
                CombatUnit u = m_CombatStateMachine.PositionSystem.GetUnit(CombatUnitType.Nikke, nikkeIndex);
                if (u == null) return;
                sb.Append("<color=#BF1313>체력: ").Append(u.CurrentHp).Append(" / </color>").Append(u.MaxHp)
                  .Append('\n')
                  .Append("<color=white>에블라: ").Append(u.Ebla).Append(" / 200</color>");
            },new Vector2(0, 40));
        }

        for (int i = 0; i < e.Enemies.Count; ++i)
        {
            CombatUnit enemy = e.Enemies[i];
            if (enemy.SlotSize == 2)
            {
                int largeIndex = enemy.SlotIndex;
                m_LargeEnemyHpBars[largeIndex].gameObject.SetActive(true);
            }
            else
            {
                m_EnemyHpBars[enemy.SlotIndex].gameObject.SetActive(true);
                m_EnemyNames[enemy.SlotIndex].gameObject.SetActive(true);
                m_EnemyNames[enemy.SlotIndex].text = enemy.UnitName;
            }

            RefreshHpBar(enemy);
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
            },new Vector2(0, -25));
        }
        RefreshTurnOrder();
        ShowAllTickersAnimated();
        m_ActiveTurnBar.gameObject.SetActive(false);
        m_LargeActiveTurnBar.gameObject.SetActive(false);
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

    private void OnRoundStarted(RoundStartedEvent e)
    {
        m_PendingRound = e.Round;
        HideAllTickers();
        ShowAllTickersAnimated();
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
    private void SnapTurnBar(CombatUnit unit)
    {
        if (unit == null)
            return;

        bool isLarge = unit.UnitType == CombatUnitType.Enemy && unit.SlotSize == 2;

        m_ActiveTurnBar.gameObject.SetActive(!isLarge);
        m_LargeActiveTurnBar.gameObject.SetActive(isLarge);

        Image bar = isLarge ? m_LargeActiveTurnBar : m_ActiveTurnBar;
        RectTransform anchor = null;

        if (unit.UnitType == CombatUnitType.Nikke)
            anchor = m_NikkeBarAnchor[unit.SlotIndex];
        else if (isLarge)
            anchor = m_LargeEnemyBarAnchors[unit.SlotIndex];
        else
            anchor = m_EnemyBarAnchor[unit.SlotIndex];

        if (anchor == null)
            return;

        bar.rectTransform.position = anchor.position;

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
            Slider bar = unit.SlotSize == 2 ? m_LargeEnemyHpBars[unit.SlotIndex] : m_EnemyHpBars[unit.SlotIndex];
            if (unit.State == UnitState.Corpse)
                bar.value = (float)unit.CurrentHp / Mathf.Max(unit.EnemyData.CorpseHp, 1);
            else
                bar.value = (float)unit.CurrentHp / unit.MaxHp;
        }
        RefreshStatusIcons(unit);
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
        m_TurnOrderText.SetText(m_TurnOrderBuilder);
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
                m_NikkeNames[i].text = unit.UnitName;
                RefreshHpBar(unit);
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
            }
            else if (unit.SlotSize == 2)
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
        // Large HP바 갱신
        for (int i = 0; i < m_LargeEnemyHpBars.Length; ++i)
        {
            CombatUnit unit = m_CombatStateMachine.PositionSystem.GetUnit(CombatUnitType.Enemy, i);
            if (unit != null && unit.SlotSize == 2 && unit.SlotIndex == i)
            {
                m_LargeEnemyHpBars[i].gameObject.SetActive(true);
                RefreshHpBar(unit);
            }
            else
            {
                m_LargeEnemyHpBars[i].gameObject.SetActive(false);
                m_LargeEnemyStatusIcons[i].Clear();
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

    // ticker
    private void CacheTickerAnimators()
    {
        for (int i = 0; i < m_NikkeTurnTickerGroups.Length; ++i)
        {
            m_NikkeTurnTickerGroups[i].Animators = new Animator[m_NikkeTurnTickerGroups[i].Tickers.Length];
            for (int j = 0; j < m_NikkeTurnTickerGroups[i].Tickers.Length; ++j)
                m_NikkeTurnTickerGroups[i].Animators[j] = m_NikkeTurnTickerGroups[i].Tickers[j].GetComponent<Animator>();
        }
        for (int i = 0; i < m_EnemyTurnTickerGroups.Length; ++i)
        {
            m_EnemyTurnTickerGroups[i].Animators = new Animator[m_EnemyTurnTickerGroups[i].Tickers.Length];
            for (int j = 0; j < m_EnemyTurnTickerGroups[i].Tickers.Length; ++j)
                m_EnemyTurnTickerGroups[i].Animators[j] = m_EnemyTurnTickerGroups[i].Tickers[j].GetComponent<Animator>();
        }
        for (int i = 0; i < m_LargeEnemyTickerGroups.Length; ++i)
        {
            m_LargeEnemyTickerGroups[i].Animators = new Animator[m_LargeEnemyTickerGroups[i].Tickers.Length];
            for (int j = 0; j < m_LargeEnemyTickerGroups[i].Tickers.Length; ++j)
                m_LargeEnemyTickerGroups[i].Animators[j] = m_LargeEnemyTickerGroups[i].Tickers[j].GetComponent<Animator>();
        }
    }

    private void SetTickerCount(CombatUnit unit, int count)
    {
        if (unit == null)
            return;

        TickerGroup group = GetTickerGroup(unit);
        for (int i = 0; i < group.Tickers.Length; ++i)
        {
            bool shouldBeActive = i < count;
            if (group.Tickers[i].gameObject.activeSelf != shouldBeActive)
            {
                if (shouldBeActive)
                {
                    if (unit == m_CombatStateMachine.ActiveUnit)
                        continue;
                    group.Tickers[i].gameObject.SetActive(true);
                    Animator anim = group.Animators[i];
                    if (anim != null)
                        anim.enabled = false;
                }
                else
                    group.Tickers[i].gameObject.SetActive(false);
            }
        }
    }


    private void RefreshTurnTickers()
    {
        HideAllTickers();

        IReadOnlyList<CombatUnit> order = m_CombatStateMachine.TurnOrder;
        if (order == null)
            return;
        int currentIndex = m_CombatStateMachine.CurrentTurnIndex;
        int startIndex = currentIndex + 1; // 현재 유닛 슬롯은 이미 소비됨

        m_TickerCountCache.Clear();
        for (int i = startIndex; i < order.Count; ++i)
        {
            CombatUnit unit = order[i];
            if (!unit.IsAlive)
                continue;
            if (m_TickerCountCache.ContainsKey(unit))
                m_TickerCountCache[unit]++;
            else
                m_TickerCountCache[unit] = 1;
        }

        foreach (KeyValuePair<CombatUnit, int> pair in m_TickerCountCache)
            SetTickerCount(pair.Key, pair.Value);
    }

    private void HideAllTickers()
    {
        for (int i = 0; i < m_NikkeTurnTickerGroups.Length; ++i)
        {
            for (int j = 0; j < m_NikkeTurnTickerGroups[i].Tickers.Length; ++j)
                m_NikkeTurnTickerGroups[i].Tickers[j].gameObject.SetActive(false);
        }
        for (int i = 0; i < m_EnemyTurnTickerGroups.Length; ++i)
        {
            for (int j = 0; j < m_EnemyTurnTickerGroups[i].Tickers.Length; ++j)
                m_EnemyTurnTickerGroups[i].Tickers[j].gameObject.SetActive(false);
        }
        for (int i = 0; i < m_LargeEnemyTickerGroups.Length; ++i)
        {
            for (int j = 0; j < m_LargeEnemyTickerGroups[i].Tickers.Length; ++j)
                m_LargeEnemyTickerGroups[i].Tickers[j].gameObject.SetActive(false);
        }
    }

    private void HideOneTicker(CombatUnit unit)
    {
        TickerGroup group = GetTickerGroup(unit);
        for (int i = group.Tickers.Length - 1; i >= 0; --i)
        {
            if (group.Tickers[i].gameObject.activeSelf)
            {
                group.Tickers[i].gameObject.SetActive(false);
                return;
            }
        }
    }

    private void ShowAllTickersAnimated()
    {
        IReadOnlyList<CombatUnit> order = m_CombatStateMachine.TurnOrder;
        if (order == null)
            return;
        m_TickerCountCache.Clear();
        for (int i = 0; i < order.Count; ++i)
        {
            CombatUnit unit = order[i];
            if (!unit.IsAlive)
                continue;
            if (m_TickerCountCache.ContainsKey(unit))
                m_TickerCountCache[unit]++;
            else
                m_TickerCountCache[unit] = 1;
        }
        foreach (KeyValuePair<CombatUnit, int> pair in m_TickerCountCache)
            ShowTickersAnimated(pair.Key, pair.Value);
        m_RoundBg.SetActive(false);
        m_RoundBg.SetActive(true);
        StartCoroutine(TickerAnimTimer());

    }
    private IEnumerator TickerAnimTimer()
    {
        IsTickerAnimating = true;
        yield return new WaitForSeconds(m_TickerAnimDuration);
        IsTickerAnimating = false;
    }

    private void ShowTickersAnimated(CombatUnit unit, int count)
    {
        TickerGroup group = GetTickerGroup(unit);
        for (int i = 0; i < group.Tickers.Length; ++i)
        {
            if (i < count)
            {
                group.Tickers[i].gameObject.SetActive(true);
                Animator anim = group.Animators[i];
                if (anim != null)
                    anim.enabled = true;
            }
            else
                group.Tickers[i].gameObject.SetActive(false);
        }
    }
    private TickerGroup GetTickerGroup(CombatUnit unit)
    {
        if (unit.UnitType == CombatUnitType.Nikke)
            return m_NikkeTurnTickerGroups[unit.SlotIndex];
        else if (unit.SlotSize == 2)
            return m_LargeEnemyTickerGroups[unit.SlotIndex];
        else
            return m_EnemyTurnTickerGroups[unit.SlotIndex];
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


}
