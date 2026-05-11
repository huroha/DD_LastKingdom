using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class UIShaker : MonoBehaviour
{
    [Header("Shake")]
    [SerializeField] private float m_Amplitude = 6f;
    [SerializeField] private float m_Frequency = 1.5f;

    private RectTransform m_Rect;
    private Vector2 m_OriginalAnchoredPosition;

    private void Awake()
    {
        m_Rect = GetComponent<RectTransform>();
        m_OriginalAnchoredPosition = m_Rect.anchoredPosition;
    }

    private void OnDisable()
    {
        m_Rect.anchoredPosition = m_OriginalAnchoredPosition;

    }
    private void Update()
    {
        float t = Time.time * m_Frequency;
        float offsetX = (Mathf.PerlinNoise(t, 0f) - 0.5f) * 2f * m_Amplitude;
        float offsetY = (Mathf.PerlinNoise(0f, t) - 0.5f) * 2f * m_Amplitude;
        m_Rect.anchoredPosition = m_OriginalAnchoredPosition + new Vector2(offsetX, offsetY);
    }
}
