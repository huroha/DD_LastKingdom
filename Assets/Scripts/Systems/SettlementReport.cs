using System.Collections.Generic;

public struct SettlementLine
{
    public LootType SourceType;
    public RelicType Relic;
    public int SourceQuantity;
    public int Credit;
}

public class SettlementReport
{
    public int Credit;
    public int BattleData;
    public int Core;
    public int Gems;
    public int ConvertedCredit;
    public List<int> CreditSlots;
    public List<SettlementLine> Lines;

    public int TotalCredit => Credit + ConvertedCredit;

    public SettlementReport()
    {
        Lines = new List<SettlementLine>();
        CreditSlots = new List<int>();
    }
}
