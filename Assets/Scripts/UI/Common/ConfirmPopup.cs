using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ConfirmPopup : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI m_MessageText;
    [SerializeField] private Button m_ConfirmButton;
    [SerializeField] private Button m_CancelButton;

    private System.Action m_OnConfirm;


    private void Awake()
    {
        m_ConfirmButton.onClick.AddListener(OnConfirmClicked);
        m_CancelButton.onClick.AddListener(OnCancelClicked);
        gameObject.SetActive(false);
    }
    public void Show(string message, System.Action onConfirm)
    {
        m_MessageText.text = message;
        m_OnConfirm = onConfirm;
        gameObject.SetActive(true);
    }
    public void Hide()
    {
        m_OnConfirm = null;
        gameObject.SetActive(false);
    }

    private void OnConfirmClicked()
    {
        System.Action callback = m_OnConfirm;
        Hide();
        callback?.Invoke();

    }
    private void OnCancelClicked()
    {
        Hide();
    }
}
