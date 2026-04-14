using UnityEngine;
using UnityEngine.UI;

public class CombatTurnBarDisplay : MonoBehaviour
{
    [Header("Active turn Bar")]
    [SerializeField] private Image m_ActiveTurnBar;
    [SerializeField] private Image m_LargeActiveTurnBar;
    [SerializeField] private RectTransform[] m_NikkeBarAnchor;
    [SerializeField] private RectTransform[] m_EnemyBarAnchor;
    [SerializeField] private RectTransform[] m_LargeEnemyBarAnchors;
    private CombatUnit m_CurrentTurnUnit;

    public void Snap(CombatUnit unit)
    {
        m_CurrentTurnUnit = unit;
        if (unit == null)
            return;

        bool isLarge = unit.UnitType == CombatUnitType.Enemy && unit.SlotSize == 2;

        m_ActiveTurnBar.gameObject.SetActive(!isLarge);
        m_LargeActiveTurnBar.gameObject.SetActive(isLarge);

        Image bar = isLarge ? m_LargeActiveTurnBar : m_ActiveTurnBar;
        RectTransform anchor = GetAnchor(unit);
        if (anchor == null)
            return;
        bar.rectTransform.position = anchor.position;
    }
    public void Hide()
    {
        m_ActiveTurnBar.gameObject.SetActive(false);
        m_LargeActiveTurnBar.gameObject.SetActive(false);
    }

    public void Refresh()
    {
        if(m_CurrentTurnUnit != null)
            Snap(m_CurrentTurnUnit);
    }
    public RectTransform GetAnchor(CombatUnit unit)
    {
        bool isLarge = unit.UnitType == CombatUnitType.Enemy && unit.SlotSize == 2;
        if (unit.UnitType == CombatUnitType.Nikke)
            return m_NikkeBarAnchor[unit.SlotIndex];
        else if (isLarge)
            return m_LargeEnemyBarAnchors[unit.SlotIndex];
        else
            return m_EnemyBarAnchor[unit.SlotIndex];
    }
}
