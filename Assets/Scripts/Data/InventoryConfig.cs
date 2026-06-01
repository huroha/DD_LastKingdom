using UnityEngine;

[CreateAssetMenu(fileName = "InventoryConfig", menuName = "LastKingdom/Inventory Config")]
public class InventoryConfig : ScriptableObject
{
    //Capacity
    [Header("Capacity")]
    [SerializeField] private int m_SlotCount = 16;

    [Header("Stack Caps")]
    [SerializeField] private int m_CreditCap;
    [SerializeField] private int m_BattleDataCap;
    [SerializeField] private int m_CoreCap;
    [SerializeField] private int m_GemsCap;
    [SerializeField] private int m_RelicCap;

    [Header("Icons")]
    [SerializeField] private Sprite m_CreditIcon;
    [SerializeField] private Sprite m_BattleDataIcon;
    [SerializeField] private Sprite m_CoreIcon;
    [SerializeField] private Sprite m_GemsIcon;
    [SerializeField] private Sprite[] m_RelicIcons;  // index = (int)RelicType

    public int SlotCount => m_SlotCount;

    public int StackCap(LootType type)
    {
        switch(type)
        {
            case LootType.Credit:
                return m_CreditCap;
            case LootType.BattleData:
                return m_BattleDataCap;
            case LootType.Core:
                return m_CoreCap;
            case LootType.Gems:
                return m_GemsCap;
            case LootType.Relics:
                return m_RelicCap;
            default:
                return 1;
        }
    }
    public Sprite Icon(LootType type, RelicType relic = default)
    {
        switch(type)
        {
            case LootType.Credit:
                return m_CreditIcon;
            case LootType.BattleData:
                return m_BattleDataIcon;
            case LootType.Core:
                return m_CoreIcon;
            case LootType.Gems:
                return m_GemsIcon;

            case LootType.Relics:
                if (m_RelicIcons == null || (int)relic >= m_RelicIcons.Length) return null;
                return m_RelicIcons[(int)relic];
            default:
                return null;
        }
    }
}
