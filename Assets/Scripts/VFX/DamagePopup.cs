using UnityEngine;
using TMPro;
using System.Collections;

public class DamagePopup : MonoBehaviour
{
    [SerializeField] private TextMeshPro m_Text;
    [SerializeField] private float m_FloatSpeed = 1f;
    [SerializeField] private float m_Duration = 0.8f;
    [SerializeField] private SpriteRenderer m_IconRenderer;
    [SerializeField] private int m_SortingOrder = 20;

    private Coroutine m_FloatRoutine;

    public void Show(Vector3 worldPosition, Vector3 offset, string text, Color color, float scale = 1f, Sprite icon = null)
    {
        m_Text.text = text;
        m_Text.color = color;
        transform.position = worldPosition + offset;
        transform.localScale = Vector3.one * scale;
        m_Text.sortingOrder = m_SortingOrder;
        if (m_IconRenderer != null)
        {
            if (icon != null)
            {
                m_IconRenderer.sprite = icon;
                m_IconRenderer.sortingOrder = m_SortingOrder;
                m_IconRenderer.enabled = true;
            }
            else
                m_IconRenderer.enabled = false;
        }

        gameObject.SetActive(true);

        CoroutineHelper.Restart(this, ref m_FloatRoutine, FloatUp());
    }

    private IEnumerator FloatUp()
    {
        float elapsed = 0f;
        while (elapsed < m_Duration)
        {
            transform.position += Vector3.up * m_FloatSpeed * Time.deltaTime;
            elapsed += Time.deltaTime;
            yield return null;
        }
        gameObject.SetActive(false);
    }
    private void OnDisable()
    {
        if (m_FloatRoutine != null)
            StopCoroutine(m_FloatRoutine);
    }

}
