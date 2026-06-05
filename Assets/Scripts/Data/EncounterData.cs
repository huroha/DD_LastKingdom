using UnityEngine;
using System.Collections.Generic;

public enum DifficultyLevel { Apprentice, Veteran, Champion }
public enum QuestLength { Short, Medium, Long }
public enum QuestType { Patrol, Clear, Collect, Purify, Boss }
public enum QuestGoalType { BattlesWon }
public enum DungeonType { Ruins, Forest, Cove, Weald }

[CreateAssetMenu(menuName = "LastKingdom/Encounter Data")]
public class EncounterData : ScriptableObject 
{
    [Header("Info")]
    [SerializeField] private string m_EncounterName;
    [SerializeField] private string m_Description;

    [Header("Quest Info")]
    [SerializeField] private DifficultyLevel m_Difficulty;
    [SerializeField] private QuestLength m_Length;
    [SerializeField] private QuestType m_QuestType;
    [SerializeField] private int m_RewardGold;

    [Header("Quest Goal")]
    [SerializeField] private QuestGoalType m_GoalType;
    [SerializeField] private int m_GoalCount = 1;

    // 추후 Relic 보상과 Trinket 보상도 추가
    [Header("Card Icons")]
    [SerializeField] private Sprite m_BgIconSprite;
    [SerializeField] private Sprite m_IconSprite;

    [Header("Enemies")]
    [SerializeField] private EnemyData[] m_Enemies;



    public string EncounterName => m_EncounterName;
    public string Description => m_Description;
    public IReadOnlyList<EnemyData> Enemies => m_Enemies;
    public DifficultyLevel Difficulty => m_Difficulty;
    public QuestLength Length => m_Length;
    public QuestType QuestType => m_QuestType;
    public QuestGoalType GoalType => m_GoalType;
    public int GoalCount => m_GoalCount;
    public int RewardGold => m_RewardGold;
    public Sprite BgIconSprite => m_BgIconSprite;
    public Sprite IconSprite => m_IconSprite;
}
