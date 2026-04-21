using UnityEngine;
using System.Collections.Generic;

public struct PendingEblaResolution
{
    public CombatUnit   Unit;
    public EblaResolutionType ResolutionType;
    public AfflictionTypeData AfflictionType;
    public VirtueTypeData VirtueType;
}
public class EblaSystem
{
    private AfflictionTypeData[] m_AfflictionTypes;
    private VirtueTypeData[] m_VirtueTypes;
    private float m_VirtueChance;

    private List<PendingEblaResolution> m_PendingQueue;

    public int PendingCount => m_PendingQueue.Count;
    public EblaSystem(AfflictionTypeData[] afflictionTypes, VirtueTypeData[] virtueTypes, float virtueChance)
    {
        m_AfflictionTypes = afflictionTypes;
        m_VirtueTypes = virtueTypes;
        m_VirtueChance = virtueChance;

        m_PendingQueue = new List<PendingEblaResolution>();
    }

    // 반환값 : true = 200 도달로 사망
    public bool ModifyEbla(CombatUnit unit, int amount)
    {
        if (unit.UnitType != CombatUnitType.Nikke || !unit.IsAlive)
            return false;

        int previousEbla = unit.Ebla;

        if (amount > 0)
        {
            amount = (int)(amount * (1f + unit.CurrentStats.eblaMultiplier / 100f));
            if (previousEbla < 100 && previousEbla + amount > 100)
                amount = 100 - previousEbla;
        }
        unit.AddEbla(amount);

        if (unit.Ebla >= 200)
        {
            unit.Kill();
            EventBus.Publish(new PermanentDeathEvent(unit));
            return true;
        }

        if (previousEbla < 100 && unit.Ebla >= 100)
            EnqueueResolution(unit);
        else if (previousEbla > 0 && unit.Ebla == 0 && unit.EblaState == EblaState.Afflicted)
            ClearResolution(unit);

        return false;
    }
    private void EnqueueResolution(CombatUnit unit)
    {
        if (unit.EblaState != EblaState.Normal)
            return;
        bool rollVirtue = unit.NikkeData.CanVirtue && m_VirtueTypes != null && m_VirtueTypes.Length > 0 && Random.value < m_VirtueChance;

        PendingEblaResolution pending;
        pending.Unit = unit;

        if (rollVirtue)
        {
            pending.ResolutionType = EblaResolutionType.Virtuous;
            pending.VirtueType = PickVirtueType();
            pending.AfflictionType = null;
            unit.SetEblaState(EblaState.Virtuous);
        }
        else
        {
            pending.ResolutionType = EblaResolutionType.Afflicted;
            pending.AfflictionType = unit.NikkeData.ForcedAfflictionType ?? PickAfflictionType();
            pending.VirtueType = null;
            unit.SetEblaState(EblaState.Afflicted);
        }

        m_PendingQueue.Add(pending);
        EventBus.Publish(new AfflictionTriggeredEvent(unit));
    }

    private void ClearResolution(CombatUnit unit)
    {
        if (unit.CurrentAfflictionType != null)
            unit.RemoveEffect(unit.CurrentAfflictionType.Debuff);

        unit.SetCurrentAfflictionType(null);
        unit.SetEblaState(EblaState.Normal);
        unit.RecalculateStats();

        for (int i = m_PendingQueue.Count - 1; i >= 0; --i)
        {
            if (m_PendingQueue[i].Unit == unit)
                m_PendingQueue.RemoveAt(i);
        }
    }
    private AfflictionTypeData PickAfflictionType()
    {
        int total = 0;
        for (int i = 0; i < m_AfflictionTypes.Length; ++i)
            total += m_AfflictionTypes[i].RandomWeight;

        if (total == 0)
        {
            Debug.LogError("AfflictionType pool is empty or all weights are zero");
            return null;
        }

        int roll = Random.Range(0, total);
        int cumulative = 0;
        for (int i=0; i< m_AfflictionTypes.Length; ++i)
        {
            cumulative += m_AfflictionTypes[i].RandomWeight;
            if (roll < cumulative)
                return m_AfflictionTypes[i];
        }
        return null;
    }

    private VirtueTypeData PickVirtueType()
    {
        int total = 0;
        for (int i = 0; i < m_VirtueTypes.Length; ++i)
            total += m_VirtueTypes[i].RandomWeight;

        if (total == 0)
        {
            Debug.LogError("VirtueType pool is empty or all weights are zero.");
            return null;
        }

        int roll = Random.Range(0, total);
        int cumulative = 0;
        for (int i=0; i < m_VirtueTypes.Length; ++i)
        {
            cumulative += m_VirtueTypes[i].RandomWeight;
            if (roll < cumulative)
                return m_VirtueTypes[i];
        }
        return null;
    }

    public IReadOnlyList<PendingEblaResolution> DrainPending()
    {
        List<PendingEblaResolution> snapshot = new List<PendingEblaResolution>(m_PendingQueue);
        m_PendingQueue.Clear();
        return snapshot;
    }

    public void ApplyResolutionEffect(PendingEblaResolution pending)
    {
        if (pending.ResolutionType == EblaResolutionType.Afflicted)
        {
            pending.Unit.AddEffect(new ActiveStatusEffect(pending.AfflictionType.Debuff));
            pending.Unit.SetCurrentAfflictionType(pending.AfflictionType);
        }
        else
        {
            pending.Unit.AddEffect(new ActiveStatusEffect(pending.VirtueType.Buff));
            pending.Unit.SetCurrentVirtueType(pending.VirtueType);
            pending.Unit.AddEbla(-50);
        }
        pending.Unit.RecalculateStats();
    }
}
