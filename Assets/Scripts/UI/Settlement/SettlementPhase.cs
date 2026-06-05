using UnityEngine;
using System;

public abstract class SettlementPhase : MonoBehaviour
{
    [Header("적용 Outcome")]
    [SerializeField] private bool m_OnClear = true;
    [SerializeField] private bool m_OnRetreat;
    [SerializeField] private bool m_OnWipe;

    public bool AppliesTo(ExpeditionOutcome outcome)
    {
        switch (outcome)
        {
            case ExpeditionOutcome.Cleared: return m_OnClear;
            case ExpeditionOutcome.Retreated: return m_OnRetreat;
            case ExpeditionOutcome.Wiped: return m_OnWipe;
            default: return false;
        }
    }
    public abstract void Begin(Action onComplete);
}
