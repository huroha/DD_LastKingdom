using UnityEngine;
using System.Collections.Generic;
public class ResourceManager : Singleton<ResourceManager>
{
    private int m_Credit;
    private int m_BattleData;
    private int m_Core;
    private int m_Gems;
    private int[] m_Relics = new int[4];

    public int Credit => m_Credit;
    public int BattleData => m_BattleData;
    public int Core => m_Core;
    public int Gems => m_Gems;
    public IReadOnlyList<int> Relics => m_Relics;

    public void ApplyCombatResult(CombatResult result)
    {
        AddCredit(result.TotalCredit);
        AddBattleData(result.TotalBattleData);
        AddCore(result.TotalCore);
        AddGems(result.TotalGems);
        if (result.RelicAmounts == null) return;
        for (int i=0; i< result.RelicAmounts.Length; ++i)
            AddRelic((RelicType)i,result.RelicAmounts[i]);
    }
    public void AddCredit(int amount) { m_Credit += amount; }
    public void AddBattleData(int amount) { m_BattleData  += amount; }
    public void AddCore(int amount) { m_Core += amount; }
    public void AddGems(int amount) { m_Gems += amount; }
    public void AddRelic(RelicType type, int amount) { m_Relics[(int)type] += amount; }

    public bool SpendCredit(int amount)
    {
        if (m_Credit < amount) return false;
        m_Credit -= amount;
        return true;
    }
    public bool SpendBattleData(int amount)
    {
        if (m_BattleData < amount) return false;
        m_BattleData -= amount;
        return true;
    }
    public bool SpendCore(int amount)
    {
        if (m_Core < amount) return false;
        m_Core -= amount;
        return true;
    }
    public bool SpendGems(int amount)
    {
        if (m_Gems < amount) return false;
        m_Gems -= amount;
        return true;
    }
    public void LoadFromSave(GameSaveData save)
    {
        m_Credit = save.credit;
        m_BattleData = save.battleData;
        m_Core = save.core;
        m_Gems = save.gems;
        if (save.relics != null)
            for (int i = 0; i < m_Relics.Length && i < save.relics.Length; ++i)
                m_Relics[i] = save.relics[i];
    }
    public void FillSaveData(GameSaveData save)
    {
        save.credit = m_Credit;
        save.battleData = m_BattleData;
        save.core = m_Core;
        save.gems = m_Gems;
        save.relics = new int[m_Relics.Length];
        for (int i=0; i<m_Relics.Length; ++i)
            save.relics[i] = m_Relics[i];
    }

}
