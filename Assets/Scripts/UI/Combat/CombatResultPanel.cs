using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class CombatResultPanel : MonoBehaviour
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

    [Header("Icons")]
    [SerializeField] private Sprite m_CreditIcon;
    [SerializeField] private Sprite m_BattleDataIcon;
    [SerializeField] private Sprite m_CoreIcon;
    [SerializeField] private Sprite m_GemsIcon;
    [SerializeField] private Sprite[] m_RelicIcons;

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

        //--- 고정 재화 슬롯 (수량 0이면 비활성) ---
        SetupFixedSlot(m_CreditSlot, LootType.Credit, result.TotalCredit, m_CreditIcon);
        SetupFixedSlot(m_BattleDataSlot, LootType.BattleData, result.TotalBattleData, m_BattleDataIcon);
        SetupFixedSlot(m_CoreSlot, LootType.Core, result.TotalCore, m_CoreIcon);
        SetupFixedSlot(m_GemsSlot, LootType.Gems, result.TotalGems, m_GemsIcon);
        
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
            m_RelicSlots[i].Setup(item, m_RelicIcons[i]);
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
    private void OnSlotCollected(LootSlot slot)
    {
        switch (slot.Item.Type)
        {
            case LootType.Credit:
                GameManager.Instance.AddCredit(slot.Item.Quantity);
                break;
            case LootType.BattleData:
                GameManager.Instance.AddBattleData(slot.Item.Quantity);
                break;
            case LootType.Core:
                GameManager.Instance.AddCore(slot.Item.Quantity);
                break;
            case LootType.Gems:
                GameManager.Instance.AddGems(slot.Item.Quantity);
                break;
            case LootType.Relics:
                GameManager.Instance.AddRelics(slot.Item.Relic, slot.Item.Quantity);
                break;
            default:
                Debug.Log("획득: " + slot.Item.Type);
                break;

        }
    }
    private void OnTakeAllClicked()
    {
        for (int i = 0; i < m_ActiveSlots.Count; ++i)
            if (!m_ActiveSlots[i].IsCollected) m_ActiveSlots[i].ForceCollect();
    }

    private void OnContinueClicked()
    {
        m_Content.SetActive(false);
        GameManager.Instance.ChangeState(GameState.Dungeon);
    }
}
