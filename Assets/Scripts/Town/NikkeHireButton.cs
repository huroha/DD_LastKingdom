using UnityEngine;
using UnityEngine.UI;

public class NikkeHireButton : MonoBehaviour
{
    [SerializeField] private NikkeData m_Nikke;
    [SerializeField] private Button m_Button;
    [SerializeField] private TownResourceDisplay m_ResourceDisplay;
    [SerializeField] private ConfirmPopup m_ConfirmPopup;
    private const string MSG_HIRE = "<b>{0}</b>을(를) 영입하시겠습니까?\n쥬얼 {1} 소모";

    private const int HireCost = 3;
    private void Awake()
    {
        m_Button.onClick.AddListener(OnClicked);
    }
    private void OnEnable()
    {
        RosterManager.Instance.OnRosterChanged += RefreshInteractable;
        RefreshInteractable();
    }
    private void OnDisable()
    {
        if (RosterManager.Instance != null) RosterManager.Instance.OnRosterChanged -= RefreshInteractable;
    }
    private void RefreshInteractable()
    {
        m_Button.interactable = !RosterManager.Instance.IsFull;
    }
    private void OnClicked()
    {
        if (m_Nikke == null) return;
        m_ConfirmPopup.Show(string.Format(MSG_HIRE, m_Nikke.NikkeName, HireCost), DoHire);
    }
    private void DoHire()
    {
        if (RosterManager.Instance.IsFull) return;
        if (!ResourceManager.Instance.SpendGems(HireCost)) return;
        RosterManager.Instance.HireNikke(m_Nikke);
        m_ResourceDisplay.Refresh();
    }
}
