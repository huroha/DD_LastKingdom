using UnityEngine;
using System.Collections.Generic;

public enum ExpeditionOutcome {  Cleared, Retreated, Wiped }
public class ExpeditionManager : Singleton<ExpeditionManager>
{
    private readonly List<NikkeInstance> m_Party = new List<NikkeInstance>();
    private EncounterData m_Encounter;
    private bool m_IsActive;
    private ExpeditionInventory m_Inventory;
    private int m_BattlesWon;
    private ExpeditionOutcome m_Outcome;
    private readonly List<NikkeInstance> m_DeadNikkes = new List<NikkeInstance>();
    private DungeonData m_Dungeon;



    public ExpeditionInventory Inventory => m_Inventory;
    public IReadOnlyList<NikkeInstance> Party => m_Party;
    public EncounterData Encounter => m_Encounter;
    public bool IsActive => m_IsActive;
    public ExpeditionOutcome Outcome => m_Outcome;
    public IReadOnlyList<NikkeInstance> DeadNikkes => m_DeadNikkes;
    public DungeonData Dungeon => m_Dungeon;
    public void SetOutcome(ExpeditionOutcome outcome)
    {
        m_Outcome = outcome;
    }
    public bool IsQuestComplete
    {
        get
        {
            if (m_Encounter == null) return false;
            switch(m_Encounter.GoalType)
            {
                case QuestGoalType.BattlesWon:
                    return m_BattlesWon >= m_Encounter.GoalCount;
                default: return false;
            }
        }
    }

    public void BeginExpedition(List<NikkeInstance> party, EncounterData encounter, DungeonData dungeon)
    {
        if (party == null || encounter == null) return;
        m_DeadNikkes.Clear();
        m_Party.Clear();
        m_Party.AddRange(party);
        m_Encounter = encounter;
        m_IsActive = true;
        m_BattlesWon = 0;
        m_Inventory = new ExpeditionInventory(DataManager.Instance.InventoryConfig);
        m_Dungeon = dungeon;
    }
    public void EndExpedition()
    {
        m_Party.Clear();
        m_Encounter = null;
        m_IsActive=false;
        m_Dungeon = null;
    }
    public void RecordBattleWon()
    {
        ++m_BattlesWon;
    }
    public void ReturnToTown()
    {
        EndExpedition();
        GameManager.Instance.ChangeState(GameState.Town);
    }
    public SettlementReport BuildReport()
    {
        if (m_Inventory == null) return new SettlementReport();
        SettlementConfig cfg = DataManager.Instance.SettlementConfig;
        SettlementReport report = new SettlementReport();

        for (int i=0; i< m_Inventory.Slots.Length; ++i)
        {
            LootItem slot = m_Inventory.Slots[i];
            if (slot.Quantity == 0) continue;

            switch (slot.Type)
            {
                case LootType.Credit:
                    report.Credit += slot.Quantity;
                    report.CreditSlots.Add(slot.Quantity);
                    break;
                case LootType.BattleData:
                    report.BattleData += slot.Quantity;
                    break;
                case LootType.Core:
                    report.Core += slot.Quantity;
                    break;
                case LootType.Gems:
                    report.Gems += slot.Quantity;
                    break;
                case LootType.Relics:
                    int credit = cfg.RelicCredit(slot.Relic) * slot.Quantity;
                    report.ConvertedCredit += credit;
                    report.Lines.Add(new SettlementLine
                    {
                        SourceType = LootType.Relics,
                        Relic = slot.Relic,
                        SourceQuantity = slot.Quantity,
                        Credit = credit
                    });
                    break;
                case LootType.SupplyItem:
                    //todo 구매가의 10분의 1
                    break;
            }
        }
        return report;
    }
    public void CommitLoot(SettlementReport report)
    {
        ResourceManager rm = ResourceManager.Instance;
        rm.AddCredit(report.Credit);
        rm.AddBattleData(report.BattleData);
        rm.AddCore(report.Core);
        rm.AddGems(report.Gems);
        rm.AddCredit(report.ConvertedCredit);
        m_Inventory.Clear();
    }
    public void AddDead(NikkeInstance nikke)
    {
        if (nikke == null) return;
        if (m_DeadNikkes.Contains(nikke)) return;
        m_DeadNikkes.Add(nikke);
    }
}
