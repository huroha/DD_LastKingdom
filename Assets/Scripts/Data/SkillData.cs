using UnityEngine;
using System.Collections.Generic;


public enum SkillType
{
    Melee,
    Ranged
}

public enum SkillRequiredState
{
    None,       // 항상 사용가능
    Awakened   // 각성 상태에서만 가능
}

public enum TargetType
{
    EnemySingle,
    EnemyMulti,     // TargetPosition에 해당하는 적 전체
    EnemyAll,       // TargetPosition 무시 전체
    AllySingle,
    AllyMulti,
    AllyAll,
    Self
}

[CreateAssetMenu(fileName = "New Skill", menuName = "LastKingdom/Skill Data")]
public class SkillData : ScriptableObject
{
    [Header("Basic Info")]
    [SerializeField] private string     m_SkillName;
    [SerializeField] private string     m_Description;
    [SerializeField] private Sprite     m_SkillIcon;
    [SerializeField] private SkillType  m_SkillType;
    [SerializeField] private SkillRequiredState     m_RequiredState;

    [Header("Position")]
    [SerializeField] private bool[]     m_UsablePositions = new bool[4]; // 사용 가능한 포지션 [0] = 1번.
    [SerializeField] private bool[]     m_TargetPositions = new bool[4]; // 타겟 가능한 포지션
    [SerializeField] private TargetType m_TargetType;

    [Header("Damage")]
    [SerializeField] private float m_DamageMultiplier = 1.0f;        // 피해 배율 (1.0 == 100%)
    [SerializeField] private int m_AccuracyMod;                     // 명중 보정
    [Range(0f, 100f)]
    [SerializeField] private float m_CritMod;                       // 치명 보정

    [Header("Heal & Ebla")]
    [SerializeField] private int m_HealAmount;                  // hp 회복량
    [SerializeField] private int m_EblaDamage;                  // 에블라 피해량
    [SerializeField] private int m_EblaHealAmount;              // 에블라 감소량

    [Header("Move")]
    [SerializeField] private int m_MoveUserAmount;      //  사용자 위치 이동 (양수 = 후방, 음수 = 전방)
    [SerializeField] private int m_MoveTargetAmount;        // 대상 강제 이동

    [Header("Special")]
    [SerializeField] private bool m_IsGuard;                    // 호위 스킬 여부
    [SerializeField] private bool m_IsMark;                     // 마크 부여 여부
    [Range(0f, 2f)]
    [SerializeField] private float m_MarkDamageBonus;        // 마크 대상 추가피해 배율 


    [Header("Effects")]
    [SerializeField] private StatusEffectData[] m_OnHitEffects;     // 적중 시 타겟에 적용

    public string SkillName => m_SkillName;
    public string Description => m_Description;
    public Sprite SkillIcon => m_SkillIcon;
    public SkillType SkillType => m_SkillType;
    public SkillRequiredState RequiredState => m_RequiredState;
    public IReadOnlyList<bool> UsablePositions => m_UsablePositions;
    public IReadOnlyList<bool> TargetPositions => m_TargetPositions;
    public TargetType TargetType => m_TargetType;
    public float DamageMultiplier => m_DamageMultiplier;
    public int AccuracyMod => m_AccuracyMod;
    public float CritMod => m_CritMod;

    public int HealAmount => m_HealAmount;
    public int EblaDamage => m_EblaDamage;
    public int EblaHealAmount => m_EblaHealAmount;
    public int MoveUserAmount => m_MoveUserAmount;
    public int MoveTargetAmount => m_MoveTargetAmount;
    public bool IsGuard => m_IsGuard;
    public bool IsMark => m_IsMark;
    public float MarkDamageBonus => m_MarkDamageBonus;


    public IReadOnlyList<StatusEffectData> OnHitEffects => m_OnHitEffects;
}