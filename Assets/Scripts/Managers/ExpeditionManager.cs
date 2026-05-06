using UnityEngine;
using System.Collections.Generic;

public class ExpeditionManager : Singleton<ExpeditionManager>
{
    private List<NikkeInstance> m_Party;
    private EncounterData m_Encounter;
    private bool m_IsActive;

    public IReadOnlyList<NikkeInstance> Party => m_Party;
    public EncounterData Encounter => m_Encounter;
    public bool IsActive => m_IsActive;

    public void BeginExpedition(List<NikkeInstance> party, EncounterData encounter)
    {
        m_Party = party;
        m_Encounter = encounter;
        m_IsActive = true;
    }
    public void EndExpedition()
    {
        m_Party = null;
        m_Encounter = null;
        m_IsActive=false;
    }
}
