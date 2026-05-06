using UnityEngine;
using System.Collections.Generic;

public class RosterManager : Singleton<RosterManager>
{
    [SerializeField] private NikkeData[] m_InitialRoster;
    private List<NikkeInstance> m_Roster;

    public IReadOnlyList<NikkeInstance> Roster => m_Roster;
    protected override void Awake()
    {
        base.Awake();

        m_Roster = new List<NikkeInstance>();
        for (int i=0; i< m_InitialRoster.Length; ++i)
        {
            if (m_InitialRoster[i] == null) continue;
            m_Roster.Add(new NikkeInstance(m_InitialRoster[i]));
        }
    }
}
