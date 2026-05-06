using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "LastKingdom/Encounter Data")]
public class EncounterData : ScriptableObject 
{
    [Header("Info")]
    [SerializeField] private string m_EncounterName;
    [SerializeField] private string m_Description;
    [SerializeField] private Sprite m_Icon;

    [Header("Enemies")]
    [SerializeField] private EnemyData[] m_Enemies;

    public string EncounterName => m_EncounterName;
    public string Description => m_Description;
    public Sprite Icon => m_Icon;
    public IReadOnlyList<EnemyData> Enemies => m_Enemies;
}
