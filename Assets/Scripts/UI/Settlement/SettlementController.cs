using UnityEngine;

public class SettlementController : MonoBehaviour
{
    [SerializeField] private SettlementPhase[] m_Phases;

    private int m_Index;
    private void Start()
    {
        m_Index = 0;
        RunNext();
    }
    private void RunNext()
    {
        ExpeditionOutcome outcome = ExpeditionManager.Instance.Outcome;

        for (int i= m_Index; i < m_Phases.Length; ++i)
        {
            if (!m_Phases[i].AppliesTo(outcome)) continue;

            m_Index = i + 1;
            m_Phases[i].Begin(OnPhaseDone);
            return;
        }
        // 적용 phase가 없으 -> 마을로
        ExpeditionManager.Instance.ReturnToTown();
    }
    private void OnPhaseDone()
    {
        RunNext();
    }
}
