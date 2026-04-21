using UnityEngine;

[CreateAssetMenu(fileName = "New VirtueType", menuName = "LastKingdom/Ebla/Virtue Type")]
public class VirtueTypeData : ScriptableObject
{
    [Header("Display")]
    [SerializeField] private string m_DisplayName;

    [Header("Effect")]
    [SerializeField] private StatusEffectData m_Buff;

    [Header("Random Pool")]
    [SerializeField] private int m_RandomWeight;

    public string DisplayName => m_DisplayName;
    public StatusEffectData Buff => m_Buff;
    public int RandomWeight => m_RandomWeight;

    
}
