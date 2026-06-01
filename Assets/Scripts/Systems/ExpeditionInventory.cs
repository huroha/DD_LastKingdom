
public class ExpeditionInventory
{
    public delegate void ChangedHandler();
    public event ChangedHandler OnChanged;

    private LootItem[] m_Slots;
    private InventoryConfig m_Config;

    public int Capacity => m_Slots.Length;
    public LootItem[] Slots => m_Slots; // 배열 직접 UI가 인덱스로 읽어야 함

    public ExpeditionInventory(InventoryConfig config)
    {
        m_Config = config;
        m_Slots = new LootItem[config.SlotCount];
    }

    public bool TryAdd(LootItem loot)
    {
        int remaining = loot.Quantity;
        int cap = StackCap(loot);

        // 같은 스택 찾아서 한도까지 채우기
        for(int i=0; i< m_Slots.Length; ++i)
        {
            if (m_Slots[i].Quantity == 0) continue;             // 빈 슬롯 건너뛰기
            if (!SameStack(m_Slots[i], loot)) continue;         // 다른 종류 건너뛰기
            if (m_Slots[i].Quantity >= cap) continue;           // 이미 꽉 찬 슬롯 건너뛰기

            int space = cap - m_Slots[i].Quantity;              // 슬롯에 들어갈 여유
            int fill = remaining < space ? remaining : space;   // 실제로 채울 양
            m_Slots[i].Quantity += fill;
            remaining -= fill;

            if (remaining == 0) break;
        }

        // 남은 수량이 있으면 빈 슬롯에 새 스택
        if (remaining  > 0)
        {
            for (int i=0; i< m_Slots.Length; ++i)
            {
                if (m_Slots[i].Quantity != 0) continue;     // 비어있지않다면 건너뛰기

                LootItem newStack = loot;
                newStack.Quantity = remaining < cap ? remaining : cap;
                m_Slots[i] = newStack;
                remaining -= newStack.Quantity;

                if (remaining == 0) break;
            }
        }

        // 결과 판정
        bool anyAdded = remaining < loot.Quantity;      // 무언가 들어갔는가
        if (anyAdded) OnChanged?.Invoke();

        return remaining == 0;      // 전부 들어가야 true;

    }
    public LootItem TakeAt(int index)
    {
        LootItem taken = m_Slots[index];
        m_Slots[index] = default;
        OnChanged?.Invoke();
        return taken;
    }
    public void PlaceAt(int index, LootItem loot)
    {
        m_Slots[index] = loot;
        OnChanged?.Invoke();
    }
    public void Clear()
    {
        for (int i=0; i<Capacity; ++i)
        {
            m_Slots[i] = default;
        }
        OnChanged?.Invoke();
    }
    private int StackCap(LootItem item)
    {
        return m_Config.StackCap(item.Type);
        // 추후 so 아이템은 item.data에서 읽을것
    }
    private bool SameStack(LootItem a, LootItem b)
    {
        if (a.Type != b.Type) return false;
        switch(a.Type)
        {
            case LootType.Relics:
                return a.Relic == b.Relic;
            case LootType.Trinket:
            case LootType.SupplyItem:
                return a.Data == b.Data;
            default:
                return true;
        }
    }

}
