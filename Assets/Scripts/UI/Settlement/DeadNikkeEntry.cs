using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DeadNikkeEntry : MonoBehaviour
{
    [SerializeField] private Image m_Portrait;
    [SerializeField] private Image m_DeathOverlay;
    [SerializeField] private TextMeshProUGUI m_NameText;

    public void Bind(NikkeInstance nikke, bool isDead)
    {
        if (nikke == null) return;
        m_Portrait.sprite = nikke.Data.PortraitSprite;
        m_NameText.SetText(nikke.DisplayName);
        m_DeathOverlay.enabled = isDead;   // 사망자만 overlay
    }
}
