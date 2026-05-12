using UnityEngine;

[CreateAssetMenu(fileName = "New Disease", menuName = "LastKingdom/Disease Data")]
public class DiseaseData : ScriptableObject
{
    [Header("Basic Info")]
    [SerializeField] private string m_Id;
    [SerializeField] private string m_DiseaseName;
    [SerializeField] private string m_Description;

    [Header("Stat Effect")]
    [SerializeField] private StatBlock m_StatDelta;

    public string Id => m_Id;
    public string DiseaseName => m_DiseaseName;
    public string Description => m_Description;
    public StatBlock StatDelta => m_StatDelta;

}
