using UnityEngine;
using System.Collections.Generic;

public class ExpeditionManager : Singleton<ExpeditionManager>
{
    private readonly List<NikkeInstance> m_Party = new List<NikkeInstance>();
    private EncounterData m_Encounter;
    private bool m_IsActive;

    public IReadOnlyList<NikkeInstance> Party => m_Party;
    public EncounterData Encounter => m_Encounter;
    public bool IsActive => m_IsActive;

    public void BeginExpedition(List<NikkeInstance> party, EncounterData encounter)
    {
        if (party == null || encounter == null) return;
        m_Party.Clear();
        m_Party.AddRange(party);
        m_Encounter = encounter;
        m_IsActive = true;
    }
    public void EndExpedition()
    {
        m_Party.Clear();
        m_Encounter = null;
        m_IsActive=false;
    }
}
