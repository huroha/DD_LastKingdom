using UnityEngine;


public class EncounterListView : MonoBehaviour
{
    [SerializeField] private Transform m_CardContainer;
    [SerializeField] private EncounterCardView m_CardPrefab;

    public delegate void EncounterSelectedHandler(EncounterData data);
    public event EncounterSelectedHandler OnEncounterSelected;

    public void Bind(EncounterData[] encounters)
    {
        for (int i = m_CardContainer.childCount - 1; i >= 0; --i)
            Destroy(m_CardContainer.GetChild(i).gameObject);

        for (int i = 0; i < encounters.Length; ++i)
        {
            if (encounters[i] == null) continue;
            EncounterCardView card = Instantiate(m_CardPrefab, m_CardContainer);
            card.Bind(encounters[i]);
            card.OnClicked += OnCardClicked;
        }
    }
    private void OnCardClicked(EncounterData data)
    {
        OnEncounterSelected?.Invoke(data);
    }


}
