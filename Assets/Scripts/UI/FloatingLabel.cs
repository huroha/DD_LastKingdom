using TMPro;
using UnityEngine;
using System.Collections;

public class FloatingLabel : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI m_Text;
    [SerializeField] private Vector2 m_Offset;

    private RectTransform m_RectTransform;

    [SerializeField] private float m_FloatSpeed = 50f;   // 초당 이동 픽셀
    [SerializeField] private float m_Duration = 1f;
    private Coroutine m_FloatRoutine;

    private void Awake()
    {
        m_RectTransform = GetComponent<RectTransform>();
        gameObject.SetActive(false);
    }
    public void Show(string text, RectTransform anchor)
    {
        m_Text.text = text;
        m_RectTransform.position = anchor.position + new Vector3(m_Offset.x, m_Offset.y, 0f);
        gameObject.SetActive(true);

        if (m_FloatRoutine != null)
            StopCoroutine(m_FloatRoutine);
        m_FloatRoutine = StartCoroutine(FloatUp());
    }

    public void Hide()
    {
        if (m_FloatRoutine != null)
        {
            StopCoroutine(m_FloatRoutine);
            m_FloatRoutine = null;
        }
        gameObject.SetActive(false);
    }

    private IEnumerator FloatUp()
    {
        float elapsed = 0f;
        while (elapsed < m_Duration)
        {
            m_RectTransform.position += new Vector3(0f, m_FloatSpeed * Time.deltaTime, 0f);
            elapsed += Time.deltaTime;
            yield return null;
        }
        Hide();
    }
}
