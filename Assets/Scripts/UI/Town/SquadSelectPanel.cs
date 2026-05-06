using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
public class SquadSelectPanel : MonoBehaviour
{
    [Header("Views")]
    [SerializeField] private NikkeRosterView m_RosterView;
    [SerializeField] private EncounterListView m_EncounterListView;
    [SerializeField] private EncounterPreview m_EncounterPreview;
    [SerializeField] private PartySlotView[] m_PartySlots; // 길이 4


    [Header("Data")]
    [SerializeField] private EncounterData[] m_AvailableEncounters;

    [Header("UI")]
    [SerializeField] private ConfirmPopup m_ConfirmPopup;
    [SerializeField] private Button m_DepartButton;

    private EncounterData m_SelectedEncounter;

    private void OnEnable()
    {
        for (int i = 0; i < m_PartySlots.Length; ++i)
            m_PartySlots[i].Init(i);
        for (int i = 0; i < m_PartySlots.Length; ++i)
            m_PartySlots[i].OnSlotChanged += OnSlotChanged;

        m_EncounterListView.OnEncounterSelected += OnEncounterSelected;
        m_DepartButton.onClick.AddListener(OnDepartClicked);

        m_RosterView.Bind(RosterManager.Instance.Roster);
        m_EncounterListView.Bind(m_AvailableEncounters);
        m_RosterView.OnCardClicked += OnNikkeCardClicked;
        RefreshDepartButton();
    }
    private void OnDisable()
    {
        for (int i = 0; i < m_PartySlots.Length; ++i)
            m_PartySlots[i].OnSlotChanged -= OnSlotChanged;

        m_EncounterListView.OnEncounterSelected -= OnEncounterSelected;
        m_DepartButton.onClick.RemoveListener(OnDepartClicked);
        m_RosterView.OnCardClicked -= OnNikkeCardClicked;
    }

    private void OnEncounterSelected(EncounterData data)
    {
        m_SelectedEncounter = data;
        m_EncounterPreview.Show(data);
        RefreshDepartButton();
    }
    private void OnSlotChanged(int slotIndex, NikkeInstance instance, NikkeCardView card)
    {
        if (instance == null && card != null)
            m_RosterView.ReturnCard(card);

        RefreshDepartButton();
    }
    private void OnNikkeCardClicked(NikkeCardView card)
    {
        int idx = GetFirstEmptySlotIndex();
        if (idx == -1) return;
        m_PartySlots[idx].AssignCard(card);
    }
    private void OnDepartClicked()
    {
        if (GetFilledSlotCount() < 4)
        {
            m_ConfirmPopup.Show("인원이 부족합니다. 출정하시겠습니까?", Depart);
        }
        else
            Depart();
    }
    private void Depart()
    {
        List<NikkeInstance> party = new List<NikkeInstance>();
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
