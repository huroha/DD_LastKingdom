using UnityEngine;
using System.Collections.Generic;

public class NikkeRosterView : MonoBehaviour
{
    [SerializeField] private Transform m_CardContainer;
    [SerializeField] private NikkeCardView m_CardPrefab;

    private List<NikkeCardView> m_Cards = new List<NikkeCardView>();
    public IReadOnlyList<NikkeCardView> Cards => m_Cards;

    public delegate void CardClickHandler(NikkeCardView card);
    public event CardClickHandler OnCardClicked;
    public event CardClickHandler OnCardRightClicked;

    private void ForwardClick(NikkeCardView c) => OnCardClicked?.Invoke(c);
    private void ForwardRightClick(NikkeCardView c) => OnCardRightClicked?.Invoke(c);

    public void Bind(IReadOnlyList<NikkeInstance> roster)
    {
        m_CardContainer.DestroyChildren();
        m_Cards.Clear();

        for (int i=0; i< roster.Count; ++i)
        {
            NikkeCardView card = Instantiate(m_CardPrefab, m_CardContainer);
            card.Bind(roster[i]);
            m_Cards.Add(card);
            card.OnClicked += ForwardClick;
            card.OnRightClicked += ForwardRightClick;
        }
    }
}
