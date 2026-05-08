using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class DungeonSectionView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI m_TypeText;
    [SerializeField] private Transform m_SlotA;
    [SerializeField] private Transform m_SlotB;
    [SerializeField] private DungeonType m_DungeonType;

    public delegate void CardClickedHandler(EncounterCardView card, EncounterData data);
    public event CardClickedHandler OnCardClicked;
    public DungeonType DungeonType => m_DungeonType;

    public void Bind(DungeonData data, EncounterCardView cardPrefab)
    {
        m_SlotA.DestroyChildren();
        m_SlotB.DestroyChildren();
        m_TypeText.SetText(LabelText.GetDungeonTypeLabel(data.DungeonType));

        IReadOnlyList<EncounterData> encounters = data.Encounters;
        for (int i=0; i< encounters.Count; ++i)
        {
            if (encounters[i] == null) continue;
            Transform parent = i < 4 ? m_SlotA : m_SlotB;
            EncounterCardView card = Instantiate(cardPrefab, parent);
            card.Bind(encounters[i]);
            card.OnClicked += OnEncounterCardClicked;
        }
    }
    private void OnEncounterCardClicked(EncounterCardView card, EncounterData data) => OnCardClicked?.Invoke(card, data);

}
