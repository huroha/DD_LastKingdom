using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
public class SquadSelectPanel : MonoBehaviour
{
    [Header("Reference")]
    [SerializeField] private TownNikkePanel m_DetailPanel;

    [Header("Views")]
    [SerializeField] private NikkeRosterView m_RosterView;
    [SerializeField] private EncounterListView m_EncounterListView;
    [SerializeField] private EncounterPreview m_EncounterPreview;
    [SerializeField] private PartySlotView[] m_PartySlots;

    [Header("Sort")]
    [SerializeField] private Button m_SortLevelButton;
    [SerializeField] private Button m_SortNameButton;
    [SerializeField] private Button m_SortClassButton;
    private enum SortKey { None, Level, Name, Class }
    private SortKey m_SortKey = SortKey.None;
    private bool m_SortAsc = true;
    private readonly List<NikkeInstance> m_SortBuffer = new List<NikkeInstance>();

    [Header("Data")]
    [SerializeField] private DungeonData[] m_Dungeons;

    [Header("UI")]
    [SerializeField] private ConfirmPopup m_ConfirmPopup;
    [SerializeField] private Button m_DepartButton;
    [SerializeField] private Image m_DepartHoverOverlay;

    private EncounterData m_SelectedEncounter;
    private PartyAssignment m_PartyAssignment;

    private const string MSG_INSUFFICIENT_PARTY = "편성된 니케의 수가 부족합니다.\n그래도 출정하시겠습니까?";
    private void OnSortLevel() => OnSortClicked(SortKey.Level);
    private void OnSortName() => OnSortClicked(SortKey.Name);
    private void OnSortClass() => OnSortClicked(SortKey.Class);
    private static int CompareName(NikkeInstance a, NikkeInstance b)
     => string.Compare(a.DisplayName, b.DisplayName, System.StringComparison.CurrentCulture);
    private void Awake()
    {
        for (int i = 0; i < m_PartySlots.Length; ++i)
            m_PartySlots[i].Init(i);
        m_PartyAssignment = new PartyAssignment(m_PartySlots.Length);
        m_DepartHoverOverlay.gameObject.SetActive(false);
        AddDepartHoverListeners();
    }
    private void OnEnable()
    {
        m_PartyAssignment.OnChanged += RefreshAllViews;

        for (int i = 0; i < m_PartySlots.Length; ++i)
        {
            m_PartySlots[i].OnDroppedHere += HandleDroppedHere;
            m_PartySlots[i].OnSwapRequested += HandleSwap;
            m_PartySlots[i].OnClearRequested += HandleClearSlot;
            m_PartySlots[i].OnRightClicked += HandleRightClickSlot;
        }

        m_EncounterListView.OnEncounterSelected += OnEncounterSelected;
        m_DepartButton.onClick.AddListener(OnDepartClicked);

        m_EncounterListView.Bind(m_Dungeons);
        m_RosterView.OnCardClicked += OnNikkeCardClicked;
        m_RosterView.OnCardRightClicked += OnNikkeCardRightClicked;
        RosterManager.Instance.OnRosterChanged += RebindRoster;
        m_SortLevelButton.onClick.AddListener(OnSortLevel);
        m_SortNameButton.onClick.AddListener(OnSortName);
        m_SortClassButton.onClick.AddListener(OnSortClass);
        BindRosterCards();
        RefreshAllViews();
    }
    private void OnDisable()
    {
        m_PartyAssignment.OnChanged -= RefreshAllViews;
        if (RosterManager.Instance != null) RosterManager.Instance.OnRosterChanged -= RebindRoster;

        for (int i = 0; i < m_PartySlots.Length; ++i)
        {
            m_PartySlots[i].OnDroppedHere -= HandleDroppedHere;
            m_PartySlots[i].OnSwapRequested -= HandleSwap;
            m_PartySlots[i].OnClearRequested -= HandleClearSlot;
            m_PartySlots[i].OnRightClicked -= HandleRightClickSlot;
        }
        m_EncounterListView.OnEncounterSelected -= OnEncounterSelected;
        m_DepartButton.onClick.RemoveListener(OnDepartClicked);
        m_RosterView.OnCardClicked -= OnNikkeCardClicked;
        m_RosterView.OnCardRightClicked -= OnNikkeCardRightClicked;
        m_SortLevelButton.onClick.RemoveListener(OnSortLevel);
        m_SortNameButton.onClick.RemoveListener(OnSortName);
        m_SortClassButton.onClick.RemoveListener(OnSortClass);

        for (int i = 0; i < m_RosterView.Cards.Count; ++i)
            m_RosterView.Cards[i].OnDragBegan -= HandleCardDragBegan;

        m_DepartHoverOverlay.gameObject.SetActive(false);
    }
    private void BindRosterCards()
    {
        BuildSortedRoster();
        m_RosterView.Bind(m_SortBuffer);              // ← Roster 대신 정렬 버퍼
        for (int i = 0; i < m_RosterView.Cards.Count; ++i)
            m_RosterView.Cards[i].OnDragBegan += HandleCardDragBegan;
    }
    private void OnSortClicked(SortKey key)
    {
        if (m_SortKey == key) m_SortAsc = !m_SortAsc;   // 같은 기준 → 방향 반전
        else { m_SortKey = key; m_SortAsc = true; }     // 다른 기준 → 오름차순
        RebindRoster();                                 // 재정렬 + 재바인딩
    }
    private void BuildSortedRoster()
    {
        m_SortBuffer.Clear();
        IReadOnlyList<NikkeInstance> roster = RosterManager.Instance.Roster;
        for (int i = 0; i < roster.Count; ++i) m_SortBuffer.Add(roster[i]);
        if (m_SortKey != SortKey.None) m_SortBuffer.Sort(CompareNikke);
    }

