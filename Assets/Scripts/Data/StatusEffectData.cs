using UnityEngine;

public enum StatusEffectType
{
    Bleed,      // 출혈 - 턴 종료시 틱 피해
    Poison,     // 중독 - 턴 종료시 틱 피해
    Disease,    // 질병 - 질병마다 고유의 효과 존재(영구 디버프 패시브 느낌)
    Stun,       // 기절 - 행동 불가
    Buff,       // 버프 - 스탯 증가
    Debuff,      // 디버프 - 스탯 감소
    Guard,      // 지정 아군 대신 피격
    Mark        // 표식된 유닛 추가피해
}

[CreateAssetMenu(fileName = "New StatusEffect", menuName = "LastKingdom/Status Effect Data")]
public class StatusEffectData : ScriptableObject
{
    [Header("Basic Info")]
    [SerializeField] private string m_EffectName;
    [SerializeField] private Sprite m_Icon;
    [SerializeField] private StatusEffectType m_EffectType;

    [Header("Duration")]
    [SerializeField] private int m_Duration;    // 지속 턴 수

    [Header("DOT")]
    [SerializeField] private int m_TickDamage;      // 턴당 피해량 (출혈, 중독 사용)

    [Header("Stat Modifier")]
    [SerializeField] private StatBlock m_StatModifier;  // 스탯 보정값 (버프 디벞 사용)

    [Header("Stack")]
    [SerializeField] private bool m_IsStackable;
    [SerializeField] private int m_MaxStack = 1;    // 스택 가능일때만 유효


    public string EffectName => m_EffectName;
    public Sprite Icon => m_Icon;
    public StatusEffectType EffectType => m_EffectType;
    public int Duration => m_Duration;
    public int TickDamage => m_TickDamage;
    public StatBlock StatModifier => m_StatModifier;
    public bool IsStackable => m_IsStackable;
    public int MaxStack => m_MaxStack;

}
