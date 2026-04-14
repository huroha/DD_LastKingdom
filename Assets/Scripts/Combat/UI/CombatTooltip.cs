using UnityEngine;
using TMPro;
using System.Text;

public class CombatTooltip : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI m_TooltipText;
    [SerializeField] private RectTransform m_RectTransform;

    [SerializeField] private Vector2 m_Padding;

    // 폰트 설정
    [SerializeField] private Color m_TextColor = Color.white;
    [SerializeField] private float m_FontSize = 22f;

    private Camera m_UICamera;

    private void Awake()
    {
        m_UICamera = null;
        m_TooltipText.color = m_TextColor;
        m_TooltipText.fontSize = m_FontSize;
    }

    public void Show(StringBuilder sb, Vector2 screenPosition, Vector2 offset)
    {
        m_TooltipText.SetText(sb);
        gameObject.SetActive(true);
        // 텍스트 preferred 크기로 RectTransform 직접 설정
        m_RectTransform.sizeDelta = new Vector2(
            m_TooltipText.preferredWidth + m_Padding.x,
            m_TooltipText.preferredHeight + m_Padding.y
        );
        RectTransform parentRect = m_RectTransform.parent as RectTransform;
        Vector2 localPoint;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPosition,m_UICamera,out localPoint);
        m_RectTransform.anchoredPosition = localPoint + offset;
        TooltipHelper.ClampToScreen(m_RectTransform);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
