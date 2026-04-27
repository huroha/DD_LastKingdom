using UnityEngine;
using System.Collections.Generic;


public class NikkeInstance
{
    private NikkeData   m_Data;
    private string      m_NameOverride;
    private int[] m_ActiveSkillIndices;
    private int m_WeaponLevel;
    private int m_ArmorLevel;
    private TrinketData[] m_Trinkets;
    private List<QuirkData> m_Quirks;
    private List<DiseaseData> m_Diseases;

    public NikkeData Data => m_Data;
    public string DisplayName => m_NameOverride ?? m_Data.NikkeName;
    public string NameOverride { get => m_NameOverride; set => m_NameOverride = value; }
    public IReadOnlyList<int> ActiveSkillIndices => m_ActiveSkillIndices;
    public int WeaponLevel { get => m_WeaponLevel; set => m_WeaponLevel = Mathf.Clamp(value, 1, 5); }
    public int ArmorLevel { get => m_ArmorLevel; set => m_ArmorLevel = Mathf.Clamp(value, 1, 5); }
    public TrinketData[] Trinkets => m_Trinkets;
    public IReadOnlyList<QuirkData> Quirks => m_Quirks;
    public IReadOnlyList<DiseaseData> Diseases => m_Diseases;

    public NikkeInstance(NikkeData data)
    {
        m_Data = data;
        m_ActiveSkillIndices = new int[] { 0, 1, 2, 3 };
        m_WeaponLevel = 1;
        m_ArmorLevel = 1;
        m_Trinkets = new TrinketData[2];
        m_Quirks = new List<QuirkData>();
        m_Diseases = new List<DiseaseData>();
    }

    public IReadOnlyList<SkillData> GetActiveSkills()
    {
        SkillData[] result = new SkillData[4];
        for (int i=0; i<4; ++i)
        {
            int idx = m_ActiveSkillIndices[i];
            result[i] = (idx >= 0 && idx < m_Data.Skills.Count) ? m_Data.Skills[idx] : null;
        }
        return result;
    }
    public void SetSkillIndex(int slot, int skillIndex)
    {
        if (slot < 0 || slot > 3) return;
        m_ActiveSkillIndices[slot] = Mathf.Clamp(skillIndex, 0, m_Data.Skills.Count -1);
    }
    public StatBlock GetEffectiveBaseStats()
    {
        StatBlock result = m_Data.BaseStats;
        if (m_Data.Weapon != null) result = result.Apply(m_Data.Weapon.GetStats(m_WeaponLevel));
        if (m_Data.Armor != null) result = result.Apply(m_Data.Armor.GetStats(m_ArmorLevel));
        for (int i=0; i < m_Trinkets.Length; ++i)
        {
            if (m_Trinkets[i] != null) result = result.Apply(m_Trinkets[i].StatDelta);
        }
        for (int i = 0; i < m_Quirks.Count; ++i)
            result = result.Apply(m_Quirks[i].StatDelta);
        for (int i = 0; i < m_Diseases.Count; ++i)
            result = result.Apply(m_Diseases[i].StatDelta);
        return result;
    }

    public void AddQuirk(QuirkData quirk)
    {
        if (quirk == null || m_Quirks.Contains(quirk)) return;
        m_Quirks.Add(quirk);
    }
    public void RemoveQuirk(QuirkData quirk)
    {
        m_Quirks.Remove(quirk);
    }
    public void AddDisease(DiseaseData disease)
    {
        if (disease == null || m_Diseases.Contains(disease)) return;
        m_Diseases.Add(disease);
    }
    public void RemoveDisease(DiseaseData disease)
    {
        m_Diseases.Remove(disease);
    }
}