    private int CompareNikke(NikkeInstance a, NikkeInstance b)
    {
        // 1) 출전 그룹 우선 — 방향 무관, 항상 최상단
        bool ea = m_PartyAssignment.IsAssigned(a);
        bool eb = m_PartyAssignment.IsAssigned(b);
        if (ea != eb) return ea ? -1 : 1;

        // 2) 같은 그룹 내에서는 기준 비교 (방향 적용)
        int result;
        switch (m_SortKey)
        {
            case SortKey.Level:
                result = a.Level.CompareTo(b.Level);
                if (result == 0) result = CompareName(a, b);
                break;
            case SortKey.Name:
                result = CompareName(a, b);
                break;
            case SortKey.Class:
                result = ((int)a.Data.NikkeClass).CompareTo((int)b.Data.NikkeClass);
                if (result == 0) result = CompareName(a, b);
                break;
            default:
                result = 0;
                break;
        }
        return m_SortAsc ? result : -result;
    }


    private void HandleDroppedHere(int slotIdx, NikkeInstance inst) => m_PartyAssignment.Assign(slotIdx, inst);
    private void HandleSwap(int scrIdx, int tgtIdx) => m_PartyAssignment.Swap(scrIdx, tgtIdx);
    private void HandleClearSlot(int slotIdx) => m_PartyAssignment.Clear(slotIdx);
    private void HandleCardDragBegan(NikkeCardView card)
    {
        int idx = m_PartyAssignment.IndexOf(card.BoundInstance);
        if (idx >= 0) m_PartyAssignment.Clear(idx);
    }
    private void RefreshAllViews()
    {
        for (int i = 0; i < m_PartySlots.Length; ++i)
            m_PartySlots[i].Render(m_PartyAssignment.Get(i));

        for (int i = 0; i < m_RosterView.Cards.Count; ++i)
            m_RosterView.Cards[i].SetEmbarked(m_PartyAssignment.IsAssigned(m_RosterView.Cards[i].BoundInstance));
        RefreshDepartButton();
    }
    private void OnNikkeCardRightClicked(NikkeCardView card)
    {
        m_DetailPanel.Show(card.BoundInstance, m_SortBuffer);
    }
    private void HandleRightClickSlot(NikkeInstance instance)
    {
        List<NikkeInstance> partyList = new List<NikkeInstance>(m_PartyAssignment.SlotCount);
        for (int i = 0; i < m_PartyAssignment.SlotCount; ++i)
        {
            NikkeInstance inst = m_PartyAssignment.Get(i);
            if (inst != null) partyList.Add(inst);
        }
        m_DetailPanel.Show(instance, partyList, true);
    }

    private void OnEncounterSelected(EncounterData data)
    {
        m_SelectedEncounter = data;
        m_EncounterPreview.Show(data);
        RefreshDepartButton();
    }
    private void OnNikkeCardClicked(NikkeCardView card)
    {
        int existingIdx = m_PartyAssignment.IndexOf(card.BoundInstance);
        if (existingIdx >= 0)
        {
            m_PartyAssignment.Clear(existingIdx);  // 이미 배치됨 → 해제
            return;
        }
        int emptyIdx = m_PartyAssignment.FindFirstEmpty();
        if (emptyIdx >= 0) m_PartyAssignment.Assign(emptyIdx, card.BoundInstance);
    }
    private void OnDepartClicked()
    {
        if (m_PartyAssignment.FilledCount() < m_PartySlots.Length)
            m_ConfirmPopup.Show(MSG_INSUFFICIENT_PARTY, Depart);
        else
            Depart();
    }
    private void Depart()
    {
        List<NikkeInstance> party = new List<NikkeInstance>(m_PartyAssignment.SlotCount);
        for (int i = 0; i < m_PartyAssignment.SlotCount; ++i)
        {
            NikkeInstance inst = m_PartyAssignment.Get(i);
            if (inst != null) party.Add(inst);
        }
        ExpeditionManager.Instance.BeginExpedition(party, m_SelectedEncounter, FindDungeon(m_SelectedEncounter));
        GameManager.Instance.ChangeState(GameState.Combat);
    }

    private void RefreshDepartButton()
    {
        m_DepartButton.interactable = m_PartyAssignment.FilledCount() > 0 && m_SelectedEncounter != null;
    }
    private void AddDepartHoverListeners()
    {
        UIEventUtil.AddHover(m_DepartButton,
    () => m_DepartHoverOverlay.gameObject.SetActive(true),
    () => m_DepartHoverOverlay.gameObject.SetActive(false));
    }
    private void RebindRoster()
    {
        BindRosterCards();
        PruneParty();
        RefreshAllViews();
    }

    private void PruneParty()
    {
        IReadOnlyList<NikkeInstance> roster = RosterManager.Instance.Roster;
        for (int i = 0; i < m_PartyAssignment.SlotCount; ++i)
        {
            NikkeInstance inst = m_PartyAssignment.Get(i);
            if (inst == null) continue;
            bool found = false;
            for (int j = 0; j < roster.Count; ++j)
                if (roster[j] == inst) { found = true; break; }
            if (!found) m_PartyAssignment.Clear(i);
        }
    }
    private DungeonData FindDungeon(EncounterData enc)
    {
        if (enc == null) return null;
        for (int i=0; i< m_Dungeons.Length; ++i)
        {
            IReadOnlyList<EncounterData> list = m_Dungeons[i].Encounters;
            for (int j =0; j < list.Count; ++j)
                if (list[j] == enc) return m_Dungeons[i];
        }
        return null;
    }
}
