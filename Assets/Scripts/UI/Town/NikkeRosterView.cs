using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class NikkeRosterView : MonoBehaviour, IDropHandler
{
    [SerializeField] private Transform m_CardContainer;
    [SerializeField] private NikkeCardView m_CardPrefab;

    private List<NikkeCardView> m_Cards = new List<NikkeCardView>();

    public delegate void CardClickHandler(NikkeCardView card);
    public event CardClickHandler OnCardClicked;
    public void Bind(IReadOnlyList<NikkeInstance> roster)
    {
        for (int i= m_CardContainer.childCount - 1; i>= 0; --i)
            Destroy(m_CardContainer.GetChild(i).gameObject);
        m_Cards.Clear();

        for (int i=0; i< roster.Count; ++i)
        {
            NikkeCardView card = Instantiate(m_CardPrefab, m_CardContainer);
            card.Bind(roster[i]);
            m_Cards.Add(card);
            card.OnClicked += c => OnCardClicked?.Invoke(c);
        }
    }
    public void ReturnCard(NikkeCardView card)
    {
        card.transform.SetParent(m_CardContainer);
        card.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
    }
    public void OnDrop(PointerEventData e)
    {
        if (e.pointerDrag == null) return;
        NikkeCardView card = e.pointerDrag.GetComponent<NikkeCardView>();
        if (card == null) return;
        ReturnCard(card);
    }
}
