using UnityEngine;

public class CombatStatusIconController : MonoBehaviour
{
    [Header("Status Effect Icons")]
    [SerializeField] private StatusEffectIconDisplay[] m_NikkeIcons;
    [SerializeField] private StatusEffectIconDisplay[] m_EnemyIcons;
    [SerializeField] private StatusEffectIconDisplay[] m_LargeEnemyIcons;

    public void SetupTooltips(CombatTooltip tooltip)
    {
        for (int i = 0; i < m_NikkeIcons.Length; ++i)
            m_NikkeIcons[i].SetTooltip(tooltip);
        for (int i = 0; i < m_EnemyIcons.Length; ++i)
            m_EnemyIcons[i].SetTooltip(tooltip);
        for (int i = 0; i < m_LargeEnemyIcons.Length; ++i)
            m_LargeEnemyIcons[i].SetTooltip(tooltip);
    }

    public void Refresh(CombatUnit unit)
    {
        GetDisplay(unit).Refresh(unit.ActiveEffects,unit);
    }
    public void Clear(CombatUnit unit)
    {
        GetDisplay(unit).Clear();
    }
    public void ClearNikke(int slotIndex)
    {
        m_NikkeIcons[slotIndex].Clear();
    }
    public void ClearEnemy(int slotIndex)
    {
        m_EnemyIcons[slotIndex].Clear();
    }
    public void ClearLargeEnemy(int slotIndex)
    {
        m_LargeEnemyIcons[slotIndex].Clear();
    }
    private StatusEffectIconDisplay GetDisplay(CombatUnit unit)
    {
        if (unit.UnitType == CombatUnitType.Nikke)
            return m_NikkeIcons[unit.SlotIndex];
        if (unit.SlotSize == 2)
            return m_LargeEnemyIcons[unit.SlotIndex];
        return m_EnemyIcons[unit.SlotIndex];
    }
}
