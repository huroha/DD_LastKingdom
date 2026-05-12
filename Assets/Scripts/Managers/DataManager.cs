using UnityEngine;
using System.Collections.Generic;
public class DataManager : Singleton<DataManager>
{
    [SerializeField] private NikkeData[] m_NikkeDatas;
    [SerializeField] private TrinketData[] m_TrinketDatas;
    [SerializeField] private QuirkData[] m_QuirkDatas;
    [SerializeField] private DiseaseData[] m_DiseaseDatas;

    private Dictionary<string, NikkeData> m_NikkeMap;
    private Dictionary<string, TrinketData> m_TrinketMap;
    private Dictionary<string, QuirkData> m_QuirkMap;
    private Dictionary<string , DiseaseData> m_DiseaseMap;

    protected override void Awake()
    {
        base.Awake();
        m_NikkeMap = new Dictionary<string, NikkeData>();
        m_TrinketMap = new Dictionary<string, TrinketData>();
        m_QuirkMap = new Dictionary<string, QuirkData>();
        m_DiseaseMap = new Dictionary<string, DiseaseData>();

        for (int i = 0; i < m_NikkeDatas.Length; ++i)
            if (m_NikkeDatas[i] != null)  m_NikkeMap[m_NikkeDatas[i].Id] = m_NikkeDatas[i];
        for (int i = 0; i < m_TrinketDatas.Length; ++i)
            if (m_TrinketDatas[i] != null) m_TrinketMap[m_TrinketDatas[i].Id] = m_TrinketDatas[i];
        for (int i = 0; i < m_QuirkDatas.Length; ++i)
            if (m_QuirkDatas[i] != null) m_QuirkMap[m_QuirkDatas[i].Id] = m_QuirkDatas[i];
        for (int i = 0; i < m_DiseaseDatas.Length; ++i)
            if (m_DiseaseDatas[i] != null) m_DiseaseMap[m_DiseaseDatas[i].Id] = m_DiseaseDatas[i];
    }
    public NikkeData GetNikkeData(string id)
    {
        if (m_NikkeMap.TryGetValue(id, out NikkeData data)) return data;
        Debug.LogWarning("[DataManager] NikkeData not found : " + id);
        return null;
    }

    public TrinketData GetTrinketData(string id)
    {
        if (m_TrinketMap.TryGetValue(id, out TrinketData data)) return data;
        Debug.LogWarning("[DataManager] TrinketData not found : " + id);
        return null;
    }

    public QuirkData GetQuirkData(string id)
    {
        if (m_QuirkMap.TryGetValue(id, out QuirkData data)) return data;
        Debug.LogWarning("[DataManager] QuirkData not found : " + id);
        return null;
    }

    public DiseaseData GetDiseaseData(string id)
    {
        if (m_DiseaseMap.TryGetValue(id, out DiseaseData data)) return data;
        Debug.LogWarning("[DataManager] DiseaseData not found : " + id);
        return null;
    }
}
