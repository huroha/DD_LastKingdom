using UnityEngine;

public enum LootType
{
    Credit,
    BattleData,
    Core,
    Gems,
    Trinket,
    SupplyItem,
    Relics,
}
public struct LootItem
{
    public LootType Type;
    public ScriptableObject Data;
    public int Quantity;
    public RelicType Relic;
}

public enum RelicType
{
    CommandInsignia,  // 지휘관 휘장 - 문장 대응
    UsbData,      // USB data - 흉상 대응
    CentralOrder, // 방주 명령서 - 증서 대응
    Handwriting,         // 수기 - 초상화 대응
}

[System.Serializable]
public struct RelicDrop
{
    public RelicType Type;
    public int Min;
    public int Max;
}
public class CombatResult
{
    public int TotalCredit;
    public int TotalBattleData;
    public int TotalCore;
    public int TotalGems;
    public int[] RelicAmounts;

    public LootItem[] Items;

    public CombatResult(int credit, int battledata, int core, int gems, int[] relicAmounts, LootItem[] items)
    {
        TotalCredit = credit;
        TotalBattleData = battledata;
        TotalCore = core;
        TotalGems = gems;
        RelicAmounts = relicAmounts;
        Items = items;
    }
}
