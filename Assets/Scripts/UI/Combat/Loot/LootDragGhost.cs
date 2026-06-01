using UnityEngine;
using UnityEngine.UI;

public class LootDragGhost : MonoBehaviour
{
    public static LootDragGhost Instance
    {
        get; private set;
    }

    [SerializeField] private Image m_Image;
    [SerializeField] private CanvasGroup m_CanvasGroup; // block raycase false 필수

    private RectTransform m_Rect;
    private Canvas m_Canvas;
    private RectTransform m_CanvasRect;

    private void Awake()
    {
        Instance  = this;
        m_Rect = GetComponent<RectTransform>();
        m_Canvas = GetComponentInParent<Canvas>();
        m_CanvasRect = m_Canvas.GetComponent<RectTransform>();
        m_CanvasGroup.blocksRaycasts = false;
        gameObject.SetActive(false);
    }
    public void Show(Sprite icon, Vector2 screenPos)
    {
        m_Image.sprite = icon;
        gameObject.SetActive(true);
        Move(screenPos);
    }
    public void Move(Vector2 screenPos)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            m_CanvasRect,
            screenPos,
            m_Canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : m_Canvas.worldCamera,
            out Vector2 localPos);
        m_Rect.anchoredPosition = localPos;
    }
    public void Hide()
    {
        gameObject.SetActive(false);
    }

}
