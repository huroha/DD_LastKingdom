using UnityEngine;

public class SpriteSlotCache : MonoBehaviour
{
    private SpriteRenderer[] m_Slots;

    private Coroutine[] m_EntryCoroutines;
    private MonoBehaviour m_Runner;

    public SpriteRenderer[] Slots => m_Slots;

    public void Setup(SpriteRenderer root, int count , MonoBehaviour runner)
    {
        m_Runner = runner;
        if (m_EntryCoroutines == null || m_EntryCoroutines.Length < count)
            m_EntryCoroutines = new Coroutine[count];
        if (m_Slots == null || m_Slots.Length < count)
            m_Slots = new SpriteRenderer[count];
        m_Slots[0] = root;

        for (int i = 1; i < count; ++i)
        {
            if (m_Slots[i] != null)
            {
                m_Slots[i].gameObject.SetActive(true);
            }
            else
            {
                GameObject child = new GameObject("SpriteEntry");
                child.transform.SetParent(root.transform.parent, false);
                child.layer = root.gameObject.layer;
                m_Slots[i] = child.AddComponent<SpriteRenderer>();
            }
            m_Slots[i].sortingLayerID = root.sortingLayerID;
            m_Slots[i].sortingOrder = root.sortingOrder;
            m_Slots[i].flipX = root.flipX;
        }
    }

    public void HideExtra()
    {
        if (m_Slots == null)
            return;
        for (int i = 1; i < m_Slots.Length; ++i)
        {
            if (m_Slots[i] != null)
                m_Slots[i].gameObject.SetActive(false);
        }
    }
    public SpriteRenderer GetSlot(int i)
    {
        return m_Slots[i];
    }
    public void StoreEntry(int i, Coroutine co) { m_EntryCoroutines[i] = co; }
    public void StopEntries()
    {
        if (m_Runner == null || m_EntryCoroutines == null) return;
        for (int i = 0; i < m_EntryCoroutines.Length; ++i)
        {
            if (m_EntryCoroutines[i] != null)
                m_Runner.StopCoroutine(m_EntryCoroutines[i]);
            m_EntryCoroutines[i] = null;
        }
    }
}
