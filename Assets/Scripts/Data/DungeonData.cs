using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "LastKingdom/Dungeon Data")]
public class DungeonData : ScriptableObject
{
    [Header("Info")]
    [SerializeField] private DungeonType m_DungeonType;
    [SerializeField] private Sprite m_Thumbnail;

    [Header("Encounters")]
    [SerializeField] private EncounterData[] m_Encounters;

    [Header("Background")]
    [SerializeField] private Sprite m_CombatBg;
    [SerializeField] private Sprite m_SettleBg;


    public DungeonType DungeonType => m_DungeonType;
    public Sprite Thumbnail => m_Thumbnail;
    public IReadOnlyList<EncounterData> Encounters => m_Encounters;
    public Sprite CombatBg => m_CombatBg;
    public Sprite SettleBg => m_SettleBg;
}
