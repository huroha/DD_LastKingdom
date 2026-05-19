using UnityEngine;
using System.Collections.Generic;



[System.Serializable]
public struct SkillLevelData
{
    [Header("Damage")]
    public float damageMultiplier;      // 피해 배율 1.0 == 100%
    public int accuracyMod;             // 명중 보정
    public float critMod;               // 크확 보정

    [Header("Heal & Ebla")]
    public int minHeal;
    public int maxHeal;
    public int eblaDamage;
    public int eblaHealAmount;
    public int allyEblaAmount;

    [Header("Effects")]
    public StatusEffectData[] onHitEffects;
    public StatusEffectData[] onSelfEffects;
    public StatusEffectData[] onAllyEffects;
}

[CreateAssetMenu(fileName = "New Skill", menuName = "LastKingdom/Skill Data")]
public class SkillData : BaseSkillData
{
    [Header("Basic Info")]
    [SerializeField] private string     m_Description;
    [SerializeField] private Sprite     m_SkillIcon;
    [SerializeField] private SkillRequiredState     m_RequiredState;


    [Header("Level Data")]
    [SerializeField] private SkillLevelData[] m_LevelData = new SkillLevelData[5];

    [Header("Special")]
    [SerializeField] private bool m_IsGuard;                    // 호위 스킬 여부
    [SerializeField] private bool m_IsForceGuard;               // 호위 강제 스킬
    [SerializeField] private int m_GuardDuration;
    [SerializeField] private bool m_MarkBonus;                  // 마크 추가 피해 여부
    [Range(0f, 2f)]
    [SerializeField] private float m_MarkDamageBonus;        // 마크 대상 추가피해 배율 
    [SerializeField] private bool m_ExcludeAllyEffect;



    public string Description => m_Description;
    public Sprite SkillIcon => m_SkillIcon;
    public SkillRequiredState RequiredState => m_RequiredState;


    public bool IsGuard => m_IsGuard;
    public bool IsForceGuard => m_IsForceGuard;
    public int GuardDuration => m_GuardDuration;
    public bool MarkBonus => m_MarkBonus;
    public float MarkDamageBonus => m_MarkDamageBonus;

    public bool ExcludeAllyEffect => m_ExcludeAllyEffect;



    public SkillLevelData GetLevelData(int level)
    {
        int idx = Mathf.Clamp(level - 1, 0, m_LevelData.Length - 1);
        return m_LevelData[idx];
    }
    private void OnValidate()
    {
        if (m_IsGuard && m_IsForceGuard)
            Debug.LogWarning($"{name}: IsGuard와 IsForceGuard는 동시에 설정 불가");
    }

}
