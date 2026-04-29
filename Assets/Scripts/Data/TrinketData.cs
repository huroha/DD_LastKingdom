using UnityEngine;

[CreateAssetMenu(fileName = "New Trinket", menuName = "LastKingdom/Trinket Data")]
public class TrinketData : ScriptableObject
{
    [Header("Basic Info")]
    [SerializeField] private string m_ItemName;
    [SerializeField] private string m_Description;
    [SerializeField] private ItemRarity m_Rarity;
    [SerializeField] private Sprite m_Icon;

    [Header("Stat Effect")]
    [SerializeField] private StatBlock m_StatDelta;

    public string ItemName => m_ItemName;
    public string Description => m_Description;
    public ItemRarity Rarity => m_Rarity;
    public Sprite Icon => m_Icon;
    public StatBlock StatDelta => m_StatDelta;

}
