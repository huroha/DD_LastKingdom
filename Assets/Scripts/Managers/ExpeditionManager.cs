using UnityEngine;
using System.Collections.Generic;

public class ExpeditionManager : Singleton<ExpeditionManager>
{
    private readonly List<NikkeInstance> m_Party = new List<NikkeInstance>();
    private EncounterData m_Encounter;
    private bool m_IsActive;
    private ExpeditionInventory m_Inventory;




    public ExpeditionInventory Inventory => m_Inventory;
    public IReadOnlyList<NikkeInstance> Party => m_Party;
    public EncounterData Encounter => m_Encounter;
    public bool IsActive => m_IsActive;

    public void BeginExpedition(List<NikkeInstance> party, EncounterData encounter)
    {
        if (party == null || encounter == null) return;
        m_Party.Clear();
        m_Party.AddRange(party);
        m_Encounter = encounter;
        m_IsActive = true;

        m_Inventory = new ExpeditionInventory(DataManager.Instance.InventoryConfig);
    }
    public void EndExpedition()
    {
        Settle();
        m_Party.Clear();
        m_Encounter = null;
        m_IsActive=false;
    }

    public void Settle()
    {
        if (m_Inventory == null) return;
        for (int i=0; i< m_Inventory.Slots.Length; ++i)
        {
            LootItem slot = m_Inventory.Slots[i];
            if (slot.Quantity == 0) continue;
            switch(slot.Type)
            {
                case LootType.Credit:   ResourceManager.Instance.AddCredit(slot.Quantity); break;
                case LootType.BattleData:   ResourceManager.Instance.AddBattleData(slot.Quantity); break;
                case LootType.Core:   ResourceManager.Instance.AddCore(slot.Quantity); break;
                case LootType.Gems:   ResourceManager.Instance.AddGems(slot.Quantity); break;
                case LootType.Relics:   ResourceManager.Instance.AddRelic(slot.Relic, slot.Quantity); break;
            }
        }
        m_Inventory.Clear();
    }
}
