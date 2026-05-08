using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
public class SquadSelectPanel : MonoBehaviour
{
    [Header("Reference")]
    [SerializeField] private NikkeDetailPanel m_DetailPanel;

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

    private EncounterData m_SelectedEncounter;

    private const string MSG_INSUFFICIENT_PARTY = "편성된 니케의 수가 부족합니다.그래도 출정하시겠습니까?";

    private void Awake()
    {
        for (int i = 0; i < m_PartySlots.Length; ++i)
            m_PartySlots[i].Init(i);
    }
    private void OnEnable()
    {
        for (int i = 0; i < m_PartySlots.Length; ++i)
            m_PartySlots[i].OnSlotChanged += OnSlotChanged;

        m_EncounterListView.OnEncounterSelected += OnEncounterSelected;
        m_DepartButton.onClick.AddListener(OnDepartClicked);

        m_RosterView.Bind(RosterManager.Instance.Roster);
        m_EncounterListView.Bind(m_Dungeons);
        m_RosterView.OnCardClicked += OnNikkeCardClicked;
        m_RosterView.OnCardRightClicked += OnNikkeCardRightClicked;
        RefreshDepartButton();
    }
    private void OnDisable()
    {
        for (int i = 0; i < m_PartySlots.Length; ++i)
            m_PartySlots[i].OnSlotChanged -= OnSlotChanged;

        m_EncounterListView.OnEncounterSelected -= OnEncounterSelected;
        m_DepartButton.onClick.RemoveListener(OnDepartClicked);
        m_RosterView.OnCardClicked -= OnNikkeCardClicked;
        m_RosterView.OnCardRightClicked -= OnNikkeCardRightClicked;
    }
    private void OnNikkeCardRightClicked(NikkeCardView card)
    {
        m_DetailPanel.Show(card.BoundInstance, RosterManager.Instance.Roster);
    }


    private void OnEncounterSelected(EncounterData data)
    {
        m_SelectedEncounter = data;
        m_EncounterPreview.Show(data);
        RefreshDepartButton();
    }
    private void OnSlotChanged()
    {
        RefreshDepartButton();
    }
    private void OnNikkeCardClicked(NikkeCardView card)
    {
        if (card.CurrentSlot != null)
        {
            card.CurrentSlot.ClearSlot();
            return;
        }
        int idx = GetFirstEmptySlotIndex();
        if (idx == -1) return;
        m_PartySlots[idx].AssignCard(card);
    }
    private void OnDepartClicked()
    {
        if (GetFilledSlotCount() < m_PartySlots.Length)
        {
            m_ConfirmPopup.Show(MSG_INSUFFICIENT_PARTY, Depart);
        }
        else
            Depart();
    }
    private void Depart()
    {
        List<NikkeInstance> party = new List<NikkeInstance>(m_PartySlots.Length);
        for (int i=0; i< m_PartySlots.Length; ++i)
        {
            if (m_PartySlots[i].AssignedInstance != null)
                party.Add(m_PartySlots[i].AssignedInstance);
        }
        ExpeditionManager.Instance.BeginExpedition(party, m_SelectedEncounter);
        GameManager.Instance.ChangeState(GameState.Combat);
    }

    private void RefreshDepartButton()
    {
        m_DepartButton.interactable = GetFilledSlotCount() > 0 && m_SelectedEncounter != null;
    }
    private int GetFilledSlotCount()
    {
        int count = 0;
        for (int i=0; i< m_PartySlots.Length; ++i)
        {
            if (!m_PartySlots[i].IsEmpty) ++count;
        }
        return count;
    }
    private int GetFirstEmptySlotIndex()
    {
        for (int i=0; i< m_PartySlots.Length; ++i)
        {
            if (m_PartySlots[i].IsEmpty) return i;
        }
        return -1;
    }
}
