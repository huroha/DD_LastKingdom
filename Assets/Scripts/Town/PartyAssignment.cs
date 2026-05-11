using UnityEngine;
using System.Collections.Generic;
public class PartyAssignment
{
    public delegate void ChangedHandler();
    public event ChangedHandler OnChanged;

    private NikkeInstance[] m_Slots;

    public int SlotCount => m_Slots.Length;
    public PartyAssignment(int slotCount) {  m_Slots = new NikkeInstance[slotCount]; }

    // 조회
    public NikkeInstance Get(int idx) => m_Slots[idx];
    public bool IsEmpty(int idx) => m_Slots[idx] == null;
    public bool IsAssigned(NikkeInstance inst)
    {
        for (int i = 0; i < m_Slots.Length; ++i)
            if (m_Slots[i] == inst) return true;
        return false;
    }
    public int IndexOf(NikkeInstance inst)
    {
        for (int i = 0; i < m_Slots.Length; ++i)
            if (m_Slots[i] == inst) return i;
        return -1;
    }
    public int FilledCount()
    {
        int count = 0;
        for (int i = 0; i < m_Slots.Length; ++i)
            if (m_Slots[i] != null) ++count;
        return count;
    }
    public int FindFirstEmpty()
    {
        for (int i = 0; i < m_Slots.Length; ++i)
            if (m_Slots[i] == null) return i;
        return -1;
    }

    // mutation 모두 OnChanged 한번 fire
    public void Assign(int slotIdx, NikkeInstance inst)
    {
        // automatic move semantic
        int existing = IndexOf(inst);
        if (existing >= 0) m_Slots[existing] = null; // 기존 슬롯 비움
        m_Slots[slotIdx] = inst;
        OnChanged?.Invoke();
    }
    public void Clear(int slotIdx) { m_Slots[slotIdx] = null; OnChanged?.Invoke(); }
    public void Swap(int slotA,int slotB)
    {
        NikkeInstance temp = m_Slots[slotA];
        m_Slots[slotA] = m_Slots[slotB];
        m_Slots[slotB] = temp;
        OnChanged?.Invoke();
    }
    public void ShiftInsert(int srcIdx, int tgtIdx)
    {
        if (srcIdx == tgtIdx) return;
        NikkeInstance moving = m_Slots[srcIdx];
        int step = srcIdx > tgtIdx ? -1 : 1;
        for (int i = srcIdx; i != tgtIdx; i += step)
            m_Slots[i] = m_Slots[i + step];
        m_Slots[tgtIdx] = moving;
        OnChanged?.Invoke();
    }
}
