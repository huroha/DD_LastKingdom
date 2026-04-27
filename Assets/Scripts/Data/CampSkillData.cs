using UnityEngine;
using System.Collections.Generic;

public enum CampTargetType {  Self, AllySingle, AllyAll }
[CreateAssetMenu(fileName ="New CampSkill", menuName = "LastKingdom/Camp Skill Data")]
public class CampSkillData : ScriptableObject
{
    [Header("Basic Info")]
    [SerializeField] private string m_SkillName;
    [SerializeField] private string m_Description;
    [SerializeField] private Sprite m_Icon;
    [SerializeField] private CampTargetType m_TargetType;
    [SerializeField] private int m_ActionCost = 1;

    [Header("Effects")]
    [SerializeField] private int m_HpHeal;
    [SerializeField] private int m_EblaReduction;
    [SerializeField] private StatusEffectData[] m_BuffEffects;

    public string SkillName => m_SkillName;
    public string Description => m_Description;
    public Sprite Icon => m_Icon;
    public CampTargetType TargetType => m_TargetType;
    public int ActionCost => m_ActionCost;
    public int HpHeal => m_HpHeal;
    public int EblaReduction => m_EblaReduction;
    public IReadOnlyList<StatusEffectData> BuffEffects => m_BuffEffects;
    
}
