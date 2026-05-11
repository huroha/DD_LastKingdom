using UnityEngine;
using System.Collections.Generic;

public class EncounterListView : MonoBehaviour
{
    [SerializeField] private EncounterCardView m_CardPrefab;
    [SerializeField] private DungeonSectionView[] m_Sections;

    private EncounterCardView m_SelectedCard;

    public delegate void EncounterSelectedHandler(EncounterData data);
    public event EncounterSelectedHandler OnEncounterSelected;

    public void Bind(DungeonData[] dungeons)
    {
        m_SelectedCard = null;
        EncounterCardView ruinsFirstCard = null;
        if (dungeons.Length > m_Sections.Length)
            Debug.LogWarning($"[EncounterListView] 던전 수({dungeons.Length})가 섹션 수({m_Sections.Length})를 초과합니다.");
        int count = Mathf.Min(dungeons.Length, m_Sections.Length);
        for (int i = 0; i < count; ++i)
        {
            if (dungeons[i] == null) continue;
            EncounterCardView firstCard = m_Sections[i].Bind(dungeons[i], m_CardPrefab);
            m_Sections[i].OnCardClicked += OnCardClicked;
            if (i == 0) ruinsFirstCard = firstCard;
        }
        if (ruinsFirstCard != null && dungeons[0].Encounters.Count > 0)
            OnCardClicked(ruinsFirstCard, dungeons[0].Encounters[0]);
    }
    private void OnCardClicked(EncounterCardView card, EncounterData data)
    {
        if (card == m_SelectedCard)
        {
            card.ShuffleRotation();
            return;
        }
        if (m_SelectedCard != null)
            m_SelectedCard.SetSelected(false);
        m_SelectedCard = card;
        m_SelectedCard.SetSelected(true);
        OnEncounterSelected?.Invoke(data);
    }


}
