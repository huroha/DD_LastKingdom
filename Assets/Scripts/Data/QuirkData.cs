using UnityEngine;

[CreateAssetMenu(fileName = "New Quirk", menuName = "LastKingdom/Quirk Data")]
public class QuirkData : ScriptableObject
{
    [Header("Basic Info")]
    [SerializeField] private string m_QuirkName;
    [SerializeField] private string m_Description;
    [SerializeField] private bool m_IsPositive;

    [Header("Stat Effect")]
    [SerializeField] private StatBlock m_StatDelta;

    public string QuirkName => m_QuirkName;
    public string Description => m_Description;
    public bool IsPositive => m_IsPositive;
    public StatBlock StatDelta => m_StatDelta;

}
