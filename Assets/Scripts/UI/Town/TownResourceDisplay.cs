using UnityEngine;
using TMPro;

public class TownResourceDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI m_CreditText;
    [SerializeField] private TextMeshProUGUI m_CoreText;
    [SerializeField] private TextMeshProUGUI m_BattleDataText;
    [SerializeField] private TextMeshProUGUI m_GemsText;

    private void OnEnable()
    {
        Refresh();
    }
    public void Refresh()
    {
        m_CreditText.text = ResourceManager.Instance.Credit.ToString("#,0");
        m_CoreText.text = ResourceManager.Instance.Core.ToString("#,0");
        m_BattleDataText.text = ResourceManager.Instance.BattleData.ToString("#,0");
        m_GemsText.text = ResourceManager.Instance.Gems.ToString("#,0");
    }
}
