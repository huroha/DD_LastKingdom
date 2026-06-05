using UnityEngine;
using System;
using UnityEngine.UI;
using System.Collections.Generic;


public class QuirkPhase : SettlementPhase
{
    [SerializeField] private GameObject m_Root;
    [SerializeField] private Transform m_EntryRoot;
    [SerializeField] private DeadNikkeEntry m_EntryPrefab;
    [SerializeField] private Button m_NextButton;

    private Action m_OnComplete;

    public override void Begin(Action onComplete)
    {
        m_OnComplete = onComplete;
        m_Root.SetActive(true);

        foreach (Transform child in m_EntryRoot) Destroy(child.gameObject);

        IReadOnlyList<NikkeInstance> dead = ExpeditionManager.Instance.DeadNikkes;
        IReadOnlyList<NikkeInstance> party = ExpeditionManager.Instance.Party;
        for (int i = 0; i < party.Count; ++i)
        {
            DeadNikkeEntry entry = Instantiate(m_EntryPrefab, m_EntryRoot);
            entry.Bind(party[i], IsDead(dead, party[i]));
        }

        m_NextButton.gameObject.SetActive(true);
        m_NextButton.onClick.AddListener(OnNextClicked);
    }
    private void OnNextClicked()
    {
        m_NextButton.onClick.RemoveListener(OnNextClicked);
        m_OnComplete?.Invoke();
    }
    private bool IsDead(IReadOnlyList<NikkeInstance> dead, NikkeInstance nikke)
    {
        for (int i = 0; i < dead.Count; ++i)
            if (dead[i] == nikke) return true;
        return false;
    }
}
