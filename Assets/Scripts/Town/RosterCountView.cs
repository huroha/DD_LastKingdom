using UnityEngine;
using TMPro;
public class RosterCountView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI m_CountText;

    private void OnEnable()
    {
        RosterManager.Instance.OnRosterChanged += Refresh;
        Refresh();
    }
    private void OnDisable()
    {
        if (RosterManager.Instance != null) RosterManager.Instance.OnRosterChanged -= Refresh;
    }
    private void Refresh()
    {
        m_CountText.SetText("{0}/{1}", RosterManager.Instance.Count, RosterManager.Instance.MaxRoster);
    }
}
