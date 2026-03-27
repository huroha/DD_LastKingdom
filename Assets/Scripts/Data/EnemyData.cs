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
    public int MinGold;
    public int MaxGold;

    [Header("Gems")]
    public int MinGems;
    public int MaxGems;

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
    [SerializeField] private int m_SlotSize = 1;
    [SerializeField] private int m_ActionsPerRound = 1;

    [Header("Stats")]
    [SerializeField] private StatBlock m_BaseStats;

    [Header("Skills")]
    [SerializeField] private SkillData[] m_Skills = new SkillData[0];

    [Header("Drop")]
    [SerializeField] private DropTable m_DropTable;

    [Header("Corpse")]
    [SerializeField] private int m_CorpseHp;
    [SerializeField] private int m_CorpseDecayTurns = 2;
    
    [Header("Visuals")]
    [SerializeField] private Sprite m_Sprite;
    [SerializeField] private Sprite m_CorpseSprite;
    public string EnemyName => m_EnemyName;
    public EnemyType EnemyType => m_EnemyType;
    public ElementType Element => m_Element;
    public StatBlock BaseStats => m_BaseStats;
    public IReadOnlyList<SkillData> Skills => m_Skills;
    public DropTable DropTable => m_DropTable;
    public int CorpseHp => m_CorpseHp;
    public int CorpseDecayTurns => m_CorpseDecayTurns;
    public Sprite Sprite => m_Sprite;
    public Sprite CorpseSprite => m_CorpseSprite;
    public int SlotSize => m_SlotSize;
    public int ActionsPerRound => m_ActionsPerRound;
}