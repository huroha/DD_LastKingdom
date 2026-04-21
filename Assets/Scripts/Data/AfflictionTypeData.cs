using UnityEngine;

[CreateAssetMenu(fileName = "New AfflictionType", menuName = "LastKingdom/Ebla/Affliction Type")]
public class AfflictionTypeData : ScriptableObject
{
    [Header("Display")]
    [SerializeField] private string m_DisplayName;

    [Header("Effect")]
    [SerializeField] private StatusEffectData m_Debuff;

    [Header("Random Pool")]
    [SerializeField] private int m_RandomWeight;        // 0 = 랜덤 풀 제외 (고정 전용)

    public string DisplayName => m_DisplayName;
    public StatusEffectData Debuff => m_Debuff;
    public int RandomWeight => m_RandomWeight;
}
