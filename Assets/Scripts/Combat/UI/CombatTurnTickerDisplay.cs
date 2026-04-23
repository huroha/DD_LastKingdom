using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
public class CombatTurnTickerDisplay : MonoBehaviour
{
    [Header("Reference")]
    [SerializeField] private CombatStateMachine m_CombatStateMachine;

    [System.Serializable]
    private struct TickerGroup
    {
        public Image[] Tickers;     // ½½·Ô´ç ÃÖ´ë ActionsPerRound  °¹¼ö
        [System.NonSerialized] public Animator[] Animators;
    }
    [Header("Turn Tickers")]
    [SerializeField] private TickerGroup[] m_NikkeTurnTickerGroups;     // 4°³
    [SerializeField] private TickerGroup[] m_EnemyTurnTickerGroups;     // 4°³
    [SerializeField] private TickerGroup[] m_LargeEnemyTickerGroups;   // 3°³
    [SerializeField] private float m_TickerAnimDuration = 0.5f;

    [Header("RoundEvent")]
    [SerializeField] private GameObject m_RoundBg;

    private Dictionary<CombatUnit, int> m_TickerCountCache = new Dictionary<CombatUnit, int>(); // ÅÏ refresh¿ë

    public bool IsTickerAnimating { get; private set; }

    private void Awake()
    {
        CacheTickerAnimators();
    }


    private void CacheTickerAnimators()
    {
        for (int i = 0; i < m_NikkeTurnTickerGroups.Length; ++i)
        {
            m_NikkeTurnTickerGroups[i].Animators = new Animator[m_NikkeTurnTickerGroups[i].Tickers.Length];
            for (int j = 0; j < m_NikkeTurnTickerGroups[i].Tickers.Length; ++j)
                m_NikkeTurnTickerGroups[i].Animators[j] = m_NikkeTurnTickerGroups[i].Tickers[j].GetComponent<Animator>();
        }
        for (int i = 0; i < m_EnemyTurnTickerGroups.Length; ++i)
        {
            m_EnemyTurnTickerGroups[i].Animators = new Animator[m_EnemyTurnTickerGroups[i].Tickers.Length];
            for (int j = 0; j < m_EnemyTurnTickerGroups[i].Tickers.Length; ++j)
                m_EnemyTurnTickerGroups[i].Animators[j] = m_EnemyTurnTickerGroups[i].Tickers[j].GetComponent<Animator>();
        }
        for (int i = 0; i < m_LargeEnemyTickerGroups.Length; ++i)
        {
            m_LargeEnemyTickerGroups[i].Animators = new Animator[m_LargeEnemyTickerGroups[i].Tickers.Length];
            for (int j = 0; j < m_LargeEnemyTickerGroups[i].Tickers.Length; ++j)
                m_LargeEnemyTickerGroups[i].Animators[j] = m_LargeEnemyTickerGroups[i].Tickers[j].GetComponent<Animator>();
        }
    }

    private void SetTickerCount(CombatUnit unit, int count)
    {
        if (unit == null)
            return;

        TickerGroup group = GetTickerGroup(unit);
        for (int i = 0; i < group.Tickers.Length; ++i)
        {
            bool shouldBeActive = i < count;
            if (group.Tickers[i].gameObject.activeSelf != shouldBeActive)
            {
                if (shouldBeActive)
                {
                    if (unit == m_CombatStateMachine.ActiveUnit)
                        continue;
                    group.Tickers[i].gameObject.SetActive(true);
                    Animator anim = group.Animators[i];
                    if (anim != null)
                        anim.enabled = false;
                }
                else
                    group.Tickers[i].gameObject.SetActive(false);
            }
        }
    }


