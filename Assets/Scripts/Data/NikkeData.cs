using UnityEngine;
using System.Collections.Generic;

public enum NikkeClass
{
    Attacker,
    Supporter,
    Defender,
}

public enum Manufacturer
{
    Pilgrim,
    Elysion,
    Missilis,
    Tetra,
    Abnormal
}

public enum ElementType
{
    Fire,
    Water,
    Wind,
    Iron,
    Electric
}

[CreateAssetMenu(fileName = "New Nikke", menuName = "LastKingdom/Nikke Data")]
public class NikkeData : ScriptableObject
{
    [Header("Basic Info")]
    [SerializeField] private string m_NikkeName;
    [SerializeField] private NikkeClass m_NikkeClass;
    [SerializeField] private bool m_CanVirtue;
    [SerializeField] private Manufacturer m_Manufacturer;
    [SerializeField] private ElementType m_Element;
    [SerializeField] private SquadData m_Squad;

    public const int MaxSkillCount = 7;

    [Header("Base Stats")]
    [SerializeField] private StatBlock m_BaseStats;

    [Header("Level")]
    [SerializeField] private int        m_MaxLevel = 6;
    [SerializeField] private int[]      m_ExpThresholds = new int[0];        // 인덱스 = 레밸, 값 = 해당 레벨 도달 누적 경험치
    [SerializeField] private StatBlock  m_StatGrowthPerLevel;   // 레벨업 시 가산되는 스탯

    [Header("Skills")]
    [SerializeField] private SkillData[] m_Skills = new SkillData[MaxSkillCount];

    [Header("Visuals")]
    [SerializeField] private Sprite m_PortraitSprite;           // 초상화
    [SerializeField] private Sprite m_CombatIdleSprite;           // Idle
    [SerializeField] private GameObject m_CombatPrefab;
    [SerializeField] private Sprite m_AttackSprite;
    [SerializeField] private Sprite m_HitSprite;

    [SerializeField] private float m_ScaleOffset;


    [SerializeField] private Sprite m_VirtuePortraitSprite;     // 각성 영웅만 사용

    [Header("Passive : Critical")]
    [SerializeField] private StatusEffectData[] m_OnCritSelfEffects;
    [SerializeField] private StatusEffectData[] m_OnReceiveCritSelfEffects;
 

    // Get Method
    public string NikkeName                 => m_NikkeName;
    public NikkeClass NikkeClass            => m_NikkeClass;
    public bool CanVirtue                   => m_CanVirtue;
    public Manufacturer Manufacturer        => m_Manufacturer;
    public SquadData Squad                  => m_Squad;
    public ElementType  Element             => m_Element;
    public StatBlock BaseStats              => m_BaseStats;
    public int MaxLevel                     => m_MaxLevel;
    public IReadOnlyList<int> ExpThresholds => m_ExpThresholds;
    public StatBlock StatGrowthPerLevel     => m_StatGrowthPerLevel;
    public IReadOnlyList<SkillData> Skills  => m_Skills;
    public Sprite PortraitSprite            => m_PortraitSprite;   
    public Sprite CombatIdleSprite            => m_CombatIdleSprite;   
    public GameObject CombatPrefab => m_CombatPrefab;
    public Sprite AttackSprite => m_AttackSprite;
    public Sprite HitSprite => m_HitSprite;
    public float ScaleOffset => m_ScaleOffset;
    public Sprite VirtuePortraitSprite      => m_VirtuePortraitSprite;

    public IReadOnlyList<StatusEffectData> OnCritSelfEffects => m_OnCritSelfEffects;
    public IReadOnlyList<StatusEffectData> OnReceiveCritSelfEffects => m_OnReceiveCritSelfEffects;
}