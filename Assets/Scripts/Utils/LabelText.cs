using UnityEngine;

public static class LabelText
{
    // NikkeDetailPanel 에서 이동
    public static string GetClassLabel(NikkeClass nikkeClass)
    {
        switch (nikkeClass)
        {
            case NikkeClass.Attacker: return "공격형";
            case NikkeClass.Supporter: return "지원형";
            case NikkeClass.Defender: return "방어형";
            default: return nikkeClass.ToString();
        }
    }
    public static string GetManufacturerLabel(Manufacturer manufacturer)
    {
        switch (manufacturer)
        {
            case Manufacturer.Pilgrim: return "필그림";
            case Manufacturer.Elysion: return "엘리시온";
            case Manufacturer.Missilis: return "미실리스";
            case Manufacturer.Tetra: return "테트라";
            case Manufacturer.Abnormal: return "어브노멀";
            default: return manufacturer.ToString();
        }
    }
    public static string GetRankLabel(int level)
    {
        switch (level)
        {
            case 0: return "풋내기";
            case 1: return "견습";
            case 2: return "모험가";
            case 3: return "베테랑";
            case 4: return "달인";
            case 5: return "영웅";
            case 6: return "전설";
            default: return level.ToString();
        }
    }
    public static string GetRarityLabel(ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.Common: return "일반";
            case ItemRarity.Uncommon: return "고급";
            case ItemRarity.Rare: return "희귀";
            case ItemRarity.Epic: return "영웅";
            case ItemRarity.Legendary: return "전설";
            default: return rarity.ToString();
        }
    }
    public static string GetDifficultyLabel(DifficultyLevel v)
    {
        switch (v)
        {
            case DifficultyLevel.Apprentice: return "견습";
            case DifficultyLevel.Veteran: return "베테랑";
            case DifficultyLevel.Champion: return "챔피언";
            default: return v.ToString();
        }
    }
    public static string GetLengthLabel(QuestLength v)
    {
        switch (v)
        {
            case QuestLength.Short: return "짧음";
            case QuestLength.Medium: return "중간";
            case QuestLength.Long: return "긺";
            default: return v.ToString();
        }
    }
    public static string GetQuestTypeLabel(QuestType v)
    {
        switch (v)
        {
            case QuestType.Patrol: return "정찰";
            case QuestType.Clear: return "전투";
            case QuestType.Collect: return "수집";
            case QuestType.Purify: return "정화";
            case QuestType.Boss: return "보스";
            default: return v.ToString();
        }
    }
    public static string GetDungeonTypeLabel(DungeonType v)
    {
        switch(v)
        {
            case DungeonType.Ruins: return "폐허";
            case DungeonType.Forest: return "산림지대";
            case DungeonType.Cove: return "해안 만";
            case DungeonType.Weald: return "황무지";
            default: return v.ToString();
        }
    }

}
