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


    [Header("Data")]
    [SerializeField] private DungeonData[] m_Dungeons;

    [Header("UI")]
    [SerializeField] private ConfirmPopup m_ConfirmPopup;
    [SerializeField] private Button m_DepartButton;
    [SerializeField] private Image m_DepartHoverOverlay;

    private EncounterData m_SelectedEncounter;
    private PartyAssignment m_PartyAssignment;

    private const string MSG_INSUFFICIENT_PARTY = "편성된 니케의 수가 부족합니다.\n그래도 출정하시겠습니까?";

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

        for (int i=0; i<m_PartySlots.Length; ++i)
        {
            m_PartySlots[i].OnDroppedHere += HandleDroppedHere;           
            m_PartySlots[i].OnSwapRequested += HandleSwap;           
            m_PartySlots[i].OnClearRequested += HandleClearSlot;           
            m_PartySlots[i].OnRightClicked += HandleRightClickSlot;           
        }

        m_EncounterListView.OnEncounterSelected += OnEncounterSelected;
        m_DepartButton.onClick.AddListener(OnDepartClicked);

        m_EncounterListView.Bind(m_Dungeons);
        m_RosterView.Bind(RosterManager.Instance.Roster);
        m_RosterView.OnCardClicked += OnNikkeCardClicked;
        m_RosterView.OnCardRightClicked += OnNikkeCardRightClicked;

        for (int i = 0; i < m_RosterView.Cards.Count; ++i)
            m_RosterView.Cards[i].OnDragBegan += HandleCardDragBegan;
        RefreshAllViews();
    }
    private void OnDisable()
    {
        m_PartyAssignment.OnChanged -= RefreshAllViews;

        for (int i=0; i< m_PartySlots.Length; ++i)
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

        for (int i = 0; i < m_RosterView.Cards.Count; ++i)
            m_RosterView.Cards[i].OnDragBegan -= HandleCardDragBegan;

        m_DepartHoverOverlay.gameObject.SetActive(false);
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
        m_DetailPanel.Show(card.BoundInstance, RosterManager.Instance.Roster);
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
        ExpeditionManager.Instance.BeginExpedition(party, m_SelectedEncounter);
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
}
