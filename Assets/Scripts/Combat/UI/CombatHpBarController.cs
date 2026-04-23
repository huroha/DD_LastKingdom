using UnityEngine;
using System.Collections.Generic;

public class CombatHpBarController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CombatStatusIconController m_StatusIconController;

    [Header("HP Bars")]
    [SerializeField] private HpBarAnimator[] m_NikkeHpBars;       // 4
    [SerializeField] private HpBarAnimator[] m_EnemyHpBars;       // 4
    [SerializeField] private HpBarAnimator[] m_LargeEnemyHpBars;  // 3

    public int NikkeCount => m_NikkeHpBars.Length;
    public int EnemyCount => m_EnemyHpBars.Length;
    public int LargeEnemyCount => m_LargeEnemyHpBars.Length;

    private HashSet<CombatUnit> m_HiddenBars;

    private void Awake()
    {
        m_HiddenBars = new HashSet<CombatUnit>();
    }

    public void ClearHidden()
    {
        if (m_HiddenBars == null)
            m_HiddenBars = new HashSet<CombatUnit>();
        else
            m_HiddenBars.Clear();
    }

    public bool IsHidden(CombatUnit unit) => m_HiddenBars.Contains(unit);
    public void RemoveHidden(CombatUnit unit) => m_HiddenBars.Remove(unit);

    public HpBarAnimator GetAnimator(CombatUnit unit)
    {
        if (unit.UnitType == CombatUnitType.Nikke)
            return m_NikkeHpBars[unit.SlotIndex];
        return unit.SlotSize == 2
            ? m_LargeEnemyHpBars[unit.SlotIndex]
            : m_EnemyHpBars[unit.SlotIndex];
    }

    public HpBarAnimator GetNikkeBar(int slotIndex) => m_NikkeHpBars[slotIndex];
    public HpBarAnimator GetEnemyBar(int slotIndex) => m_EnemyHpBars[slotIndex];
    public HpBarAnimator GetLargeEnemyBar(int slotIndex) => m_LargeEnemyHpBars[slotIndex];

    public void HideAllBars()
    {
        for (int i = 0; i < m_NikkeHpBars.Length; ++i)
            m_NikkeHpBars[i].gameObject.SetActive(false);
        for (int i = 0; i < m_EnemyHpBars.Length; ++i)
            m_EnemyHpBars[i].gameObject.SetActive(false);
        for (int i = 0; i < m_LargeEnemyHpBars.Length; ++i)
            m_LargeEnemyHpBars[i].gameObject.SetActive(false);
    }

    public void InitBar(CombatUnit unit)
    {
        HpBarAnimator bar = GetAnimator(unit);
        if (unit.UnitType == CombatUnitType.Nikke)
            bar.InitHp(unit.CurrentHp, unit.MaxHp);
        else if (unit.State == UnitState.Corpse)
            bar.InitHp(unit.CurrentHp, Mathf.Max(unit.EnemyData.CorpseHp, 1));
        else
            bar.InitHp(unit.CurrentHp, unit.MaxHp);
    }

    public void RefreshBar(CombatUnit unit)
    {
        HpBarAnimator bar = GetAnimator(unit);
        if (unit.UnitType == CombatUnitType.Nikke)
            bar.SetHp(unit.CurrentHp, unit.MaxHp);
        else if (unit.State == UnitState.Corpse)
            bar.InitHp(unit.CurrentHp, Mathf.Max(unit.EnemyData.CorpseHp, 1));
        else
            bar.SetHp(unit.CurrentHp, unit.MaxHp);
    }

    public void Hide(CombatUnit unit)
    {
        m_HiddenBars.Add(unit);
        HpBarAnimator bar = GetAnimator(unit);
        if (bar != null)
            bar.gameObject.SetActive(false);
        m_StatusIconController.Clear(unit);
    }

    public void ShowForCorpse(CombatUnit unit)
    {
        m_HiddenBars.Remove(unit);
        HpBarAnimator bar = GetAnimator(unit);
        if (bar == null) return;
        bar.gameObject.SetActive(true);
        RefreshBar(unit);
        m_StatusIconController.Refresh(unit);
    }

    public void PrepareGhost(CombatUnit unit, int previousHp)
    {
        if (unit.State == UnitState.Dead) return;
        HpBarAnimator bar = GetAnimator(unit);
        if (bar == null) return;
        int maxHp = unit.State == UnitState.Corpse ? Mathf.Max(unit.EnemyData.CorpseHp, 1) : unit.MaxHp;
        bar.PrepareGhost(previousHp, unit.CurrentHp, maxHp);
    }

    public void StartGhostDrain(CombatUnit unit)
    {
        if (m_HiddenBars.Contains(unit)) return;
        HpBarAnimator bar = GetAnimator(unit);
        if (bar == null) return;
        if (!bar.gameObject.activeSelf)
            bar.gameObject.SetActive(true);
        bar.StartGhostDrain();
    }
}
