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

public enum EffectMovement
{
    Static,
    Projectile,
}
public enum TargetMoveMode
{
    Fixed,      // 기존 고정이동
    RandomSlot, // 단일 대상 랜덤
    Reshuffle   // 전체 랜덤
}
public abstract class BaseSkillData : ScriptableObject
{
    [Header("Basic Info")]
    [SerializeField] private string m_SkillName;
    [SerializeField] private SkillType m_SkillType;

    [Header("Position")]
    [SerializeField] private bool[] m_UsablePositions = new bool[4];     //  사용자 위치 이동 (양수 = 후방, 음수 = 전방)
    [SerializeField] private bool[] m_TargetPositions = new bool[4];     // 대상 강제 이동
    [SerializeField] private TargetType m_TargetType;

    [Header("Move")]
    [SerializeField] private int m_MoveUserAmount;
    [SerializeField] private int m_MoveTargetAmount;
    [SerializeField] private TargetMoveMode m_TargetMoveMode;

    [Header("Special")]
    [SerializeField] private bool m_BypassGuard;

    [Header("Combat Effects")]
    [SerializeField] private Sprite m_AttackSprite;
    [SerializeField] private CombatEffectData m_AttackEffect;
    [SerializeField] private EffectMovement m_AttackMovement;
    [SerializeField] private float m_ProjectileSpeed = 20f;
    [SerializeField] private CombatEffectData m_HitEffect;

    public bool IsEnemyTargeting => m_TargetType == TargetType.EnemySingle
                              || m_TargetType == TargetType.EnemyMulti
                              || m_TargetType == TargetType.EnemyAll;
    public bool IsAllyTargeting => m_TargetType == TargetType.AllySingle
                                  || m_TargetType == TargetType.AllyMulti
                                  || m_TargetType == TargetType.AllyAll
                                  || m_TargetType == TargetType.Self;
    public string SkillName => m_SkillName;
    public IReadOnlyList<bool> UsablePositions => m_UsablePositions;
    public IReadOnlyList<bool> TargetPositions => m_TargetPositions;
    public TargetType TargetType => m_TargetType;
    public int MoveUserAmount => m_MoveUserAmount;
    public int MoveTargetAmount => m_MoveTargetAmount;
    public TargetMoveMode TargetMoveMode => m_TargetMoveMode;
    public bool BypassGuard => m_BypassGuard;
    public CombatEffectData AttackEffect => m_AttackEffect;
    public EffectMovement AttackMovement => m_AttackMovement;
    public float ProjectileSpeed => m_ProjectileSpeed;
    public CombatEffectData HitEffect => m_HitEffect;
    public Sprite AttackSprite => m_AttackSprite;
    public SkillType SkillType => m_SkillType;
}
