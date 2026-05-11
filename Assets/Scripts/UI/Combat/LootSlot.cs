using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LootSlot : MonoBehaviour
{
    [SerializeField] private Image m_Icon;
    [SerializeField] private TextMeshProUGUI m_QuantityText;
    [SerializeField] private Button m_Button;

    private LootItem m_Item;
    private bool m_Collected;

    public delegate void CollectHandler(LootSlot slot);
    public event CollectHandler OnCollected;

    public LootItem Item => m_Item;
    public bool IsCollected => m_Collected;

    private void Awake()
    {
        m_Button.onClick.AddListener(OnClick);
    }
    public void Setup(LootItem item, Sprite iconSprite)
    {
        m_Item = item;
        m_Collected = false;
        m_Icon.sprite = iconSprite;
        m_QuantityText.SetText(item.Quantity > 1 ? "{0}" : "", item.Quantity);
        gameObject.SetActive(true);
    }
    private void OnClick()
    {
        if (m_Collected) return;
        m_Collected = true;
        gameObject.SetActive(false);
        if (OnCollected != null) OnCollected(this);
    }
    public void ForceCollect()
    {
        if (m_Collected) return;
        OnClick();
    }

}
