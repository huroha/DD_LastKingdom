using UnityEngine;
using System.Collections.Generic;
using System;

public class NikkeInstance
{
    private NikkeData m_Data;
    private string m_NameOverride;
    private int[] m_ActiveSkillIndices;
    private int[] m_ActiveCampSkillIndices;
    private int m_Level;
    private int m_WeaponLevel;
    private int m_ArmorLevel;
    private int[] m_SkillLevels;
    private TrinketData[] m_Trinkets;
    private List<QuirkData> m_PosQuirks;
    private List<QuirkData> m_NegQuirks;
    private List<DiseaseData> m_Diseases;
    private int m_Exp;

    private StatBlock m_CachedStats;
    private bool m_StatsDirty = true;
    private void InvalidateStats() => m_StatsDirty = true;
    public NikkeData Data => m_Data;
    public string DisplayName => m_NameOverride ?? m_Data.NikkeName;
    public string NameOverride { get => m_NameOverride; set => m_NameOverride = value; }
    public IReadOnlyList<int> ActiveSkillIndices => m_ActiveSkillIndices;
    public void SetSkillIndex(int slot, int skillIndex)
    {
        if (slot < 0 || slot >= m_ActiveSkillIndices.Length) return;
        if (skillIndex == -1) { m_ActiveSkillIndices[slot] = -1; return; }
        m_ActiveSkillIndices[slot] = Mathf.Clamp(skillIndex, 0, m_Data.Skills.Count - 1);
    }
    public IReadOnlyList<int> ActiveCampSkillIndices => m_ActiveCampSkillIndices;
    public void SetCampSkillIndex(int slot, int skillIndex)
    {
        if (slot < 0 || slot >= m_ActiveCampSkillIndices.Length) return;
        if (skillIndex == -1) { m_ActiveCampSkillIndices[slot] = -1; return; }
        m_ActiveCampSkillIndices[slot] = Mathf.Clamp(skillIndex, 0, m_Data.CampSkills.Count - 1);
    }
    public int Exp
    {
        get => m_Exp;
        set
        {
            IReadOnlyList<int> thresholds = m_Data.ExpThresholds;
            if (m_Level >= thresholds.Count) return;   // 최대 레벨이면 변경 안 함
            m_Exp = Mathf.Clamp(value, 0, thresholds[m_Level]);
        }
    }
    public int Level
    {
        get => m_Level;
        set { m_Level = Mathf.Clamp(value, 0, m_Data.MaxLevel); InvalidateStats(); }
    }
    public int WeaponLevel
    { get => m_WeaponLevel;
        set { m_WeaponLevel = Mathf.Clamp(value, 1, 5); InvalidateStats(); }
    }
    public int ArmorLevel
    {
        get => m_ArmorLevel;
        set { m_ArmorLevel = Mathf.Clamp(value, 1, 5); InvalidateStats(); }
    }
    public TrinketData[] Trinkets => m_Trinkets;
    public IReadOnlyList<QuirkData> PosQuirks => m_PosQuirks;
    public IReadOnlyList<QuirkData> NegQuirks => m_NegQuirks;
    public IReadOnlyList<DiseaseData> Diseases => m_Diseases;

    public NikkeInstance(NikkeData data)
    {
        m_Data = data;
        m_ActiveSkillIndices = new int[] { 0, 1, 2, 3 };
        m_ActiveCampSkillIndices = new int[] { 0, 1, 2 };
        m_Level = 0;
        m_SkillLevels = new int[m_Data.Skills.Count];
        for (int i = 0; i < m_SkillLevels.Length; ++i) m_SkillLevels[i] = 1;
        m_WeaponLevel = 1;
        m_ArmorLevel = 1;
        m_Trinkets = new TrinketData[2];
        m_PosQuirks = new List<QuirkData>();
        m_NegQuirks = new List<QuirkData>();
        m_Diseases = new List<DiseaseData>();
        m_Exp = 0;
    }

    public IReadOnlyList<SkillData> GetActiveSkills()
    {
        SkillData[] result = new SkillData[4];
        for (int i = 0; i < 4; ++i)
        {
            int idx = m_ActiveSkillIndices[i];
            result[i] = (idx >= 0 && idx < m_Data.Skills.Count) ? m_Data.Skills[idx] : null;
        }
        return result;
    }

    public IReadOnlyList<int> SkillLevels => m_SkillLevels;
    public void SetSkillLevel(int skillIdx, int level)
    {
        if (skillIdx < 0 || skillIdx >= m_SkillLevels.Length) return;
        m_SkillLevels[skillIdx] = Mathf.Max(1, level);
    }
    public int GetSkillLevel(SkillData skill)
    {
        IReadOnlyList<SkillData> skills = m_Data.Skills;
        for (int i=0; i< skills.Count; ++i)
        {
            if (skills[i] == skill) return m_SkillLevels[i];
        }
        return 1;
    }
    public StatBlock GetEffectiveBaseStats()
    {
        if (!m_StatsDirty) return m_CachedStats;
        StatBlock result = m_Data.BaseStats;
        result = result.Apply(m_Data.StatGrowthPerLevel.Scale(m_Level));
        if (m_Data.Weapon != null) result = result.Apply(m_Data.Weapon.GetStats(m_WeaponLevel));
        if (m_Data.Armor != null) result = result.Apply(m_Data.Armor.GetStats(m_ArmorLevel));
        for (int i=0; i < m_Trinkets.Length; ++i)
        {
            if (m_Trinkets[i] != null) result = result.Apply(m_Trinkets[i].StatDelta);
        }
        for (int i = 0; i < m_PosQuirks.Count; ++i)
            result = result.Apply(m_PosQuirks[i].StatDelta);
        for (int i = 0; i < m_NegQuirks.Count; ++i)
            result = result.Apply(m_NegQuirks[i].StatDelta);
        for (int i = 0; i < m_Diseases.Count; ++i)
            result = result.Apply(m_Diseases[i].StatDelta);

        m_CachedStats = result;
        m_StatsDirty = false;
        return m_CachedStats;
    }

