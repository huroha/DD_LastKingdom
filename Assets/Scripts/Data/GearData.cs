using UnityEngine;
using System.Collections.Generic;

public enum GearType { Weapon, Armor }
[CreateAssetMenu(fileName = "New Gear", menuName = "LastKingdom/Gear Data")]
public class GearData : ScriptableObject
{
    [Header("Basic Info")]
    [SerializeField] private string m_GearName;
    [SerializeField] private string m_Description;
    [SerializeField] private GearType m_GearType;

    [Header("Levels (index 0 = lv 1 ~ index 4 = lv 5")]
    [SerializeField] private Sprite[] m_LevelSprites;
    [SerializeField] private StatBlock[] m_LevelStats;

    public string GearName => m_GearName;
    public string Description => m_Description;
    public GearType GearType => m_GearType;

    public IReadOnlyList<Sprite> LevelSprites => m_LevelSprites;
    public IReadOnlyList<StatBlock> LevelStats => m_LevelStats;

    public Sprite GetSprite(int level)
    {
        int idx = Mathf.Clamp(level - 1, 0, m_LevelSprites.Length - 1);
        return m_LevelSprites[idx];
    }
    public StatBlock GetStats(int level)
    {
        int idx = Mathf.Clamp(level - 1, 0, m_LevelStats.Length - 1);
        return m_LevelStats[idx];
    }
}
