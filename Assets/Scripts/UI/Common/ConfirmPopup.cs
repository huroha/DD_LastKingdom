using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ConfirmPopup : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI m_MessageText;
    [SerializeField] private Button m_ConfirmButton;
    [SerializeField] private Button m_CancelButton;
    [SerializeField] private Image m_HoverOverlay;

    private System.Action m_OnConfirm;


    private void Awake()
    {
        m_ConfirmButton.onClick.AddListener(OnConfirmClicked);
        m_CancelButton.onClick.AddListener(OnCancelClicked);
        m_HoverOverlay.gameObject.SetActive(false);
        AddHoverListeners(m_ConfirmButton);
        AddHoverListeners(m_CancelButton);
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
    private void AddHoverListeners(Button btn)
    {
        UIEventUtil.AddHover(m_ConfirmButton,
            () => OnButtonHovered(m_ConfirmButton),
            () => m_HoverOverlay.gameObject.SetActive(false));
        UIEventUtil.AddHover(m_CancelButton,
            () => OnButtonHovered(m_CancelButton),
            () => m_HoverOverlay.gameObject.SetActive(false));
    }
    private void OnButtonHovered(Button btn)
    {
        m_HoverOverlay.transform.position = btn.transform.position;
        m_HoverOverlay.gameObject.SetActive(true);
    }
}