    public void RefreshTurnTickers()
    {
        HideAllTickers();

        IReadOnlyList<CombatUnit> order = m_CombatStateMachine.TurnOrder;
        if (order == null)
            return;
        int currentIndex = m_CombatStateMachine.CurrentTurnIndex;
        int startIndex = currentIndex + 1; // ÇöÀç À¯´Ö ½½·ÔÀº ÀÌ¹Ì ¼ÒºñµÊ

        m_TickerCountCache.Clear();
        for (int i = startIndex; i < order.Count; ++i)
        {
            CombatUnit unit = order[i];
            if (!unit.IsAlive)
                continue;
            if (m_TickerCountCache.ContainsKey(unit))
                m_TickerCountCache[unit]++;
            else
                m_TickerCountCache[unit] = 1;
        }

        foreach (KeyValuePair<CombatUnit, int> pair in m_TickerCountCache)
            SetTickerCount(pair.Key, pair.Value);
    }

    public void HideAllTickers()
    {
        for (int i = 0; i < m_NikkeTurnTickerGroups.Length; ++i)
        {
            for (int j = 0; j < m_NikkeTurnTickerGroups[i].Tickers.Length; ++j)
                m_NikkeTurnTickerGroups[i].Tickers[j].gameObject.SetActive(false);
        }
        for (int i = 0; i < m_EnemyTurnTickerGroups.Length; ++i)
        {
            for (int j = 0; j < m_EnemyTurnTickerGroups[i].Tickers.Length; ++j)
                m_EnemyTurnTickerGroups[i].Tickers[j].gameObject.SetActive(false);
        }
        for (int i = 0; i < m_LargeEnemyTickerGroups.Length; ++i)
        {
            for (int j = 0; j < m_LargeEnemyTickerGroups[i].Tickers.Length; ++j)
                m_LargeEnemyTickerGroups[i].Tickers[j].gameObject.SetActive(false);
        }
    }

    public void HideOneTicker(CombatUnit unit)
    {
        TickerGroup group = GetTickerGroup(unit);
        for (int i = group.Tickers.Length - 1; i >= 0; --i)
        {
            if (group.Tickers[i].gameObject.activeSelf)
            {
                group.Tickers[i].gameObject.SetActive(false);
                return;
            }
        }
    }

    public void ShowAllTickersAnimated()
    {
        IReadOnlyList<CombatUnit> order = m_CombatStateMachine.TurnOrder;
        if (order == null)
            return;
        m_TickerCountCache.Clear();
        for (int i = 0; i < order.Count; ++i)
        {
            CombatUnit unit = order[i];
            if (!unit.IsAlive)
                continue;
            if (m_TickerCountCache.ContainsKey(unit))
                m_TickerCountCache[unit]++;
            else
                m_TickerCountCache[unit] = 1;
        }
        foreach (KeyValuePair<CombatUnit, int> pair in m_TickerCountCache)
            ShowTickersAnimated(pair.Key, pair.Value);
        m_RoundBg.SetActive(false);
        m_RoundBg.SetActive(true);
        StartCoroutine(TickerAnimTimer());

    }
    private IEnumerator TickerAnimTimer()
    {
        IsTickerAnimating = true;
        yield return new WaitForSeconds(m_TickerAnimDuration);
        IsTickerAnimating = false;
    }

    private void ShowTickersAnimated(CombatUnit unit, int count)
    {
        TickerGroup group = GetTickerGroup(unit);
        for (int i = 0; i < group.Tickers.Length; ++i)
        {
            if (i < count)
            {
                group.Tickers[i].gameObject.SetActive(true);
                Animator anim = group.Animators[i];
                if (anim != null)
                    anim.enabled = true;
            }
            else
                group.Tickers[i].gameObject.SetActive(false);
        }
    }
    private TickerGroup GetTickerGroup(CombatUnit unit)
    {
        if (unit.UnitType == CombatUnitType.Nikke)
            return m_NikkeTurnTickerGroups[unit.SlotIndex];
        else if (unit.SlotSize == 2)
            return m_LargeEnemyTickerGroups[unit.SlotIndex];
        else
            return m_EnemyTurnTickerGroups[unit.SlotIndex];
    }
    public void HideUnitTickers(CombatUnit unit)
    {
        TickerGroup group = GetTickerGroup(unit);
        for (int i = 0; i < group.Tickers.Length; ++i)
        {
            group.Tickers[i].gameObject.SetActive(false);
        }
    }
}
