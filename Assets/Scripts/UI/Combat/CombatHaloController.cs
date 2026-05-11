using UnityEngine;

public class CombatHaloController : MonoBehaviour
{
    [Header("Halo")]
    [SerializeField] private UnitHaloDisplay[] m_NikkeHalos;
    [SerializeField] private EnemyHaloDisplay[] m_EnemyHalos;
    [SerializeField] private EnemyHaloDisplay[] m_LargeEnemyHalos;

    public void ShowStunHalo(CombatUnit unit)
    {
        GetStunHaloDisplay(unit)?.ShowStunHalo();
    }
    public void HideStunHalo(CombatUnit unit)
    {
        GetStunHaloDisplay(unit)?.HideStunHalo();
    }
    public void PopupDeathsDoorHalo(CombatUnit unit)
    {
        GetHaloDisplay(unit)?.PopupDeathsDoor();
    }
    public void PopupEblaHalo(CombatUnit unit, int delta)
    {
        if (delta > 0)
            GetHaloDisplay(unit)?.PopupEblaUp(delta);
        else if (delta < 0)
            GetHaloDisplay(unit)?.PopupEblaDown(delta);
    }
    private UnitHaloDisplay GetHaloDisplay(CombatUnit unit)
    {
        if (unit.UnitType != CombatUnitType.Nikke)
            return null;
        return m_NikkeHalos[unit.SlotIndex];
    }
    private IStunHaloDisplay GetStunHaloDisplay(CombatUnit unit)
    {
        if (unit.UnitType == CombatUnitType.Nikke)
            return m_NikkeHalos[unit.SlotIndex];
        return unit.SlotSize == 2
            ? m_LargeEnemyHalos[unit.SlotIndex]
            : m_EnemyHalos[unit.SlotIndex];
    }
}