    public void AddQuirk(QuirkData quirk)
    {
        if (quirk == null) return;
        List<QuirkData> target = quirk.IsPositive ? m_PosQuirks : m_NegQuirks;
        if (target.Contains(quirk)) return;
        target.Add(quirk);
        InvalidateStats();
    }
    public void RemoveQuirk(QuirkData quirk)
    {
        if (quirk == null) return;
        if (quirk.IsPositive)
        {
            m_PosQuirks.Remove(quirk);
            InvalidateStats();
        }
        else
        {
            m_NegQuirks.Remove(quirk);
            InvalidateStats();
        }
    }
    public void AddDisease(DiseaseData disease)
    {
        if (disease == null || m_Diseases.Contains(disease)) return;
        m_Diseases.Add(disease);
        InvalidateStats();
    }
    public void RemoveDisease(DiseaseData disease)
    {
        m_Diseases.Remove(disease);
        InvalidateStats();
    }
    public NikkeSaveData ToSaveData()
    {
        NikkeSaveData save = new NikkeSaveData();
        save.nikkeId = m_Data.Id;
        save.nameOverride = m_NameOverride;
        save.level = m_Level;
        save.exp = m_Exp;
        save.weaponLevel = m_WeaponLevel;
        save.armorLevel = m_ArmorLevel;

        save.skillLevels = new int[m_SkillLevels.Length];
        Array.Copy(m_SkillLevels, save.skillLevels, m_SkillLevels.Length);

        save.activeSkillIndices = new int[m_ActiveSkillIndices.Length];
        Array.Copy(m_ActiveSkillIndices, save.activeSkillIndices, m_ActiveSkillIndices.Length);

        save.activeCampSkillIndices = new int[m_ActiveCampSkillIndices.Length];
        Array.Copy(m_ActiveCampSkillIndices, save.activeCampSkillIndices, m_ActiveCampSkillIndices.Length);

        save.trinketNames = new string[m_Trinkets.Length];
        for (int i = 0; i < m_Trinkets.Length; ++i)
            save.trinketNames[i] = m_Trinkets[i] != null ? m_Trinkets[i].Id : "";

        save.posQuirkNames = ToIds(m_PosQuirks, q => q.Id);
        save.negQuirkNames = ToIds(m_NegQuirks, q => q.Id);
        save.diseaseNames = ToIds(m_Diseases, d => d.Id);

        return save;
    }
    private static string[] ToIds<T>(List<T> list, Func<T, string> getId)
    {
        string[] ids = new string[list.Count];
        for (int i = 0; i < list.Count; ++i)
            ids[i] = getId(list[i]);
        return ids;
    }
    private static void HydrateList<T>(string[] ids, Func<string, T> resolve, List<T> dst) where T : class
    {
        if (ids == null) return;
        for (int i = 0; i < ids.Length; ++i)
        {
            T item = resolve(ids[i]);
            if (item != null) dst.Add(item);
        }
    }
    public NikkeInstance(NikkeData data, NikkeSaveData save)
    {
        m_Data = data;
        m_NameOverride = save.nameOverride;
        m_Level = Mathf.Clamp(save.level, 0, data.MaxLevel);
        Exp = save.exp;   // setter로 threshold 검증
        m_WeaponLevel = Mathf.Clamp(save.weaponLevel, 1, 5);
        m_ArmorLevel = Mathf.Clamp(save.armorLevel, 1, 5);

        m_SkillLevels = new int[data.Skills.Count];
        for (int i = 0; i < m_SkillLevels.Length; ++i)
            m_SkillLevels[i] = (save.skillLevels != null && i < save.skillLevels.Length) ? save.skillLevels[i] : 1;

        m_ActiveSkillIndices = save.activeSkillIndices ?? new int[] { 0, 1, 2, 3 };
        m_ActiveCampSkillIndices = save.activeCampSkillIndices ?? new int[] { 0, 1, 2 };

        m_Trinkets = new TrinketData[2];
        if (save.trinketNames != null)
            for (int i = 0; i < m_Trinkets.Length && i < save.trinketNames.Length; ++i)
                if (!string.IsNullOrEmpty(save.trinketNames[i]))
                    m_Trinkets[i] = DataManager.Instance.GetTrinketData(save.trinketNames[i]);

        m_PosQuirks = new List<QuirkData>();
        m_NegQuirks = new List<QuirkData>();
        m_Diseases = new List<DiseaseData>();
        HydrateList(save.posQuirkNames, DataManager.Instance.GetQuirkData, m_PosQuirks);
        HydrateList(save.negQuirkNames, DataManager.Instance.GetQuirkData, m_NegQuirks);
        HydrateList(save.diseaseNames, DataManager.Instance.GetDiseaseData, m_Diseases);
    }
    public void SetTrinket(int slot, TrinketData data)
    {
        if (slot < 0 || slot >= m_Trinkets.Length) return;
        m_Trinkets[slot] = data;
        InvalidateStats();
    }
}
