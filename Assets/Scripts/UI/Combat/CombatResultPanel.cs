using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class CombatResultPanel : MonoBehaviour, IDropHandler
{
    [Header("Content Root")]
    [SerializeField] private GameObject m_Content;

    [Header("Slots")]
    [SerializeField] private LootSlot m_CreditSlot;
    [SerializeField] private LootSlot m_BattleDataSlot;
    [SerializeField] private LootSlot m_CoreSlot;
    [SerializeField] private LootSlot m_GemsSlot;
    [SerializeField] private LootSlot[] m_RelicSlots;
    [SerializeField] private Transform m_ItemSlotRoot;
    [SerializeField] private LootSlot m_SlotPrefab;

    [Header("Buttons")]
    [SerializeField] private Button m_TakeAllBtn;
    [SerializeField] private Button m_ContinueBtn;


    private List<LootSlot> m_ActiveSlots = new List<LootSlot>();
    private List<LootSlot> m_DynamicSlots = new List<LootSlot>();

    private void OnEnable()
    {
        EventBus.Subscribe<BattleEndedEvent>(OnBattleEnded);
        m_TakeAllBtn.onClick.AddListener(OnTakeAllClicked);
        m_ContinueBtn.onClick.AddListener(OnContinueClicked);
    }

    private void OnDisable()
    {
        EventBus.Unsubscribe<BattleEndedEvent>(OnBattleEnded);
        m_TakeAllBtn.onClick.RemoveListener(OnTakeAllClicked);
        m_ContinueBtn.onClick.RemoveListener(OnContinueClicked);
    }
    private void OnBattleEnded(BattleEndedEvent e)
    {
        if (!e.IsVictory) return;
        m_Content.SetActive(true);
        SetupSlots(e.Result);
    }
    private void SetupSlots(CombatResult result)
    {
        // --- 이전 구독 정리 ---
        m_CreditSlot.OnCollected -= OnSlotCollected;
        m_BattleDataSlot.OnCollected -= OnSlotCollected;
        m_CoreSlot.OnCollected -= OnSlotCollected;
        m_GemsSlot.OnCollected -= OnSlotCollected;
        for (int i = 0; i < m_RelicSlots.Length; ++i)
            m_RelicSlots[i].OnCollected -= OnSlotCollected;

        //--- 동적 슬롯 정리 ---
        m_ActiveSlots.Clear();
        for (int i = 0; i < m_DynamicSlots.Count; ++i)
            if (m_DynamicSlots[i] != null)
                Destroy(m_DynamicSlots[i].gameObject);
        m_DynamicSlots.Clear();

        InventoryConfig cfg = DataManager.Instance.InventoryConfig;
        //--- 고정 재화 슬롯 (수량 0이면 비활성) ---
        SetupFixedSlot(m_CreditSlot, LootType.Credit, result.TotalCredit, cfg.Icon(LootType.Credit));
        SetupFixedSlot(m_BattleDataSlot, LootType.BattleData, result.TotalBattleData, cfg.Icon(LootType.BattleData));
        SetupFixedSlot(m_CoreSlot, LootType.Core, result.TotalCore, cfg.Icon(LootType.Core));
        SetupFixedSlot(m_GemsSlot, LootType.Gems, result.TotalGems, cfg.Icon(LootType.Gems));

        
         //--- Relic 슬롯 ---
         for (int i = 0;  i < m_RelicSlots.Length; ++i)
        {
            int qty = result.RelicAmounts[i];
            LootItem item = new LootItem
            {
                Type = LootType.Relics,
                Quantity = qty,
                Relic = (RelicType)i
            };
            m_RelicSlots[i].Setup(item, cfg.Icon(LootType.Relics, (RelicType)i));
            m_RelicSlots[i].OnCollected += OnSlotCollected;
            m_ActiveSlots.Add(m_RelicSlots[i]);
            if (qty == 0) m_RelicSlots[i].gameObject.SetActive(false);
        }
        
        //--- Trinket/SupplyItem 슬롯 (동적) ---
         for (int i = 0;  i < result.Items.Length;++i)
        {
            LootSlot slot = Instantiate(m_SlotPrefab, m_ItemSlotRoot);
            m_DynamicSlots.Add(slot);
            slot.Setup(result.Items[i], null);
           slot.OnCollected += OnSlotCollected;
           m_ActiveSlots.Add(slot);
        }
    }
    private void SetupFixedSlot(LootSlot slot, LootType type, int quantity, Sprite icon)
    {
        LootItem item = new LootItem { Type = type, Quantity = quantity };
        slot.Setup(item, icon);
        slot.OnCollected += OnSlotCollected;
        m_ActiveSlots.Add(slot);
        if (quantity == 0) slot.gameObject.SetActive(false);
    }
    private void OnTakeAllClicked()
    {
        for (int i = 0; i < m_ActiveSlots.Count; ++i)
            if (!m_ActiveSlots[i].IsCollected) m_ActiveSlots[i].ForceCollect();
    }

    private void OnContinueClicked()
    {
        m_Content.SetActive(false);
        EventBus.Publish(new LootDismissedEvent());
    }
    private void OnSlotCollected(LootSlot slot)
    {
        bool ok = ExpeditionManager.Instance.Inventory.TryAdd(slot.Item);
        if (!ok) slot.Restore();   // 가방 가득 → 슬롯 되살림 (미수집 유지)
    }
    public void OnDrop(PointerEventData eventData)
    {
        if (!LootDragState.IsDragging) return;
        if (LootDragState.From != DragSource.Inventory) return;

        LootItem taken = ExpeditionManager.Instance.Inventory.TakeAt(LootDragState.InventoryIndex);
        DropToGround(taken);
        LootDragState.IsDragging = false;
    }
    public void DropToGround(LootItem item)
    {
        LootSlot slot = Instantiate(m_SlotPrefab, m_ItemSlotRoot);
        Sprite icon = DataManager.Instance.InventoryConfig.Icon(item.Type, item.Relic);
        slot.Setup(item, icon);
        slot.OnCollected += OnSlotCollected;
        m_DynamicSlots.Add(slot);
        m_ActiveSlots.Add(slot);
    }

}
