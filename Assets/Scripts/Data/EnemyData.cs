using UnityEngine;
using System.Collections.Generic;

public enum EnemyType
{
    Normal,
    Elite,
    Boss
}

[System.Serializable]
public struct DropTable
{
    [Header("Gold")]
    public int minGold;
    public int maxGold;

    [Header("Gems")]
    public int minGems;
    public int maxGems;

    // 추후 추가 예정
    // public TrinketDrop[]     trinkets;
    // public SupplyItemDrop[]  supplyItems;
    // public MaterialDrop[]    buildingMaterials;
}

[CreateAssetMenu(fileName = "New Enemy", menuName = "LastKingdom/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("Basic Info")]
    [SerializeField] private string m_EnemyName;
    [SerializeField] private EnemyType m_EnemyType;
    [SerializeField] private ElementType m_Element;

    [Header("Stats")]
    [SerializeField] private StatBlock m_BaseStats;

    [Header("Skills")]
    [SerializeField] private SkillData[] m_Skills;

    [Header("Drop")]
    [SerializeField] private DropTable m_DropTable;

    [Header("Corpse")]
    [SerializeField] private int m_CorpseHp;
    
    [Header("Visuals")]
    [SerializeField] private Sprite m_Sprite;

    public string EnemyName => m_EnemyName;
    public EnemyType EnemyType => m_EnemyType;
    public ElementType Element => m_Element;
    public StatBlock BaseStats => m_BaseStats;
    public IReadOnlyList<SkillData> Skills => m_Skills;
    public DropTable DropTable => m_DropTable;
    public int CorpseHp => m_CorpseHp;
    public Sprite Sprite => m_Sprite;
}