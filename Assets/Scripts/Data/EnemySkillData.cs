using UnityEngine;

[CreateAssetMenu(fileName = "New Enemy Skill", menuName = "LastKingdom/Enemy Skill Data")]
public class EnemySkillData : BaseSkillData
{
    [Header("Attack")]
    [SerializeField] private int m_MinDamage;
    [SerializeField] private int m_MaxDamage;
    [SerializeField] private int m_AccuracyMod; // 명중률
    [SerializeField] private float m_CritMod;
    [Header("Cooldown")]
    [SerializeField] private int m_Cooldown;    // 적 기준 N턴마다 1회 0 = 제한 없음

    [Header("Ebla")]
    [SerializeField] private int m_EblaDamage;  // 명중 시 대상 에블라 증가

    [Header("Effects")]
    [SerializeField] private StatusEffectData[] m_OnHitEffects;     // 명중 시 대상에게
    [SerializeField] private StatusEffectData[] m_OnSelfEffects;    // 자신
    [SerializeField] private StatusEffectData[] m_OnAllyEffects;    // 다른 아군

    public int MaxDamage => m_MaxDamage;
    public int MinDamage => m_MinDamage;
    public int AccuracyMod => m_AccuracyMod;
    public float CritMod => m_CritMod;
    public int EblaDamage => m_EblaDamage;

    public StatusEffectData[] OnHitEffects => m_OnHitEffects;
    public StatusEffectData[] OnSelfEffects => m_OnSelfEffects;
    public StatusEffectData[] OnAllyEffects => m_OnAllyEffects;
    public int Cooldown => m_Cooldown;
}
