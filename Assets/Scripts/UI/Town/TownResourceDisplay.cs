using UnityEngine;
using TMPro;

public class TownResourceDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI m_CreditText;

    private void OnEnable()
    {
        Refresh();
    }
    private void Refresh()
    {
        m_CreditText.text = ResourceManager.Instance.Credit.ToString("#,0");
    }
}
